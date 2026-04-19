using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Editing
{
    public interface IGridEditCellAccessor
    {
        bool TryGetValue(string rowKey, string columnKey, out object value);

        void SetValue(string rowKey, string columnKey, object value);
    }

    public interface IGridEditValidator
    {
        IReadOnlyList<GridValidationError> Validate(string rowKey, string columnKey, object parsedValue, string editingText);
    }

    /// <summary>
    /// Stan pojedynczej sesji edycji komórki.
    /// </summary>
    public sealed class GridEditSession
    {
        /// <summary>
        /// Klucz wiersza, który jest edytowany.
        /// </summary>
        public string RowKey { get; }

        /// <summary>
        /// Klucz kolumny, która jest edytowana.
        /// </summary>
        public string ColumnKey { get; }

        /// <summary>
        /// Oryginalna wartość przed edycją.
        /// </summary>
        public object OriginalValue { get; }

        /// <summary>
        /// Bieżący tekst edytowany w komórce.
        /// </summary>
        public string EditingText { get; set; }

        /// <summary>
        /// Czy jest nowa komórka (empty cell) czy istniejąca.
        /// </summary>
        public bool IsNewCell { get; }

        /// <summary>
        /// Timestamp kiedy rozpoczęła się sesja.
        /// </summary>
        public System.DateTime StartedAt { get; }

        /// <summary>
        /// Czy tekst został zmodyfikowany od rozpoczęcia sesji.
        /// </summary>
        public bool IsModified => EditingText != GetOriginalText();

        public GridEditSession(string rowKey, string columnKey, object originalValue, bool isNewCell = false)
        {
            RowKey = rowKey ?? throw new System.ArgumentNullException(nameof(rowKey));
            ColumnKey = columnKey ?? throw new System.ArgumentNullException(nameof(columnKey));
            OriginalValue = originalValue;
            IsNewCell = isNewCell;
            StartedAt = System.DateTime.Now;
            EditingText = GetOriginalText();
        }

        private string GetOriginalText()
        {
            if (IsNewCell) return string.Empty;
            return OriginalValue?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Resetuje do oryginalnej wartości.
        /// </summary>
        public void Reset()
        {
            EditingText = GetOriginalText();
        }
    }

    /// <summary>
    /// Stan edycji grida - zawiera info o bieżącej sesji edycji.
    /// </summary>
    public sealed class GridEditState
    {
        /// <summary>
        /// Czy grid jest w trybie edycji.
        /// </summary>
        public bool IsInEditMode { get; set; }

        /// <summary>
        /// Bieżąca sesja edycji (null jeśli nie ma sesji).
        /// </summary>
        public GridEditSession CurrentSession { get; set; }

        /// <summary>
        /// Czy edycja jest w trakcie walidacji.
        /// </summary>
        public bool IsValidating { get; set; }

        /// <summary>
        /// Komunikat błędu walidacji (jeśli istnieje).
        /// </summary>
        public string ValidationError { get; set; }

        /// <summary>
        /// Tryb rozpoczęcia edycji. 
        /// </summary>
        public GridEditStartMode StartMode { get; set; } = GridEditStartMode.DoubleClick;
    }

    /// <summary>
    /// Realny model edycji pojedynczej komórki używany przez surface grid.
    /// </summary>
    public sealed class GridEditModel
    {
        private readonly IGridEditCellAccessor _cellAccessor;
        private readonly IGridEditValidator _validator;
        private readonly Func<string, string, string, object> _valueParser;
        private readonly Dictionary<string, IReadOnlyList<GridValidationError>> _validationErrors =
            new Dictionary<string, IReadOnlyList<GridValidationError>>(StringComparer.Ordinal);

        public GridEditModel(
            IGridEditCellAccessor cellAccessor,
            IGridEditValidator validator = null,
            Func<string, string, string, object> valueParser = null,
            GridEditState state = null)
        {
            _cellAccessor = cellAccessor ?? throw new ArgumentNullException(nameof(cellAccessor));
            _validator = validator;
            _valueParser = valueParser;
            State = state ?? new GridEditState();
        }

        public GridEditState State { get; }

        public IReadOnlyDictionary<string, IReadOnlyList<GridValidationError>> ValidationErrors => _validationErrors;

        public bool StartSession(string rowKey, string columnKey, GridEditStartMode startMode, bool isNewCell = false)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                throw new ArgumentException("Row key is required.", nameof(rowKey));
            }

            if (string.IsNullOrWhiteSpace(columnKey))
            {
                throw new ArgumentException("Column key is required.", nameof(columnKey));
            }

            _cellAccessor.TryGetValue(rowKey, columnKey, out var currentValue);

            State.CurrentSession = new GridEditSession(rowKey, columnKey, currentValue, isNewCell);
            State.StartMode = startMode;
            State.IsInEditMode = true;
            State.IsValidating = false;
            State.ValidationError = null;
            _validationErrors.Remove(GetCellKey(rowKey, columnKey));
            return true;
        }

        public bool Commit()
        {
            var session = State.CurrentSession;
            if (session == null)
            {
                return false;
            }

            State.IsValidating = true;
            var parsedValue = ParseEditingValue(session);
            var errors = _validator?.Validate(session.RowKey, session.ColumnKey, parsedValue, session.EditingText)
                ?? Array.Empty<GridValidationError>();
            var cellKey = GetCellKey(session.RowKey, session.ColumnKey);

            if (errors.Count > 0)
            {
                _validationErrors[cellKey] = errors;
                State.ValidationError = errors[0].Message;
                State.IsValidating = false;
                State.IsInEditMode = true;
                return false;
            }

            _cellAccessor.SetValue(session.RowKey, session.ColumnKey, parsedValue);
            _validationErrors.Remove(cellKey);
            State.ValidationError = null;
            State.IsValidating = false;
            State.IsInEditMode = false;
            State.CurrentSession = null;
            return true;
        }

        public bool Cancel()
        {
            if (State.CurrentSession == null)
            {
                return false;
            }

            _validationErrors.Remove(GetCellKey(State.CurrentSession.RowKey, State.CurrentSession.ColumnKey));
            State.CurrentSession.Reset();
            State.ValidationError = null;
            State.IsValidating = false;
            State.IsInEditMode = false;
            State.CurrentSession = null;
            return true;
        }

        public bool AppendText(string text)
        {
            if (State.CurrentSession == null || string.IsNullOrEmpty(text))
            {
                return false;
            }

            State.CurrentSession.EditingText += text;
            RevalidateCurrentSession();
            return true;
        }

        public bool DeleteLast()
        {
            if (State.CurrentSession == null || string.IsNullOrEmpty(State.CurrentSession.EditingText))
            {
                return false;
            }

            var text = State.CurrentSession.EditingText;
            State.CurrentSession.EditingText = text.Substring(0, text.Length - 1);
            RevalidateCurrentSession();
            return true;
        }

        public bool Clear()
        {
            if (State.CurrentSession == null)
            {
                return false;
            }

            State.CurrentSession.EditingText = string.Empty;
            RevalidateCurrentSession();
            return true;
        }

        public bool SetText(string text)
        {
            if (State.CurrentSession == null)
            {
                return false;
            }

            var normalizedText = text ?? string.Empty;
            if (string.Equals(State.CurrentSession.EditingText, normalizedText, StringComparison.Ordinal))
            {
                return false;
            }

            State.CurrentSession.EditingText = normalizedText;
            RevalidateCurrentSession();
            return true;
        }

        private void RevalidateCurrentSession()
        {
            var session = State.CurrentSession;
            if (session == null)
            {
                return;
            }

            var cellKey = GetCellKey(session.RowKey, session.ColumnKey);
            var parsedValue = ParseEditingValue(session);
            var errors = _validator?.Validate(session.RowKey, session.ColumnKey, parsedValue, session.EditingText)
                ?? Array.Empty<GridValidationError>();

            if (errors.Count > 0)
            {
                _validationErrors[cellKey] = errors;
                State.ValidationError = errors[0].Message;
                return;
            }

            _validationErrors.Remove(cellKey);
            State.ValidationError = null;
        }

        private object ParseEditingValue(GridEditSession session)
        {
            if (_valueParser != null)
            {
                return _valueParser(session.RowKey, session.ColumnKey, session.EditingText);
            }

            return session.EditingText;
        }

        private static string GetCellKey(string rowKey, string columnKey)
        {
            return rowKey + "_" + columnKey;
        }
    }

    /// <summary>
    /// Sposób rozpoczęcia edycji komórki.
    /// </summary>
    public enum GridEditStartMode
    {
        /// <summary>
        /// Edycja przez double-click.
        /// </summary>
        DoubleClick,

        /// <summary>
        /// Edycja przez naciśnięcie Enter.
        /// </summary>
        Enter,

        /// <summary>
        /// Edycja przez wpisanie znaku (replace mode).
        /// </summary>
        ReplaceMode,

        /// <summary>
        /// Edycja przez F2.
        /// </summary>
        F2,

        /// <summary>
        /// Edycja programowa (programmatically).
        /// </summary>
        Programmatic,
    }

}

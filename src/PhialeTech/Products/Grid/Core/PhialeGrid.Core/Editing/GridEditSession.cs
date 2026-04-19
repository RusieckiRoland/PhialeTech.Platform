using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Editing
{
    public sealed class GridEditSession<T>
    {
        private readonly IGridRowEditor<T> _editor;
        private readonly Func<T, string> _rowIdSelector;
        private readonly IGridRowValidator<T> _validator;
        private readonly Func<T, string> _rowVersionSelector;
        private readonly IGridConflictResolver<T> _conflictResolver;

        private readonly Dictionary<string, T> _originalRows = new Dictionary<string, T>();
        private readonly Dictionary<string, T> _workingRows = new Dictionary<string, T>();
        private readonly Dictionary<string, GridRowChangeType> _changeTypes = new Dictionary<string, GridRowChangeType>();
        private readonly Dictionary<string, Dictionary<string, GridCellChange>> _cellChanges = new Dictionary<string, Dictionary<string, GridCellChange>>();
        private readonly HashSet<string> _newRows = new HashSet<string>();

        public GridEditSession(
            IGridRowEditor<T> editor,
            Func<T, string> rowIdSelector,
            IGridRowValidator<T> validator = null,
            Func<T, string> rowVersionSelector = null,
            IGridConflictResolver<T> conflictResolver = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _rowIdSelector = rowIdSelector ?? throw new ArgumentNullException(nameof(rowIdSelector));
            _validator = validator;
            _rowVersionSelector = rowVersionSelector;
            _conflictResolver = conflictResolver;
        }

        public IReadOnlyCollection<string> DirtyRowIds => _changeTypes.Where(x => x.Value != GridRowChangeType.None).Select(x => x.Key).ToArray();

        public void BeginEdit(T row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var rowId = _rowIdSelector(row);
            if (_newRows.Contains(rowId))
            {
                if (!_workingRows.ContainsKey(rowId))
                {
                    _workingRows[rowId] = _editor.Clone(row);
                }

                EnsureCellChanges(rowId);
                if (!_changeTypes.ContainsKey(rowId))
                {
                    _changeTypes[rowId] = GridRowChangeType.Added;
                }

                return;
            }

            if (!_originalRows.ContainsKey(rowId))
            {
                _originalRows[rowId] = _editor.Clone(row);
            }

            if (!_workingRows.ContainsKey(rowId))
            {
                _workingRows[rowId] = _editor.Clone(row);
            }

            EnsureCellChanges(rowId);
            if (!_changeTypes.ContainsKey(rowId))
            {
                _changeTypes[rowId] = GridRowChangeType.None;
            }
        }

        public void MarkAsNew(T row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var rowId = _rowIdSelector(row);
            _workingRows[rowId] = _editor.Clone(row);
            _originalRows.Remove(rowId);
            _changeTypes[rowId] = GridRowChangeType.Added;
            _newRows.Add(rowId);
            EnsureCellChanges(rowId);
        }

        public void MarkAsDeleted(T row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var rowId = _rowIdSelector(row);
            if (_newRows.Contains(rowId))
            {
                RemoveTracking(rowId);
                return;
            }

            BeginEdit(row);
            _changeTypes[rowId] = GridRowChangeType.Deleted;
        }

        public void SetCellValue(T row, string columnId, object value)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            BeginEdit(row);
            var rowId = _rowIdSelector(row);
            var working = _workingRows[rowId];
            _editor.SetValue(working, columnId, value);

            var originalValue = _originalRows.ContainsKey(rowId) ? _editor.GetValue(_originalRows[rowId], columnId) : null;
            var currentValue = _editor.GetValue(working, columnId);
            var changes = EnsureCellChanges(rowId);

            if (!_newRows.Contains(rowId) && object.Equals(originalValue, currentValue))
            {
                changes.Remove(columnId);
                if (changes.Count == 0 && _changeTypes[rowId] == GridRowChangeType.Modified)
                {
                    _changeTypes[rowId] = GridRowChangeType.None;
                }

                return;
            }

            changes[columnId] = new GridCellChange(columnId, originalValue, currentValue);

            if (_changeTypes[rowId] == GridRowChangeType.None)
            {
                _changeTypes[rowId] = GridRowChangeType.Modified;
            }
        }

        public T GetWorkingRow(string rowId)
        {
            T row;
            if (!_workingRows.TryGetValue(rowId, out row))
            {
                throw new InvalidOperationException("Row not tracked: " + rowId);
            }

            return row;
        }

        public IReadOnlyList<GridCellChange> GetCellChanges(string rowId)
        {
            Dictionary<string, GridCellChange> changes;
            if (!_cellChanges.TryGetValue(rowId, out changes))
            {
                return Array.Empty<GridCellChange>();
            }

            return changes.Values.ToArray();
        }

        public void CancelChanges(string rowId)
        {
            if (_newRows.Contains(rowId))
            {
                RemoveTracking(rowId);
                return;
            }

            if (!_originalRows.ContainsKey(rowId))
            {
                return;
            }

            _workingRows[rowId] = _editor.Clone(_originalRows[rowId]);
            _changeTypes[rowId] = GridRowChangeType.None;
            EnsureCellChanges(rowId).Clear();
        }

        public Task<IReadOnlyList<GridRowChange<T>>> CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return CommitAsync(null, cancellationToken);
        }

        public async Task<IReadOnlyList<GridRowChange<T>>> CommitAsync(Func<string, CancellationToken, Task<T>> latestRowLoader, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new List<GridRowChange<T>>();
            foreach (var rowId in _changeTypes.Where(x => x.Value != GridRowChangeType.None).Select(x => x.Key).ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var original = _originalRows.ContainsKey(rowId) ? _originalRows[rowId] : default(T);
                var current = _workingRows[rowId];
                var type = _changeTypes[rowId];
                var originalVersion = GetVersion(original);
                var latestRow = default(T);
                var latestVersion = originalVersion;
                var mergedRow = current;
                var hasConflict = false;

                if (latestRowLoader != null && _rowVersionSelector != null && type != GridRowChangeType.Added)
                {
                    latestRow = await latestRowLoader(rowId, cancellationToken).ConfigureAwait(false);
                    latestVersion = GetVersion(latestRow);
                    hasConflict = !string.Equals(originalVersion, latestVersion, StringComparison.Ordinal);

                    if (hasConflict && _conflictResolver != null)
                    {
                        mergedRow = _conflictResolver.Resolve(new GridConcurrencyConflict<T>(
                            rowId,
                            _editor.Clone(original),
                            _editor.Clone(current),
                            latestRow == null ? default(T) : _editor.Clone(latestRow),
                            originalVersion,
                            latestVersion));

                        current = mergedRow;
                        _workingRows[rowId] = _editor.Clone(mergedRow);
                        hasConflict = false;
                    }
                }

                var errors = (await ValidateAsync(current, cancellationToken).ConfigureAwait(false)).ToList();
                if (hasConflict)
                {
                    errors.Add(new GridValidationError(string.Empty, "Concurrency conflict detected."));
                }

                var change = new GridRowChange<T>(
                    rowId,
                    type,
                    CloneIfPresent(original),
                    CloneIfPresent(current),
                    errors,
                    GetCellChanges(rowId),
                    originalVersion,
                    latestVersion,
                    hasConflict,
                    CloneIfPresent(latestRow),
                    CloneIfPresent(mergedRow));

                result.Add(change);

                if (change.IsValid)
                {
                    if (type == GridRowChangeType.Deleted)
                    {
                        RemoveTracking(rowId);
                        continue;
                    }

                    _originalRows[rowId] = _editor.Clone(current);
                    _workingRows[rowId] = _editor.Clone(current);
                    _changeTypes[rowId] = GridRowChangeType.None;
                    EnsureCellChanges(rowId).Clear();
                    _newRows.Remove(rowId);
                }
            }

            return result;
        }

        private string GetVersion(T row)
        {
            if (_rowVersionSelector == null || EqualityComparer<T>.Default.Equals(row, default(T)))
            {
                return null;
            }

            return _rowVersionSelector(row);
        }

        private Task<IReadOnlyList<GridValidationError>> ValidateAsync(T row, CancellationToken cancellationToken)
        {
            if (_validator == null)
            {
                return Task.FromResult<IReadOnlyList<GridValidationError>>(Array.Empty<GridValidationError>());
            }

            return _validator.ValidateAsync(row, cancellationToken);
        }

        private Dictionary<string, GridCellChange> EnsureCellChanges(string rowId)
        {
            Dictionary<string, GridCellChange> changes;
            if (!_cellChanges.TryGetValue(rowId, out changes))
            {
                changes = new Dictionary<string, GridCellChange>(StringComparer.Ordinal);
                _cellChanges[rowId] = changes;
            }

            return changes;
        }

        private T CloneIfPresent(T row)
        {
            return EqualityComparer<T>.Default.Equals(row, default(T)) ? default(T) : _editor.Clone(row);
        }

        private void RemoveTracking(string rowId)
        {
            _originalRows.Remove(rowId);
            _workingRows.Remove(rowId);
            _changeTypes.Remove(rowId);
            _cellChanges.Remove(rowId);
            _newRows.Remove(rowId);
        }
    }
}

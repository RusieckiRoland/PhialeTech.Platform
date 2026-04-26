using PhialeTech.YamlApp.Runtime.Model;
using System;

namespace PhialeTech.YamlApp.Runtime.Controls.DocumentEditor
{
    public sealed class YamlDocumentEditorFieldBinding : IDisposable
    {
        private readonly RuntimeFieldState _fieldState;

        public YamlDocumentEditorFieldBinding(RuntimeFieldState fieldState)
        {
            _fieldState = fieldState ?? throw new ArgumentNullException(nameof(fieldState));
            _fieldState.StateChanged += OnFieldStateChanged;
        }

        public event EventHandler<YamlDocumentEditorFieldBindingState> StateChanged;

        public YamlDocumentEditorFieldBindingState GetState()
        {
            return new YamlDocumentEditorFieldBindingState
            {
                Caption = _fieldState.Field?.CaptionKey ?? string.Empty,
                Placeholder = _fieldState.Field?.PlaceholderKey ?? string.Empty,
                DocumentJson = _fieldState.Value == null ? string.Empty : _fieldState.Value.ToString(),
                OldValue = _fieldState.OldValue == null ? string.Empty : _fieldState.OldValue.ToString(),
                ErrorMessage = _fieldState.ErrorMessage ?? string.Empty,
                IsEnabled = _fieldState.Enabled,
                IsVisible = _fieldState.Visible,
                IsRequired = _fieldState.Field != null && _fieldState.Field.IsRequired,
                ShowOldValueRestoreButton = _fieldState.ShowOldValueRestoreButton,
                InteractionMode = _fieldState.InteractionMode,
                DensityMode = _fieldState.DensityMode,
                FieldChromeMode = _fieldState.Field == null ? Abstractions.Enums.FieldChromeMode.Framed : _fieldState.Field.FieldChromeMode,
                CaptionPlacement = _fieldState.Field == null ? Abstractions.Enums.CaptionPlacement.Top : _fieldState.Field.CaptionPlacement,
            };
        }

        public void UpdateDocumentJson(string documentJson)
        {
            _fieldState.SetValue(documentJson ?? string.Empty);
        }

        public void RestoreOldValue()
        {
            _fieldState.RestoreOldValue();
        }

        public void Dispose()
        {
            _fieldState.StateChanged -= OnFieldStateChanged;
        }

        private void OnFieldStateChanged(object sender, EventArgs e)
        {
            StateChanged?.Invoke(this, GetState());
        }
    }
}

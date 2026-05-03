using PhialeTech.DocumentEditor.Abstractions;
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
            var documentEditorField = _fieldState.Field == null ? null : _fieldState.Field.Definition as PhialeTech.YamlApp.Abstractions.Interfaces.IDocumentEditorFieldDefinition;
            return new YamlDocumentEditorFieldBindingState
            {
                Caption = _fieldState.Field?.CaptionKey ?? string.Empty,
                Placeholder = _fieldState.Field?.PlaceholderKey ?? string.Empty,
                DocumentJson = _fieldState.Value == null ? string.Empty : _fieldState.Value.ToString(),
                ErrorMessage = _fieldState.ErrorMessage ?? string.Empty,
                IsEnabled = _fieldState.Enabled,
                IsVisible = _fieldState.Visible,
                IsRequired = _fieldState.Field != null && _fieldState.Field.IsRequired,
                InteractionMode = _fieldState.InteractionMode,
                DensityMode = _fieldState.DensityMode,
                FieldChromeMode = _fieldState.Field == null ? Abstractions.Enums.FieldChromeMode.Framed : _fieldState.Field.FieldChromeMode,
                CaptionPlacement = _fieldState.Field == null ? Abstractions.Enums.CaptionPlacement.Top : _fieldState.Field.CaptionPlacement,
                OverlayMode = documentEditorField == null || !documentEditorField.OverlayMode.HasValue
                    ? DocumentEditorOverlayMode.Container
                    : documentEditorField.OverlayMode.Value,
            };
        }

        public void UpdateDocumentJson(string documentJson)
        {
            _fieldState.SetValue(documentJson ?? string.Empty);
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

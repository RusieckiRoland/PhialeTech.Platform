using System;
using PhialeTech.YamlApp.Runtime.Model;

namespace PhialeTech.YamlApp.Runtime.Controls.TextBox
{
    /// <summary>
    /// Bridges RuntimeFieldState with platform hosts while keeping the binding logic platform-agnostic.
    /// </summary>
    public sealed class YamlTextBoxFieldBinding : IDisposable
    {
        private readonly RuntimeFieldState _fieldState;

        public YamlTextBoxFieldBinding(RuntimeFieldState fieldState)
        {
            _fieldState = fieldState ?? throw new ArgumentNullException(nameof(fieldState));
            _fieldState.StateChanged += OnFieldStateChanged;
        }

        public event EventHandler<YamlTextBoxFieldBindingState> StateChanged;

        public RuntimeFieldState FieldState => _fieldState;

        public YamlTextBoxFieldBindingState GetState()
        {
            return new YamlTextBoxFieldBindingState
            {
                FieldId = _fieldState.Id ?? string.Empty,
                Caption = _fieldState.Field?.CaptionKey ?? string.Empty,
                Placeholder = _fieldState.Field?.PlaceholderKey ?? string.Empty,
                Text = _fieldState.Value == null ? string.Empty : _fieldState.Value.ToString(),
                OldValue = _fieldState.OldValue == null ? string.Empty : _fieldState.OldValue.ToString(),
                ErrorMessage = _fieldState.ErrorMessage ?? string.Empty,
                Width = _fieldState.Width,
                WidthHint = _fieldState.WidthHint,
                MaxLength = _fieldState.MaxLength,
                InteractionMode = _fieldState.InteractionMode,
                DensityMode = _fieldState.DensityMode,
                IsEnabled = _fieldState.Enabled,
                IsVisible = _fieldState.Visible,
                IsRequired = _fieldState.Field != null && _fieldState.Field.IsRequired,
                ShowOldValueRestoreButton = _fieldState.ShowOldValueRestoreButton,
                FieldChromeMode = _fieldState.Field == null ? Abstractions.Enums.FieldChromeMode.Framed : _fieldState.Field.FieldChromeMode,
                CaptionPlacement = _fieldState.Field == null ? Abstractions.Enums.CaptionPlacement.Top : _fieldState.Field.CaptionPlacement,
            };
        }

        public void UpdateText(string text)
        {
            _fieldState.SetValue(text ?? string.Empty);
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

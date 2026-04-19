using PhialeTech.YamlApp.Core.Resolved;
using System;
using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Runtime.Model
{
    public sealed class RuntimeFieldState
    {
        public RuntimeFieldState(ResolvedFieldDefinition field)
        {
            Field = field;
            Value = null;
            OldValue = null;
            IsTouched = false;
            IsPristine = true;
            IsDirty = false;
            IsValid = true;
        }

        public event EventHandler StateChanged;

        public ResolvedFieldDefinition Field { get; }

        public string Id => Field == null ? null : Field.Id;

        public string Name => Field == null ? null : Field.Name;

        public double? Width => Field == null ? null : Field.Width;

        public FieldWidthHint? WidthHint => Field == null ? null : Field.WidthHint;

        public InteractionMode InteractionMode => Field == null ? InteractionMode.Classic : Field.InteractionMode;

        public DensityMode? DensityMode => Field == null ? null : Field.DensityMode;

        public bool Visible => Field != null && Field.Visible;

        public bool Enabled => Field != null && Field.Enabled;

        public bool ShowOldValueRestoreButton => Field != null && Field.ShowOldValueRestoreButton;

        public CaptionPlacement CaptionPlacement => Field == null ? CaptionPlacement.Top : Field.CaptionPlacement;

        public int? MaxLength => Field == null ? null : Field.MaxLength;

        public int? MinValue => Field == null ? null : Field.MinValue;

        public int? MaxNumericValue => Field == null ? null : Field.MaxNumericValue;

        public object Value { get; private set; }

        public object OldValue { get; private set; }

        public bool IsTouched { get; private set; }

        public bool IsPristine { get; private set; }

        public bool IsDirty { get; private set; }

        public bool IsValid { get; private set; }

        public string ErrorCode { get; private set; }

        public string ErrorMessage { get; private set; }

        public void SetValue(object value, bool markTouched = true)
        {
            Value = value;
            IsTouched = IsTouched || markTouched;
            IsDirty = !Equals(Value, OldValue);
            IsPristine = !IsDirty;
            RaiseStateChanged();
        }

        public void LoadValue(object value)
        {
            Value = value;
            OldValue = value;
            IsTouched = false;
            IsDirty = false;
            IsPristine = true;
            RaiseStateChanged();
        }

        public void RestoreOldValue(bool markTouched = true)
        {
            SetValue(OldValue, markTouched);
        }

        public void SetValidation(string errorCode, string errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            IsValid = string.IsNullOrWhiteSpace(errorCode) && string.IsNullOrWhiteSpace(errorMessage);
            RaiseStateChanged();
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

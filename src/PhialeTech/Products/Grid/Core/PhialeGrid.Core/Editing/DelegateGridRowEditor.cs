using System;

namespace PhialeGrid.Core.Editing
{
    public sealed class DelegateGridRowEditor<T> : IGridRowEditor<T>
    {
        private readonly Func<T, string, object> _getter;
        private readonly Action<T, string, object> _setter;
        private readonly Func<T, T> _cloner;

        public DelegateGridRowEditor(Func<T, string, object> getter, Action<T, string, object> setter, Func<T, T> cloner)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _cloner = cloner ?? throw new ArgumentNullException(nameof(cloner));
        }

        public object GetValue(T row, string columnId)
        {
            return _getter(row, columnId);
        }

        public void SetValue(T row, string columnId, object value)
        {
            _setter(row, columnId, value);
        }

        public T Clone(T row)
        {
            return _cloner(row);
        }
    }
}

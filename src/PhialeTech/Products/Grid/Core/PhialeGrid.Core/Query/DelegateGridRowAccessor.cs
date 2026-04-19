using System;

namespace PhialeGrid.Core.Query
{
    public sealed class DelegateGridRowAccessor<T> : IGridRowAccessor<T>
    {
        private readonly Func<T, string, object> _getter;

        public DelegateGridRowAccessor(Func<T, string, object> getter)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        }

        public object GetValue(T row, string columnId)
        {
            return _getter(row, columnId);
        }
    }
}

namespace PhialeGrid.Core.Editing
{
    public interface IGridRowEditor<T>
    {
        object GetValue(T row, string columnId);

        void SetValue(T row, string columnId, object value);

        T Clone(T row);
    }
}

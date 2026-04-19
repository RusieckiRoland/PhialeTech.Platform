namespace PhialeGrid.Core.Query
{
    public enum GridFilterOperator
    {
        Equals = 0,
        Contains = 1,
        StartsWith = 2,
        GreaterThan = 3,
        LessThan = 4,
        Between = 5,
        IsTrue = 6,
        IsFalse = 7,
        Custom = 8,
    }
}

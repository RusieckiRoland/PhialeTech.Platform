namespace PhialeGis.Library.Domain.Tasking
{
    public enum TaskItemStatus
    {
        Unknown = 0,
        Draft = 1,
        Ready = 2,
        Assigned = 3,
        InProgress = 4,
        OnHold = 5,
        Completed = 6,
        Cancelled = 7,
    }
}

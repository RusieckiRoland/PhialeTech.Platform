namespace PhialeGis.Library.Abstractions.Integrations.Tasking
{
    public enum TaskingExternalStatus
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

namespace PhialeGis.Library.Abstractions.Integrations.Tasking
{
    public sealed class TaskingProviderCapabilities
    {
        public bool CanCreateTasks { get; set; }
        public bool CanUpdateTasks { get; set; }
        public bool CanUpdateStatus { get; set; }
        public bool CanAssignTasks { get; set; }
        public bool CanDeleteTasks { get; set; }
        public bool CanFilterByBounds { get; set; }
        public bool CanStoreGeometry { get; set; }
    }
}

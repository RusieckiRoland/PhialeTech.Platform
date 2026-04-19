namespace PhialeGis.Library.Domain.Tasking
{
    public sealed class TaskAssignee
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string TeamId { get; set; }
        public string TeamName { get; set; }
    }
}

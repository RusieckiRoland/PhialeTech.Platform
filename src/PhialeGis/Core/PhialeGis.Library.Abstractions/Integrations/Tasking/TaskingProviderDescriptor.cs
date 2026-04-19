namespace PhialeGis.Library.Abstractions.Integrations.Tasking
{
    public sealed class TaskingProviderDescriptor
    {
        public TaskingProviderDescriptor()
        {
            Capabilities = new TaskingProviderCapabilities();
        }

        public string ProviderId { get; set; }
        public string DisplayName { get; set; }
        public TaskingProviderCapabilities Capabilities { get; set; }
    }
}

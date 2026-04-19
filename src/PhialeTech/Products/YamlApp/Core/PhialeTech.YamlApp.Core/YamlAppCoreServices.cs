using UniversalInput.Contracts;

namespace PhialeTech.YamlApp.Core
{
    /// <summary>
    /// Placeholder entry point for platform-agnostic YamlApp core services.
    /// </summary>
    public sealed class YamlAppCoreServices
    {
        public UniversalMetadata CreateEmptyMetadata()
        {
            return new UniversalMetadata();
        }
    }
}

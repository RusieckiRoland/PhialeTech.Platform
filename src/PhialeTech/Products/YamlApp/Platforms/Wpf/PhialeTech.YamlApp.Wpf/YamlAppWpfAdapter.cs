using UniversalInput.Contracts;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Wpf.Document;

namespace PhialeTech.YamlApp.Wpf
{
    /// <summary>
    /// Placeholder WPF adapter entry point for the YamlApp family.
    /// </summary>
    public sealed class YamlAppWpfAdapter
    {
        public UniversalMetadata CreateEmptyMetadata()
        {
            return new UniversalMetadata();
        }

        public YamlDocumentHost CreateDocumentHost(RuntimeDocumentState runtimeDocumentState)
        {
            return new YamlDocumentHost
            {
                RuntimeDocumentState = runtimeDocumentState,
            };
        }
    }
}

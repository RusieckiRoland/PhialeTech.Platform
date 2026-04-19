using System;
using System.Collections.Generic;
using System.IO;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Infrastructure.Loading;

namespace PhialeTech.YamlApp.Infrastructure.Configuration
{
    public sealed class FilesystemYamlDocumentConfigurationSource : IDocumentConfigurationSourceWithDiagnostics
    {
        private readonly string _rootDirectory;
        private readonly YamlDocumentDefinitionImporter _importer;

        public FilesystemYamlDocumentConfigurationSource(string rootDirectory)
            : this(rootDirectory, new YamlDocumentDefinitionImporter())
        {
        }

        public FilesystemYamlDocumentConfigurationSource(string rootDirectory, YamlDocumentDefinitionImporter importer)
        {
            _rootDirectory = rootDirectory;
            _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        }

        public IDocumentDefinition GetConfigurationByName(string configurationName)
        {
            IDocumentDefinition configuration;
            IReadOnlyList<string> diagnostics;
            if (!TryGetConfigurationByName(configurationName, out configuration, out diagnostics))
            {
                var message = diagnostics == null || diagnostics.Count == 0
                    ? string.Format("Configuration '{0}' could not be loaded.", configurationName)
                    : string.Join(Environment.NewLine, diagnostics);

                throw new InvalidOperationException(message);
            }

            return configuration;
        }

        public bool TryGetConfigurationByName(string configurationName, out IDocumentDefinition configuration, out IReadOnlyList<string> diagnostics)
        {
            configuration = null;
            diagnostics = Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(configurationName))
            {
                diagnostics = new[] { "Configuration name cannot be empty." };
                return false;
            }

            if (string.IsNullOrWhiteSpace(_rootDirectory))
            {
                diagnostics = new[] { "Configuration root directory was not configured." };
                return false;
            }

            if (!Directory.Exists(_rootDirectory))
            {
                diagnostics = new[] { string.Format("Configuration root directory does not exist: {0}", _rootDirectory) };
                return false;
            }

            var yamlPath = ResolveConfigurationPath(configurationName);
            if (yamlPath == null)
            {
                diagnostics = new[] { string.Format("Configuration '{0}' was not found under '{1}'.", configurationName, _rootDirectory) };
                return false;
            }

            var result = _importer.ImportFromFile(yamlPath);
            diagnostics = result.Diagnostics;
            configuration = result.Definition;
            return result.Success;
        }

        private string ResolveConfigurationPath(string configurationName)
        {
            var directYaml = Path.Combine(_rootDirectory, configurationName + ".yaml");
            if (File.Exists(directYaml))
            {
                return directYaml;
            }

            var directYml = Path.Combine(_rootDirectory, configurationName + ".yml");
            if (File.Exists(directYml))
            {
                return directYml;
            }

            var nestedYaml = Path.Combine(_rootDirectory, configurationName, configurationName + ".yaml");
            if (File.Exists(nestedYaml))
            {
                return nestedYaml;
            }

            var nestedYml = Path.Combine(_rootDirectory, configurationName, configurationName + ".yml");
            if (File.Exists(nestedYml))
            {
                return nestedYml;
            }

            return null;
        }
    }
}



using System;
using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Runtime.Model;

namespace PhialeTech.YamlApp.Runtime.Services
{
    public sealed class PrepareDocumentRuntimeResult
    {
        public PrepareDocumentRuntimeResult(
            string configurationName,
            IDocumentDefinition configuration,
            YamlDocumentNormalizationResult normalization,
            RuntimeDocumentState runtime,
            IReadOnlyList<string> diagnostics)
        {
            ConfigurationName = configurationName;
            Configuration = configuration;
            Normalization = normalization;
            Runtime = runtime;
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public string ConfigurationName { get; }

        public IDocumentDefinition Configuration { get; }

        public YamlDocumentNormalizationResult Normalization { get; }

        public RuntimeDocumentState Runtime { get; }

        public IReadOnlyList<string> Diagnostics { get; }

        public bool Success => Diagnostics.Count == 0 && Configuration != null && Normalization != null && Normalization.Success && Runtime != null;
    }
}


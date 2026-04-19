using System;
using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Core.Resolved;

namespace PhialeTech.YamlApp.Runtime.Services
{
    public sealed class DocumentRuntimePreparationService
    {
        private readonly IDocumentConfigurationSource _configurationSource;
        private readonly IDocumentConfigurationSourceWithDiagnostics _configurationSourceWithDiagnostics;
        private readonly YamlDocumentDefinitionNormalizer _normalizer;
        private readonly RuntimeDocumentStateFactory _runtimeFactory;
        private readonly RuntimeDocumentJsonMapper _jsonMapper;

        public DocumentRuntimePreparationService(
            IDocumentConfigurationSource configurationSource,
            YamlDocumentDefinitionNormalizer normalizer,
            RuntimeDocumentStateFactory runtimeFactory,
            RuntimeDocumentJsonMapper jsonMapper)
        {
            _configurationSource = configurationSource ?? throw new ArgumentNullException(nameof(configurationSource));
            _configurationSourceWithDiagnostics = configurationSource as IDocumentConfigurationSourceWithDiagnostics;
            _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
            _runtimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
            _jsonMapper = jsonMapper ?? throw new ArgumentNullException(nameof(jsonMapper));
        }

        public PrepareDocumentRuntimeResult Prepare(string configurationName, string inputJson = null)
        {
            var diagnostics = new List<string>();

            if (string.IsNullOrWhiteSpace(configurationName))
            {
                diagnostics.Add("Configuration name cannot be empty.");
                return new PrepareDocumentRuntimeResult(configurationName, null, null, null, diagnostics);
            }

            IDocumentDefinition configuration;
            if (!TryLoadConfiguration(configurationName, out configuration, diagnostics))
            {
                return new PrepareDocumentRuntimeResult(configurationName, null, null, null, diagnostics);
            }

            var normalized = _normalizer.Normalize(configuration as YamlDocumentDefinition);
            foreach (var diagnostic in normalized.Diagnostics)
            {
                diagnostics.Add(diagnostic);
            }

            if (!normalized.Success)
            {
                return new PrepareDocumentRuntimeResult(configurationName, configuration, normalized, null, diagnostics);
            }

            var resolvedForm = normalized.ResolvedDocument as ResolvedFormDocumentDefinition;
            if (resolvedForm == null)
            {
                diagnostics.Add("Only form documents can be prepared for runtime.");
                return new PrepareDocumentRuntimeResult(configurationName, configuration, normalized, null, diagnostics);
            }

            var runtime = string.IsNullOrWhiteSpace(inputJson)
                ? _runtimeFactory.Create(resolvedForm)
                : _jsonMapper.CreateFromJson(resolvedForm, inputJson);

            return new PrepareDocumentRuntimeResult(configurationName, configuration, normalized, runtime, diagnostics);
        }

        private bool TryLoadConfiguration(string configurationName, out IDocumentDefinition configuration, List<string> diagnostics)
        {
            configuration = null;

            if (_configurationSourceWithDiagnostics != null)
            {
                IReadOnlyList<string> sourceDiagnostics;
                if (_configurationSourceWithDiagnostics.TryGetConfigurationByName(configurationName, out configuration, out sourceDiagnostics))
                {
                    return true;
                }

                if (sourceDiagnostics != null)
                {
                    foreach (var diagnostic in sourceDiagnostics)
                    {
                        diagnostics.Add(diagnostic);
                    }
                }

                return false;
            }

            try
            {
                configuration = _configurationSource.GetConfigurationByName(configurationName);
                return configuration != null;
            }
            catch (Exception ex)
            {
                diagnostics.Add(ex.Message);
                return false;
            }
        }
    }
}



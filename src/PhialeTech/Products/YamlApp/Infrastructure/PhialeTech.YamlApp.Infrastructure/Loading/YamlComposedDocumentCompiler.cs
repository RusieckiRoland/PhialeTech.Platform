using PhialeTech.YamlApp.Definitions.Documents;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PhialeTech.YamlApp.Infrastructure.Loading
{
    public sealed class YamlComposedDocumentCompiler
    {
        private static readonly HashSet<string> SupportedDocumentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "name",
            "kind",
            "width",
            "widthHint",
            "weight",
            "visible",
            "enabled",
            "showOldValueRestoreButton",
            "validationTrigger",
            "interactionMode",
            "densityMode",
            "fieldChromeMode",
            "captionPlacement",
            "layout",
            "actionAreas",
            "actions"
        };

        private static readonly HashSet<string> SupportedFieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "name",
            "control",
            "valueType",
            "caption",
            "captionKey",
            "placeholder",
            "placeholderKey",
            "width",
            "widthHint",
            "weight",
            "visible",
            "enabled",
            "showOldValueRestoreButton",
            "required",
            "showLabel",
            "showPlaceholder",
            "maxLength",
            "minValue",
            "maxValue",
            "validationTrigger",
            "interactionMode",
            "densityMode",
            "fieldChromeMode",
            "captionPlacement",
            "value",
            "oldValue"
        };

        public YamlDefinitionImportResult<YamlDocumentDefinition> Compile(string yaml, IEnumerable<Assembly> libraries, string languageCode)
        {
            var diagnostics = new List<string>();

            if (string.IsNullOrWhiteSpace(yaml))
            {
                return new YamlDefinitionImportResult<YamlDocumentDefinition>(null, new[] { "YAML content cannot be empty." });
            }

            try
            {
                var repository = EmbeddedYamlLibraryRepository.Load(libraries, NormalizeLanguage(languageCode), diagnostics);
                var entryModule = ParseModule(yaml, diagnostics, "inline");
                if (entryModule == null)
                {
                    return new YamlDefinitionImportResult<YamlDocumentDefinition>(null, diagnostics);
                }

                var namespaceIndex = new Dictionary<string, ComposedYamlModule>(repository.Modules, StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(entryModule.Namespace))
                {
                    namespaceIndex[entryModule.Namespace] = entryModule;
                }

                string documentKey;
                var documentNode = ResolveEntryDocument(entryModule, diagnostics, out documentKey);
                if (documentNode == null)
                {
                    return new YamlDefinitionImportResult<YamlDocumentDefinition>(null, diagnostics);
                }

                var visibleNamespaces = CollectVisibleNamespaces(entryModule, namespaceIndex, diagnostics);
                var definitionIndex = BuildDefinitionIndex(visibleNamespaces, namespaceIndex, diagnostics);
                var legacyRoot = BuildLegacyRoot(documentKey, documentNode, definitionIndex, repository.Localization, diagnostics);
                if (legacyRoot == null)
                {
                    return new YamlDefinitionImportResult<YamlDocumentDefinition>(null, diagnostics);
                }

                var importer = new YamlDocumentDefinitionImporter();
                var transformedYaml = SerializeNode(legacyRoot);
                var importResult = importer.Import(transformedYaml);
                diagnostics.AddRange(importResult.Diagnostics);
                return new YamlDefinitionImportResult<YamlDocumentDefinition>(importResult.Definition, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.Add(FormatYamlFailure("Failed to compile composed YAML document", ex));
                return new YamlDefinitionImportResult<YamlDocumentDefinition>(null, diagnostics);
            }
        }

        private static string FormatYamlFailure(string prefix, Exception ex)
        {
            if (ex is YamlException yamlException)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} at line {1}, column {2}: {3}",
                    prefix,
                    yamlException.Start.Line,
                    yamlException.Start.Column,
                    yamlException.Message);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", prefix, ex.Message);
        }

        private static ComposedYamlModule ParseModule(string yaml, IList<string> diagnostics, string sourceName)
        {
            var stream = new YamlStream();
            using (var reader = new StringReader(yaml))
            {
                stream.Load(reader);
            }

            if (stream.Documents.Count == 0 || !(stream.Documents[0].RootNode is YamlMappingNode root))
            {
                diagnostics.Add("YAML root document must be a mapping.");
                return null;
            }

            if (!TryGetScalar(root, "namespace", out var moduleNamespace) || string.IsNullOrWhiteSpace(moduleNamespace))
            {
                diagnostics.Add(string.Format(CultureInfo.InvariantCulture, "Module '{0}' is missing required property 'namespace'.", sourceName));
                return null;
            }

            var module = new ComposedYamlModule
            {
                Namespace = moduleNamespace,
                SourceName = sourceName
            };

            if (TryGetSequence(root, "imports", out var importsNode))
            {
                foreach (var child in importsNode.Children)
                {
                    if (child is YamlScalarNode scalar && !string.IsNullOrWhiteSpace(scalar.Value))
                    {
                        module.Imports.Add(scalar.Value);
                    }
                }
            }

            if (TryGetMapping(root, "definitions", out var definitionsNode))
            {
                foreach (var child in definitionsNode.Children)
                {
                    if (child.Key is YamlScalarNode key && child.Value is YamlMappingNode value && !string.IsNullOrWhiteSpace(key.Value))
                    {
                        module.Definitions[key.Value] = value;
                    }
                }
            }

            YamlMappingNode singleDocumentNode = null;
            YamlSequenceNode documentsSequence = null;
            YamlMappingNode documentsNode = null;
            var hasSingleDocument = TryGetMapping(root, "document", out singleDocumentNode);
            var hasDocumentSequence = TryGetSequence(root, "documents", out documentsSequence);
            var hasDocumentMapping = !hasDocumentSequence && TryGetMapping(root, "documents", out documentsNode);

            if (hasSingleDocument && (hasDocumentSequence || hasDocumentMapping))
            {
                diagnostics.Add("Module cannot contain both 'document' and 'documents'.");
            }

            if (hasSingleDocument)
            {
                var documentId = ReadScalar(singleDocumentNode, "id");
                if (string.IsNullOrWhiteSpace(documentId))
                {
                    diagnostics.Add("Property 'document' is missing required property 'id'.");
                }
                else
                {
                    module.Documents[documentId] = singleDocumentNode;
                }
            }
            else if (hasDocumentSequence)
            {
                foreach (var child in documentsSequence.Children)
                {
                    if (!(child is YamlMappingNode value))
                    {
                        diagnostics.Add("Each document in 'documents' must be a mapping.");
                        continue;
                    }

                    var documentId = ReadScalar(value, "id");
                    if (string.IsNullOrWhiteSpace(documentId))
                    {
                        diagnostics.Add("Each document in 'documents' is missing required property 'id'.");
                        continue;
                    }

                    if (module.Documents.ContainsKey(documentId))
                    {
                        diagnostics.Add(string.Format(CultureInfo.InvariantCulture, "Duplicate document id '{0}' was found in module '{1}'.", documentId, sourceName));
                        continue;
                    }

                    module.Documents[documentId] = value;
                }
            }
            else if (hasDocumentMapping)
            {
                foreach (var child in documentsNode.Children)
                {
                    if (child.Key is YamlScalarNode key && child.Value is YamlMappingNode value && !string.IsNullOrWhiteSpace(key.Value))
                    {
                        if (string.IsNullOrWhiteSpace(ReadScalar(value, "id")))
                        {
                            SetScalar(value, "id", key.Value);
                        }

                        module.Documents[key.Value] = value;
                    }
                }
            }

            return module;
        }

        private static YamlMappingNode ResolveEntryDocument(ComposedYamlModule module, IList<string> diagnostics, out string documentKey)
        {
            documentKey = null;
            if (module == null || module.Documents.Count == 0)
            {
                diagnostics.Add("Composed YAML must contain a 'documents' section with at least one document.");
                return null;
            }

            if (module.Documents.Count > 1)
            {
                diagnostics.Add("Composed YAML demo currently supports exactly one document per source file.");
                return null;
            }

            var pair = module.Documents.First();
            documentKey = pair.Key;
            return pair.Value;
        }

        private static HashSet<string> CollectVisibleNamespaces(
            ComposedYamlModule entryModule,
            IDictionary<string, ComposedYamlModule> namespaceIndex,
            IList<string> diagnostics)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Visit(string namespaceName)
            {
                if (string.IsNullOrWhiteSpace(namespaceName) || !result.Add(namespaceName))
                {
                    return;
                }

                if (!namespaceIndex.TryGetValue(namespaceName, out var module))
                {
                    diagnostics.Add(string.Format(CultureInfo.InvariantCulture, "Imported namespace '{0}' could not be resolved.", namespaceName));
                    return;
                }

                foreach (var import in module.Imports)
                {
                    Visit(import);
                }
            }

            Visit(entryModule.Namespace);
            foreach (var import in entryModule.Imports)
            {
                Visit(import);
            }

            return result;
        }

        private static Dictionary<string, QualifiedDefinitionNode> BuildDefinitionIndex(
            HashSet<string> visibleNamespaces,
            IDictionary<string, ComposedYamlModule> namespaceIndex,
            IList<string> diagnostics)
        {
            var result = new Dictionary<string, QualifiedDefinitionNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var namespaceName in visibleNamespaces)
            {
                if (!namespaceIndex.TryGetValue(namespaceName, out var module))
                {
                    continue;
                }

                foreach (var pair in module.Definitions)
                {
                    var fullName = namespaceName + "." + pair.Key;
                    if (result.ContainsKey(fullName))
                    {
                        diagnostics.Add(string.Format(CultureInfo.InvariantCulture, "Duplicate definition '{0}' was found.", fullName));
                        continue;
                    }

                    result[fullName] = new QualifiedDefinitionNode(namespaceName, pair.Key, pair.Value);
                }
            }

            return result;
        }

        private static YamlMappingNode BuildLegacyRoot(
            string documentKey,
            YamlMappingNode documentNode,
            IDictionary<string, QualifiedDefinitionNode> definitionIndex,
            IDictionary<string, string> localization,
            IList<string> diagnostics)
        {
            var root = new YamlMappingNode();
            SetScalar(root, "id", ReadScalar(documentNode, "id") ?? documentKey);

            foreach (var child in documentNode.Children)
            {
                if (!(child.Key is YamlScalarNode key) || string.IsNullOrWhiteSpace(key.Value))
                {
                    continue;
                }

                if (string.Equals(key.Value, "fields", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(key.Value, "id", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(key.Value, "actionAreas", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(key.Value, "actions", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!SupportedDocumentKeys.Contains(key.Value))
                {
                    continue;
                }

                root.Children.Add(new YamlScalarNode(key.Value), LocalizeNode(DeepClone(child.Value), localization));
            }

            var fieldsMapping = BuildLegacyFields(documentNode, definitionIndex, localization, diagnostics);
            root.Children.Add(new YamlScalarNode("fields"), fieldsMapping);
            var actionAreasSequence = BuildLegacyActionAreas(documentNode, definitionIndex, localization, diagnostics);
            if (actionAreasSequence.Children.Count > 0)
            {
                root.Children.Add(new YamlScalarNode("actionAreas"), actionAreasSequence);
            }

            var actionsSequence = BuildLegacyActions(documentNode, definitionIndex, localization, diagnostics);
            if (actionsSequence.Children.Count > 0)
            {
                root.Children.Add(new YamlScalarNode("actions"), actionsSequence);
            }
            return root;
        }

        private static YamlMappingNode BuildLegacyFields(
            YamlMappingNode documentNode,
            IDictionary<string, QualifiedDefinitionNode> definitionIndex,
            IDictionary<string, string> localization,
            IList<string> diagnostics)
        {
            var result = new YamlMappingNode();

            if (!TryGetSequence(documentNode, "fields", out var fieldsNode))
            {
                diagnostics.Add("Document is missing required property 'fields'.");
                return result;
            }

            foreach (var child in fieldsNode.Children)
            {
                string fieldId;
                YamlMappingNode fieldItem;

                if (child is YamlScalarNode scalar && !string.IsNullOrWhiteSpace(scalar.Value))
                {
                    fieldId = scalar.Value;
                    fieldItem = new YamlMappingNode
                    {
                        { new YamlScalarNode("id"), new YamlScalarNode(fieldId) },
                        { new YamlScalarNode("extends"), new YamlScalarNode(fieldId) }
                    };
                }
                else if (child is YamlMappingNode mapping)
                {
                    fieldItem = mapping;
                    fieldId = ReadScalar(mapping, "id");
                }
                else
                {
                    diagnostics.Add("Each document field must be either a scalar id or a mapping.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fieldId))
                {
                    diagnostics.Add("Document field is missing required property 'id'.");
                    continue;
                }

                var extends = ReadScalar(fieldItem, "extends");
                if (string.IsNullOrWhiteSpace(extends))
                {
                    var inferredDefinition = ResolveDefinitionReference(fieldId, definitionIndex, diagnostics: null);
                    if (inferredDefinition != null)
                    {
                        extends = fieldId;
                    }
                    else
                    {
                        diagnostics.Add(string.Format(CultureInfo.InvariantCulture, "Document field '{0}' is missing required property 'extends', and no definition named '{0}' was found.", fieldId));
                        continue;
                    }
                }

                var resolvedDefinition = ResolveDefinitionNode(extends, definitionIndex, new HashSet<string>(StringComparer.OrdinalIgnoreCase), diagnostics);
                var merged = MergeMappings(resolvedDefinition, fieldItem);
                RemoveKey(merged, "extends");
                SetScalar(merged, "id", fieldId);
                LocalizeFieldKeys(merged, localization);
                result.Children.Add(new YamlScalarNode(fieldId), FilterFieldKeys(merged));
            }

            return result;
        }

        private static YamlSequenceNode BuildLegacyActionAreas(
            YamlMappingNode documentNode,
            IDictionary<string, QualifiedDefinitionNode> definitionIndex,
            IDictionary<string, string> localization,
            IList<string> diagnostics)
        {
            var result = new YamlSequenceNode();
            if (!TryGetSequence(documentNode, "actionAreas", out var actionAreasNode))
            {
                return result;
            }

            foreach (var child in actionAreasNode.Children)
            {
                if (!(child is YamlMappingNode mapping))
                {
                    diagnostics.Add("Each document action area must be a mapping.");
                    continue;
                }

                var actionAreaId = ReadScalar(mapping, "id");
                if (string.IsNullOrWhiteSpace(actionAreaId))
                {
                    diagnostics.Add("Document action area is missing required property 'id'.");
                    continue;
                }

                var extends = ReadScalar(mapping, "extends");
                var merged = string.IsNullOrWhiteSpace(extends)
                    ? DeepClone(mapping) as YamlMappingNode ?? new YamlMappingNode()
                    : MergeMappings(
                        ResolveDefinitionNode(extends, definitionIndex, new HashSet<string>(StringComparer.OrdinalIgnoreCase), diagnostics),
                        mapping);
                RemoveKey(merged, "extends");
                SetScalar(merged, "id", actionAreaId);
                result.Add(LocalizeNode(merged, localization));
            }

            return result;
        }

        private static YamlSequenceNode BuildLegacyActions(
            YamlMappingNode documentNode,
            IDictionary<string, QualifiedDefinitionNode> definitionIndex,
            IDictionary<string, string> localization,
            IList<string> diagnostics)
        {
            var result = new YamlSequenceNode();
            if (!TryGetSequence(documentNode, "actions", out var actionsNode))
            {
                return result;
            }

            foreach (var child in actionsNode.Children)
            {
                if (!(child is YamlMappingNode mapping))
                {
                    diagnostics.Add("Each document action must be a mapping.");
                    continue;
                }

                var actionId = ReadScalar(mapping, "id");
                if (string.IsNullOrWhiteSpace(actionId))
                {
                    diagnostics.Add("Document action is missing required property 'id'.");
                    continue;
                }

                var extends = ReadScalar(mapping, "extends");
                var merged = string.IsNullOrWhiteSpace(extends)
                    ? DeepClone(mapping) as YamlMappingNode ?? new YamlMappingNode()
                    : MergeMappings(
                        ResolveDefinitionNode(extends, definitionIndex, new HashSet<string>(StringComparer.OrdinalIgnoreCase), diagnostics),
                        mapping);
                RemoveKey(merged, "extends");
                SetScalar(merged, "id", actionId);
                result.Add(LocalizeNode(merged, localization));
            }

            return result;
        }

        private static YamlMappingNode ResolveDefinitionNode(
            string reference,
            IDictionary<string, QualifiedDefinitionNode> definitionIndex,
            ISet<string> resolutionStack,
            IList<string> diagnostics)
        {
            var definition = ResolveDefinitionReference(reference, definitionIndex, diagnostics);
            if (definition == null)
            {
                return new YamlMappingNode();
            }

            var fullName = definition.FullName;
            if (!resolutionStack.Add(fullName))
            {
                diagnostics.Add(string.Format(CultureInfo.InvariantCulture, "Cyclic definition inheritance was detected for '{0}'.", fullName));
                return DeepClone(definition.Node) as YamlMappingNode ?? new YamlMappingNode();
            }

            try
            {
                var current = DeepClone(definition.Node) as YamlMappingNode ?? new YamlMappingNode();
                var parentReference = ReadScalar(current, "extends");
                if (string.IsNullOrWhiteSpace(parentReference))
                {
                    return current;
                }

                var parent = ResolveDefinitionNode(parentReference, definitionIndex, resolutionStack, diagnostics);
                return MergeMappings(parent, current);
            }
            finally
            {
                resolutionStack.Remove(fullName);
            }
        }

        private static QualifiedDefinitionNode ResolveDefinitionReference(
            string reference,
            IDictionary<string, QualifiedDefinitionNode> definitionIndex,
            IList<string> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            if (definitionIndex.TryGetValue(reference, out var fullyQualified))
            {
                return fullyQualified;
            }

            var matches = definitionIndex.Values
                .Where(item => string.Equals(item.DefinitionId, reference, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0];
            }

            if (matches.Count > 1)
            {
                diagnostics?.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "Definition reference '{0}' is ambiguous. Matches: {1}.",
                    reference,
                    string.Join(", ", matches.Select(item => item.FullName))));
                return matches[0];
            }

            diagnostics?.Add(string.Format(CultureInfo.InvariantCulture, "Definition '{0}' could not be resolved.", reference));
            return null;
        }

        private static YamlMappingNode MergeMappings(YamlMappingNode baseNode, YamlMappingNode overrideNode)
        {
            var result = DeepClone(baseNode) as YamlMappingNode ?? new YamlMappingNode();
            if (overrideNode == null)
            {
                return result;
            }

            foreach (var child in overrideNode.Children)
            {
                if (!(child.Key is YamlScalarNode key) || string.IsNullOrWhiteSpace(key.Value))
                {
                    continue;
                }

                SetNode(result, key.Value, DeepClone(child.Value));
            }

            return result;
        }

        private static YamlMappingNode FilterFieldKeys(YamlMappingNode source)
        {
            var result = new YamlMappingNode();
            foreach (var child in source.Children)
            {
                if (child.Key is YamlScalarNode key && !string.IsNullOrWhiteSpace(key.Value) && SupportedFieldKeys.Contains(key.Value))
                {
                    result.Children.Add(new YamlScalarNode(key.Value), DeepClone(child.Value));
                }
            }

            return result;
        }

        private static void LocalizeFieldKeys(YamlMappingNode node, IDictionary<string, string> localization)
        {
            LocalizeScalar(node, "captionKey", localization);
            LocalizeScalar(node, "placeholderKey", localization);
        }

        private static YamlNode LocalizeNode(YamlNode node, IDictionary<string, string> localization)
        {
            if (node is YamlMappingNode mapping)
            {
                var localized = new YamlMappingNode();
                foreach (var child in mapping.Children)
                {
                    var key = child.Key as YamlScalarNode;
                    var keyValue = key == null ? null : key.Value;
                    if (!string.IsNullOrWhiteSpace(keyValue) &&
                        (string.Equals(keyValue, "captionKey", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(keyValue, "placeholderKey", StringComparison.OrdinalIgnoreCase)))
                    {
                        localized.Children.Add(new YamlScalarNode(keyValue), LocalizeScalarNode(child.Value, localization));
                        continue;
                    }

                    localized.Children.Add(DeepClone(child.Key), LocalizeNode(child.Value, localization));
                }

                return localized;
            }

            if (node is YamlSequenceNode sequence)
            {
                var localized = new YamlSequenceNode();
                foreach (var child in sequence.Children)
                {
                    localized.Children.Add(LocalizeNode(child, localization));
                }

                return localized;
            }

            return DeepClone(node);
        }

        private static YamlNode LocalizeScalarNode(YamlNode node, IDictionary<string, string> localization)
        {
            if (!(node is YamlScalarNode scalar) || string.IsNullOrWhiteSpace(scalar.Value))
            {
                return DeepClone(node);
            }

            if (localization != null && localization.TryGetValue(scalar.Value, out var localized))
            {
                return new YamlScalarNode(localized);
            }

            return new YamlScalarNode(scalar.Value);
        }

        private static void LocalizeScalar(YamlMappingNode node, string key, IDictionary<string, string> localization)
        {
            if (!TryGetNode(node, key, out var child) || !(child is YamlScalarNode scalar) || string.IsNullOrWhiteSpace(scalar.Value))
            {
                return;
            }

            if (localization != null && localization.TryGetValue(scalar.Value, out var localized))
            {
                SetScalar(node, key, localized);
            }
        }

        private static string SerializeNode(YamlNode node)
        {
            var stream = new YamlStream(new YamlDocument(node));
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                stream.Save(writer, false);
                return writer.ToString();
            }
        }

        private static void AddScalar(YamlMappingNode node, string key, string value)
        {
            if (node == null || string.IsNullOrWhiteSpace(key) || value == null)
            {
                return;
            }

            node.Children.Add(new YamlScalarNode(key), new YamlScalarNode(value));
        }

        private static void SetScalar(YamlMappingNode node, string key, string value)
        {
            SetNode(node, key, new YamlScalarNode(value ?? string.Empty));
        }

        private static void SetNode(YamlMappingNode node, string key, YamlNode value)
        {
            RemoveKey(node, key);
            node.Children.Add(new YamlScalarNode(key), value ?? new YamlScalarNode(string.Empty));
        }

        private static void RemoveKey(YamlMappingNode node, string key)
        {
            if (node == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var existing = node.Children.Keys
                .OfType<YamlScalarNode>()
                .FirstOrDefault(item => string.Equals(item.Value, key, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                node.Children.Remove(existing);
            }
        }

        private static bool TryGetNode(YamlMappingNode node, string key, out YamlNode value)
        {
            value = null;
            if (node == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            foreach (var child in node.Children)
            {
                if (child.Key is YamlScalarNode scalar && string.Equals(scalar.Value, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = child.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetMapping(YamlMappingNode node, string key, out YamlMappingNode value)
        {
            value = null;
            if (!TryGetNode(node, key, out var child) || !(child is YamlMappingNode mapping))
            {
                return false;
            }

            value = mapping;
            return true;
        }

        private static bool TryGetSequence(YamlMappingNode node, string key, out YamlSequenceNode value)
        {
            value = null;
            if (!TryGetNode(node, key, out var child) || !(child is YamlSequenceNode sequence))
            {
                return false;
            }

            value = sequence;
            return true;
        }

        private static bool TryGetScalar(YamlMappingNode node, string key, out string value)
        {
            value = null;
            if (!TryGetNode(node, key, out var child) || !(child is YamlScalarNode scalar))
            {
                return false;
            }

            value = scalar.Value;
            return true;
        }

        private static string ReadScalar(YamlMappingNode node, string key)
        {
            return TryGetScalar(node, key, out var value) ? value : null;
        }

        private static YamlNode DeepClone(YamlNode node)
        {
            if (node is YamlScalarNode scalar)
            {
                return new YamlScalarNode(scalar.Value);
            }

            if (node is YamlSequenceNode sequence)
            {
                var clone = new YamlSequenceNode();
                foreach (var child in sequence.Children)
                {
                    clone.Add(DeepClone(child));
                }

                return clone;
            }

            if (node is YamlMappingNode mapping)
            {
                var clone = new YamlMappingNode();
                foreach (var child in mapping.Children)
                {
                    clone.Add(DeepClone(child.Key), DeepClone(child.Value));
                }

                return clone;
            }

            return new YamlScalarNode(string.Empty);
        }

        private static string NormalizeLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return "en";
            }

            var normalized = languageCode.Trim().ToLowerInvariant();
            if (normalized.StartsWith("pl", StringComparison.OrdinalIgnoreCase))
            {
                return "pl";
            }

            return "en";
        }

        private sealed class ComposedYamlModule
        {
            public string Namespace { get; set; }

            public string SourceName { get; set; }

            public List<string> Imports { get; } = new List<string>();

            public Dictionary<string, YamlMappingNode> Definitions { get; } = new Dictionary<string, YamlMappingNode>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, YamlMappingNode> Documents { get; } = new Dictionary<string, YamlMappingNode>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class QualifiedDefinitionNode
        {
            public QualifiedDefinitionNode(string @namespace, string definitionId, YamlMappingNode node)
            {
                Namespace = @namespace;
                DefinitionId = definitionId;
                Node = node;
            }

            public string Namespace { get; }

            public string DefinitionId { get; }

            public YamlMappingNode Node { get; }

            public string FullName => Namespace + "." + DefinitionId;
        }

        private sealed class EmbeddedYamlLibraryRepository
        {
            public Dictionary<string, ComposedYamlModule> Modules { get; } = new Dictionary<string, ComposedYamlModule>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, string> Localization { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public static EmbeddedYamlLibraryRepository Load(IEnumerable<Assembly> assemblies, string languageCode, IList<string> diagnostics)
            {
                var repository = new EmbeddedYamlLibraryRepository();
                var assemblyList = (assemblies ?? Array.Empty<Assembly>()).Where(item => item != null).Distinct().ToList();

                foreach (var assembly in assemblyList)
                {
                    foreach (var resourceName in assembly.GetManifestResourceNames())
                    {
                        if (!resourceName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var content = ReadResourceText(assembly, resourceName);
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            continue;
                        }

                        if (resourceName.IndexOf(".Localization.", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (resourceName.EndsWith("." + languageCode + ".yaml", StringComparison.OrdinalIgnoreCase) ||
                                (string.Equals(languageCode, "pl", StringComparison.OrdinalIgnoreCase) == false &&
                                 resourceName.EndsWith(".en.yaml", StringComparison.OrdinalIgnoreCase)))
                            {
                                MergeLocalization(repository.Localization, content);
                            }

                            continue;
                        }

                        var module = ParseModule(content, diagnostics, resourceName);
                        if (module == null)
                        {
                            continue;
                        }

                        repository.Modules[module.Namespace] = module;
                    }
                }

                return repository;
            }

            private static string ReadResourceText(Assembly assembly, string resourceName)
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            private static void MergeLocalization(IDictionary<string, string> target, string yaml)
            {
                var stream = new YamlStream();
                using (var reader = new StringReader(yaml))
                {
                    stream.Load(reader);
                }

                if (stream.Documents.Count == 0 || !(stream.Documents[0].RootNode is YamlMappingNode root))
                {
                    return;
                }

                foreach (var child in root.Children)
                {
                    if (child.Key is YamlScalarNode key &&
                        child.Value is YamlScalarNode value &&
                        !string.IsNullOrWhiteSpace(key.Value))
                    {
                        target[key.Value] = value.Value ?? string.Empty;
                    }
                }
            }
        }
    }
}

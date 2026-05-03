using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Definitions.Fields;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Definitions.Layouts;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Core;

namespace PhialeTech.YamlApp.Infrastructure.Loading
{
    public sealed class YamlDocumentDefinitionImporter
    {
        public YamlDefinitionImportResult<YamlDocumentDefinition> ImportFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Failure("YAML file path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                return Failure(string.Format(CultureInfo.InvariantCulture, "YAML file was not found: {0}", filePath));
            }

            return Import(File.ReadAllText(filePath));
        }

        public YamlDefinitionImportResult<YamlDocumentDefinition> Import(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml))
            {
                return Failure("YAML content cannot be empty.");
            }

            var diagnostics = new List<string>();

            try
            {
                var stream = new YamlStream();
                using (var reader = new StringReader(yaml))
                {
                    stream.Load(reader);
                }

                if (stream.Documents.Count == 0 || !(stream.Documents[0].RootNode is YamlMappingNode root))
                {
                    return Failure("YAML root document must be a mapping.");
                }

                var form = ParseForm(root, diagnostics);
                return new YamlDefinitionImportResult<YamlDocumentDefinition>(form, diagnostics);
            }
            catch (Exception ex)
            {
                return Failure(FormatYamlFailure("Failed to import YAML form definition", ex));
            }
        }

        private static YamlDocumentDefinition ParseForm(YamlMappingNode root, List<string> diagnostics)
        {
            var kind = ReadNullableEnum<DocumentKind>(root, "kind") ?? DocumentKind.Form;

            ValidateAllowedKeys(
                root,
                "Document",
                diagnostics,
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
                "topRegionChrome",
                "bottomRegionChrome",
                "header",
                "footer",
                "actionAreas",
                "fields",
                "layout",
                "actions");

            var form = kind == DocumentKind.Form
                ? (YamlDocumentDefinition)new YamlFormDocumentDefinition()
                : new YamlDocumentDefinition();

            form.Id = ReadScalar(root, "id");
            form.Name = ReadScalar(root, "name");
            form.Kind = kind;
            form.Width = ReadNullableDouble(root, "width");
            form.WidthHint = ReadNullableEnum<FieldWidthHint>(root, "widthHint");
            form.Weight = ReadNullableDouble(root, "weight");
            form.Visible = ReadNullableBoolean(root, "visible");
            form.Enabled = ReadNullableBoolean(root, "enabled");
            form.ShowOldValueRestoreButton = ReadNullableBoolean(root, "showOldValueRestoreButton");
            form.ValidationTrigger = ReadNullableEnum<ValidationTrigger>(root, "validationTrigger");
            form.InteractionMode = ReadNullableEnum<InteractionMode>(root, "interactionMode");
            form.DensityMode = ReadNullableEnum<DensityMode>(root, "densityMode");
            form.FieldChromeMode = ReadNullableEnum<FieldChromeMode>(root, "fieldChromeMode");
            form.CaptionPlacement = ReadNullableEnum<CaptionPlacement>(root, "captionPlacement");
            form.TopRegionChrome = ReadNullableEnum<DocumentRegionChromeMode>(root, "topRegionChrome");
            form.BottomRegionChrome = ReadNullableEnum<DocumentRegionChromeMode>(root, "bottomRegionChrome");
            form.Header = ReadHeader(root, diagnostics);
            form.Footer = ReadFooter(root, diagnostics);
            form.Layout = ReadLayout(root, diagnostics);

            ValidateNullableEnumValue<FieldWidthHint>(root, "widthHint", "Document", diagnostics);
            ValidateNullableEnumValue<ValidationTrigger>(root, "validationTrigger", "Document", diagnostics);
            ValidateNullableEnumValue<InteractionMode>(root, "interactionMode", "Document", diagnostics);
            ValidateNullableEnumValue<DensityMode>(root, "densityMode", "Document", diagnostics);
            ValidateNullableEnumValue<FieldChromeMode>(root, "fieldChromeMode", "Document", diagnostics);
            ValidateNullableEnumValue<CaptionPlacement>(root, "captionPlacement", "Document", diagnostics);
            ValidateNullableEnumValue<DocumentRegionChromeMode>(root, "topRegionChrome", "Document", diagnostics);
            ValidateNullableEnumValue<DocumentRegionChromeMode>(root, "bottomRegionChrome", "Document", diagnostics);
            ValidateNullableEnumValue<DocumentKind>(root, "kind", "Document", diagnostics);
            ValidateExclusiveWidth("Document", form.Width, form.WidthHint, diagnostics);

            if (string.IsNullOrWhiteSpace(form.Name))
            {
                form.Name = form.Id;
            }

            if (kind == DocumentKind.Form)
            {
                var formDocument = (YamlFormDocumentDefinition)form;
                foreach (var actionArea in ReadActionAreas(root, diagnostics))
                {
                    formDocument.ActionAreas.Add(actionArea);
                }

                foreach (var field in ReadFields(root, diagnostics))
                {
                    formDocument.Fields.Add(field);
                }

                foreach (var action in ReadActions(root, diagnostics))
                {
                    formDocument.Actions.Add(action);
                }
            }
            else
            {
                if (TryGetMappingChild(root, "actionAreas", out _))
                {
                    diagnostics.Add("Document kind does not support 'actionAreas'.");
                }

                if (TryGetMappingChild(root, "fields", out _))
                {
                    diagnostics.Add("Document kind does not support 'fields'.");
                }

                if (TryGetMappingChild(root, "actions", out _))
                {
                    diagnostics.Add("Document kind does not support 'actions'.");
                }
            }

            return form;
        }

        private static YamlDocumentHeaderDefinition ReadHeader(YamlMappingNode root, List<string> diagnostics)
        {
            if (!TryGetMappingChild(root, "header", out var headerNode) || !(headerNode is YamlMappingNode headerMapping))
            {
                return null;
            }

            ValidateAllowedKeys(
                headerMapping,
                "Document header",
                diagnostics,
                "title",
                "titleKey",
                "subtitle",
                "subtitleKey",
                "description",
                "descriptionKey",
                "status",
                "statusKey",
                "context",
                "contextKey",
                "icon",
                "iconKey",
                "visible");

            return new YamlDocumentHeaderDefinition
            {
                TitleKey = FirstNonEmpty(ReadScalar(headerMapping, "titleKey"), ReadScalar(headerMapping, "title")),
                SubtitleKey = FirstNonEmpty(ReadScalar(headerMapping, "subtitleKey"), ReadScalar(headerMapping, "subtitle")),
                DescriptionKey = FirstNonEmpty(ReadScalar(headerMapping, "descriptionKey"), ReadScalar(headerMapping, "description")),
                StatusKey = FirstNonEmpty(ReadScalar(headerMapping, "statusKey"), ReadScalar(headerMapping, "status")),
                ContextKey = FirstNonEmpty(ReadScalar(headerMapping, "contextKey"), ReadScalar(headerMapping, "context")),
                IconKey = FirstNonEmpty(ReadScalar(headerMapping, "iconKey"), ReadScalar(headerMapping, "icon")),
                Visible = ReadNullableBoolean(headerMapping, "visible")
            };
        }

        private static YamlDocumentFooterDefinition ReadFooter(YamlMappingNode root, List<string> diagnostics)
        {
            if (!TryGetMappingChild(root, "footer", out var footerNode) || !(footerNode is YamlMappingNode footerMapping))
            {
                return null;
            }

            ValidateAllowedKeys(
                footerMapping,
                "Document footer",
                diagnostics,
                "note",
                "noteKey",
                "status",
                "statusKey",
                "source",
                "sourceKey",
                "visible");

            return new YamlDocumentFooterDefinition
            {
                NoteKey = FirstNonEmpty(ReadScalar(footerMapping, "noteKey"), ReadScalar(footerMapping, "note")),
                StatusKey = FirstNonEmpty(ReadScalar(footerMapping, "statusKey"), ReadScalar(footerMapping, "status")),
                SourceKey = FirstNonEmpty(ReadScalar(footerMapping, "sourceKey"), ReadScalar(footerMapping, "source")),
                Visible = ReadNullableBoolean(footerMapping, "visible")
            };
        }

        private static IEnumerable<IFieldDefinition> ReadFields(YamlMappingNode root, List<string> diagnostics)
        {
            if (!TryGetMappingChild(root, "fields", out var fieldsNode))
            {
                yield break;
            }

            if (fieldsNode is YamlSequenceNode fields)
            {
                foreach (var node in fields)
                {
                    if (!(node is YamlMappingNode fieldNode))
                    {
                        diagnostics.Add("Each field definition must be a mapping.");
                        continue;
                    }

                    yield return ParseFieldDefinition(fieldNode, diagnostics);
                }
                yield break;
            }

            if (fieldsNode is YamlMappingNode fieldMap)
            {
                foreach (var child in fieldMap.Children)
                {
                    if (!(child.Key is YamlScalarNode key) || !(child.Value is YamlMappingNode fieldNode))
                    {
                        diagnostics.Add("Each named field definition must be a mapping.");
                        continue;
                    }

                    var field = ParseFieldDefinition(fieldNode, diagnostics, key.Value);
                    yield return field;
                }
            }
        }

        private static IFieldDefinition ParseFieldDefinition(YamlMappingNode fieldNode, List<string> diagnostics, string defaultId = null)
        {
            var definitionId = FirstNonEmpty(ReadScalar(fieldNode, "id"), defaultId) ?? "<unknown>";
            var fieldScope = string.Format(CultureInfo.InvariantCulture, "Field '{0}'", definitionId);
            var isIntegerField = IsIntegerField(fieldNode);
            var isDocumentEditorField = IsDocumentEditorField(fieldNode);

            ValidateAllowedKeys(
                fieldNode,
                fieldScope,
                diagnostics,
                "id",
                "name",
                "extends",
                "control",
                "valueType",
                "caption",
                "captionKey",
                "placeholder",
                "placeholderKey",
                "width",
                "widthHint",
                "weight",
                "overlayScope",
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
                "overlayMode",
                "value",
                "oldValue");

            IFieldDefinition field = isDocumentEditorField
                ? (IFieldDefinition)new YamlDocumentEditorFieldDefinition
                {
                    Id = definitionId,
                    Name = ReadScalar(fieldNode, "name"),
                    Extends = ReadScalar(fieldNode, "extends"),
                    CaptionKey = FirstNonEmpty(ReadScalar(fieldNode, "captionKey"), ReadScalar(fieldNode, "caption")),
                    PlaceholderKey = FirstNonEmpty(ReadScalar(fieldNode, "placeholderKey"), ReadScalar(fieldNode, "placeholder")),
                    Width = ReadNullableDouble(fieldNode, "width"),
                    WidthHint = ReadNullableEnum<FieldWidthHint>(fieldNode, "widthHint"),
                    Weight = ReadNullableDouble(fieldNode, "weight"),
                    Visible = ReadNullableBoolean(fieldNode, "visible"),
                    Enabled = ReadNullableBoolean(fieldNode, "enabled"),
                    ShowOldValueRestoreButton = ReadNullableBoolean(fieldNode, "showOldValueRestoreButton"),
                    IsRequired = ReadBoolean(fieldNode, "required"),
                    ShowLabel = ReadBoolean(fieldNode, "showLabel", true),
                    ShowPlaceholder = ReadBoolean(fieldNode, "showPlaceholder"),
                    ValidationTrigger = ReadNullableEnum<ValidationTrigger>(fieldNode, "validationTrigger"),
                    InteractionMode = ReadNullableEnum<InteractionMode>(fieldNode, "interactionMode"),
                    DensityMode = ReadNullableEnum<DensityMode>(fieldNode, "densityMode"),
                    FieldChromeMode = ReadNullableEnum<FieldChromeMode>(fieldNode, "fieldChromeMode"),
                    CaptionPlacement = ReadNullableEnum<CaptionPlacement>(fieldNode, "captionPlacement"),
                    OverlayMode = ReadNullableEnum<DocumentEditorOverlayMode>(fieldNode, "overlayMode"),
                    Value = ReadScalar(fieldNode, "value"),
                    OldValue = ReadScalar(fieldNode, "oldValue")
                }
                : isIntegerField
                ? (IFieldDefinition)new YamlIntegerFieldDefinition
                {
                    Id = definitionId,
                    Name = ReadScalar(fieldNode, "name"),
                    Extends = ReadScalar(fieldNode, "extends"),
                    CaptionKey = FirstNonEmpty(ReadScalar(fieldNode, "captionKey"), ReadScalar(fieldNode, "caption")),
                    PlaceholderKey = FirstNonEmpty(ReadScalar(fieldNode, "placeholderKey"), ReadScalar(fieldNode, "placeholder")),
                    Width = ReadNullableDouble(fieldNode, "width"),
                    WidthHint = ReadNullableEnum<FieldWidthHint>(fieldNode, "widthHint"),
                    Weight = ReadNullableDouble(fieldNode, "weight"),
                    Visible = ReadNullableBoolean(fieldNode, "visible"),
                    Enabled = ReadNullableBoolean(fieldNode, "enabled"),
                    ShowOldValueRestoreButton = ReadNullableBoolean(fieldNode, "showOldValueRestoreButton"),
                    IsRequired = ReadBoolean(fieldNode, "required"),
                    ShowLabel = ReadBoolean(fieldNode, "showLabel", true),
                    ShowPlaceholder = ReadBoolean(fieldNode, "showPlaceholder"),
                    MinValue = ReadNullableInt(fieldNode, "minValue"),
                    MaxValue = ReadNullableInt(fieldNode, "maxValue"),
                    ValidationTrigger = ReadNullableEnum<ValidationTrigger>(fieldNode, "validationTrigger"),
                    InteractionMode = ReadNullableEnum<InteractionMode>(fieldNode, "interactionMode"),
                    DensityMode = ReadNullableEnum<DensityMode>(fieldNode, "densityMode"),
                    FieldChromeMode = ReadNullableEnum<FieldChromeMode>(fieldNode, "fieldChromeMode"),
                    CaptionPlacement = ReadNullableEnum<CaptionPlacement>(fieldNode, "captionPlacement"),
                }
                : (IFieldDefinition)new YamlStringFieldDefinition
                {
                    Id = definitionId,
                    Name = ReadScalar(fieldNode, "name"),
                    Extends = ReadScalar(fieldNode, "extends"),
                    CaptionKey = FirstNonEmpty(ReadScalar(fieldNode, "captionKey"), ReadScalar(fieldNode, "caption")),
                    PlaceholderKey = FirstNonEmpty(ReadScalar(fieldNode, "placeholderKey"), ReadScalar(fieldNode, "placeholder")),
                    Width = ReadNullableDouble(fieldNode, "width"),
                    WidthHint = ReadNullableEnum<FieldWidthHint>(fieldNode, "widthHint"),
                    Weight = ReadNullableDouble(fieldNode, "weight"),
                    Visible = ReadNullableBoolean(fieldNode, "visible"),
                    Enabled = ReadNullableBoolean(fieldNode, "enabled"),
                    ShowOldValueRestoreButton = ReadNullableBoolean(fieldNode, "showOldValueRestoreButton"),
                    IsRequired = ReadBoolean(fieldNode, "required"),
                    ShowLabel = ReadBoolean(fieldNode, "showLabel", true),
                    ShowPlaceholder = ReadBoolean(fieldNode, "showPlaceholder"),
                    MaxLength = ReadNullableInt(fieldNode, "maxLength"),
                    ValidationTrigger = ReadNullableEnum<ValidationTrigger>(fieldNode, "validationTrigger"),
                    InteractionMode = ReadNullableEnum<InteractionMode>(fieldNode, "interactionMode"),
                    DensityMode = ReadNullableEnum<DensityMode>(fieldNode, "densityMode"),
                    FieldChromeMode = ReadNullableEnum<FieldChromeMode>(fieldNode, "fieldChromeMode"),
                    CaptionPlacement = ReadNullableEnum<CaptionPlacement>(fieldNode, "captionPlacement"),
                    Value = ReadScalar(fieldNode, "value"),
                    OldValue = ReadScalar(fieldNode, "oldValue")
                };

            ValidateNullableEnumValue<FieldWidthHint>(fieldNode, "widthHint", fieldScope, diagnostics);
            ValidateNullableEnumValue<ValidationTrigger>(fieldNode, "validationTrigger", fieldScope, diagnostics);
            ValidateNullableEnumValue<InteractionMode>(fieldNode, "interactionMode", fieldScope, diagnostics);
            ValidateNullableEnumValue<DensityMode>(fieldNode, "densityMode", fieldScope, diagnostics);
            ValidateNullableEnumValue<FieldChromeMode>(fieldNode, "fieldChromeMode", fieldScope, diagnostics);
            ValidateNullableEnumValue<CaptionPlacement>(fieldNode, "captionPlacement", fieldScope, diagnostics);
            ValidateNullableEnumValue<DocumentEditorOverlayMode>(fieldNode, "overlayMode", fieldScope, diagnostics);
            ValidateExclusiveWidth(fieldScope, field.Width, field.WidthHint, diagnostics);

            if (isDocumentEditorField)
            {
                if (HasScalar(fieldNode, "maxLength"))
                {
                    diagnostics.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} uses unsupported property 'maxLength' for document editor fields.",
                        fieldScope));
                }

                if (HasScalar(fieldNode, "minValue"))
                {
                    diagnostics.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} uses unsupported property 'minValue' for document editor fields.",
                        fieldScope));
                }

                if (HasScalar(fieldNode, "maxValue"))
                {
                    diagnostics.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} uses unsupported property 'maxValue' for document editor fields.",
                        fieldScope));
                }
            }
            else if (isIntegerField)
            {
                ValidateNullableInteger(fieldNode, "minValue", fieldScope, diagnostics);
                ValidateNullableInteger(fieldNode, "maxValue", fieldScope, diagnostics);
                ValidateNumericRange(fieldNode, fieldScope, diagnostics);

                if (HasScalar(fieldNode, "maxLength"))
                {
                    diagnostics.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} uses unsupported property 'maxLength' for integer fields.",
                        fieldScope));
                }
            }
            else
            {
                ValidateNullablePositiveInteger(fieldNode, "maxLength", fieldScope, diagnostics);

                if (HasScalar(fieldNode, "minValue"))
                {
                    diagnostics.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} uses unsupported property 'minValue' for string fields.",
                        fieldScope));
                }

                if (HasScalar(fieldNode, "maxValue"))
                {
                    diagnostics.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} uses unsupported property 'maxValue' for string fields.",
                        fieldScope));
                }
            }

            var writableField = field as YamlFieldDefinition;
            if (writableField != null && string.IsNullOrWhiteSpace(writableField.Name))
            {
                writableField.Name = FirstNonEmpty(field.Id, defaultId);
            }

            if (string.IsNullOrWhiteSpace(field.Id))
            {
                diagnostics.Add("Field definition is missing required property 'id'.");
            }

            return field;
        }

        private static IEnumerable<IActionAreaDefinition> ReadActionAreas(YamlMappingNode root, List<string> diagnostics)
        {
            if (!TryGetMappingChild(root, "actionAreas", out var actionAreasNode) || !(actionAreasNode is YamlSequenceNode actionAreas))
            {
                yield break;
            }

            foreach (var node in actionAreas)
            {
                if (!(node is YamlMappingNode areaNode))
                {
                    diagnostics.Add("Each action area definition must be a mapping.");
                    continue;
                }

                ValidateAllowedKeys(
                    areaNode,
                    "Action area",
                    diagnostics,
                    "id",
                    "extends",
                    "name",
                    "placement",
                    "horizontalAlignment",
                    "chromeMode",
                    "shared",
                    "sticky",
                    "visible");

                var area = new YamlActionAreaDefinition
                {
                    Id = ReadScalar(areaNode, "id"),
                    Extends = ReadScalar(areaNode, "extends"),
                    Name = ReadScalar(areaNode, "name"),
                    Placement = ReadNullableEnum<ActionPlacement>(areaNode, "placement"),
                    HorizontalAlignment = ReadNullableEnum<ActionAlignment>(areaNode, "horizontalAlignment"),
                    ChromeMode = ReadNullableEnum<ActionAreaChromeMode>(areaNode, "chromeMode"),
                    Shared = ReadNullableBoolean(areaNode, "shared"),
                    Sticky = ReadNullableBoolean(areaNode, "sticky"),
                    Visible = ReadNullableBoolean(areaNode, "visible")
                };

                ValidateNullableEnumValue<ActionPlacement>(areaNode, "placement", string.Format(CultureInfo.InvariantCulture, "Action area '{0}'", area.Id ?? "<unknown>"), diagnostics);
                ValidateNullableEnumValue<ActionAlignment>(areaNode, "horizontalAlignment", string.Format(CultureInfo.InvariantCulture, "Action area '{0}'", area.Id ?? "<unknown>"), diagnostics);
                ValidateNullableEnumValue<ActionAreaChromeMode>(areaNode, "chromeMode", string.Format(CultureInfo.InvariantCulture, "Action area '{0}'", area.Id ?? "<unknown>"), diagnostics);

                if (string.IsNullOrWhiteSpace(area.Name))
                {
                    area.Name = area.Id;
                }

                if (string.IsNullOrWhiteSpace(area.Id))
                {
                    diagnostics.Add("Action area definition is missing required property 'id'.");
                }

                yield return area;
            }
        }

        private static IEnumerable<IDocumentActionDefinition> ReadActions(YamlMappingNode root, List<string> diagnostics)
        {
            if (!TryGetMappingChild(root, "actions", out var actionsNode) || !(actionsNode is YamlSequenceNode actions))
            {
                yield break;
            }

            foreach (var node in actions)
            {
                if (!(node is YamlMappingNode actionNode))
                {
                    diagnostics.Add("Each action definition must be a mapping.");
                    continue;
                }

                ValidateAllowedKeys(
                    actionNode,
                    "Action",
                    diagnostics,
                    "id",
                    "extends",
                    "name",
                    "caption",
                    "captionKey",
                    "icon",
                    "iconKey",
                    "area",
                    "isPrimary",
                    "order",
                    "slot",
                    "semantic",
                    "visible",
                    "enabled",
                    "actionKind",
                    "kind");

                var action = new YamlDocumentActionDefinition
                {
                    Id = ReadScalar(actionNode, "id"),
                    Extends = ReadScalar(actionNode, "extends"),
                    Name = ReadScalar(actionNode, "name"),
                    CaptionKey = FirstNonEmpty(ReadScalar(actionNode, "captionKey"), ReadScalar(actionNode, "caption")),
                    IconKey = FirstNonEmpty(ReadScalar(actionNode, "iconKey"), ReadScalar(actionNode, "icon")),
                    Area = ReadScalar(actionNode, "area"),
                    IsPrimary = ReadNullableBoolean(actionNode, "isPrimary"),
                    Order = ReadNullableInt(actionNode, "order"),
                    Slot = ReadNullableEnum<ActionSlot>(actionNode, "slot"),
                    Semantic = ReadNullableEnum<ActionSemantic>(
                        actionNode,
                        "semantic") ?? ReadNullableEnum<ActionSemantic>(actionNode, "actionKind") ?? ReadNullableEnum<ActionSemantic>(actionNode, "kind"),
                    Visible = ReadNullableBoolean(actionNode, "visible"),
                    Enabled = ReadNullableBoolean(actionNode, "enabled")
                };

                ValidateNullableEnumValue<ActionSlot>(actionNode, "slot", string.Format(CultureInfo.InvariantCulture, "Action '{0}'", action.Id ?? "<unknown>"), diagnostics);
                ValidateNullableEnumValue<ActionSemantic>(actionNode, "semantic", string.Format(CultureInfo.InvariantCulture, "Action '{0}'", action.Id ?? "<unknown>"), diagnostics);
                ValidateNullableEnumValue<ActionSemantic>(actionNode, "actionKind", string.Format(CultureInfo.InvariantCulture, "Action '{0}'", action.Id ?? "<unknown>"), diagnostics);
                ValidateNullableEnumValue<ActionSemantic>(actionNode, "kind", string.Format(CultureInfo.InvariantCulture, "Action '{0}'", action.Id ?? "<unknown>"), diagnostics);
                ValidateNullableInteger(actionNode, "order", string.Format(CultureInfo.InvariantCulture, "Action '{0}'", action.Id ?? "<unknown>"), diagnostics);

                if (string.IsNullOrWhiteSpace(action.Name))
                {
                    action.Name = action.Id;
                }

                if (string.IsNullOrWhiteSpace(action.Id))
                {
                    diagnostics.Add("Action definition is missing required property 'id'.");
                }

                yield return action;
            }
        }

        public YamlDefinitionImportResult<YamlLayoutDefinition> ImportLayout(string yaml)
        {
            if (string.IsNullOrWhiteSpace(yaml))
            {
                return new YamlDefinitionImportResult<YamlLayoutDefinition>(null, new[] { "YAML content cannot be empty." });
            }

            var diagnostics = new List<string>();

            try
            {
                var stream = new YamlStream();
                using (var reader = new StringReader(yaml))
                {
                    stream.Load(reader);
                }

                if (stream.Documents.Count == 0 || !(stream.Documents[0].RootNode is YamlMappingNode root))
                {
                    return new YamlDefinitionImportResult<YamlLayoutDefinition>(null, new[] { "YAML root document must be a mapping." });
                }

                if (!TryGetMappingChild(root, "layout", out var layoutNode) || !(layoutNode is YamlMappingNode layoutMapping))
                {
                    return new YamlDefinitionImportResult<YamlLayoutDefinition>(null, new[] { "Root form YAML must contain a 'layout' mapping." });
                }

                return new YamlDefinitionImportResult<YamlLayoutDefinition>(ParseLayout(layoutMapping, diagnostics), diagnostics);
            }
            catch (Exception ex)
            {
                return new YamlDefinitionImportResult<YamlLayoutDefinition>(null, new[] { FormatYamlFailure("Failed to import YAML layout definition", ex) });
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

        private static YamlLayoutDefinition ReadLayout(YamlMappingNode root, List<string> diagnostics)
        {
            if (!TryGetMappingChild(root, "layout", out var layoutNode) || !(layoutNode is YamlMappingNode layoutMapping))
            {
                return null;
            }

            return ParseLayout(layoutMapping, diagnostics);
        }

        private static YamlLayoutDefinition ParseLayout(YamlMappingNode layoutNode, List<string> diagnostics)
        {
            ValidateAllowedKeys(
                layoutNode,
                "Layout",
                diagnostics,
                "id",
                "name",
                "type",
                "width",
                "widthHint",
                "weight",
                "overlayScope",
                "visible",
                "enabled",
                "showOldValueRestoreButton",
                "validationTrigger",
                "interactionMode",
                "densityMode",
                "fieldChromeMode",
                "captionPlacement",
                "items");

            var layout = new YamlLayoutDefinition
            {
                Id = ReadScalar(layoutNode, "id"),
                Name = ReadScalar(layoutNode, "name"),
                Width = ReadNullableDouble(layoutNode, "width"),
                WidthHint = ReadNullableEnum<FieldWidthHint>(layoutNode, "widthHint"),
                Weight = ReadNullableDouble(layoutNode, "weight"),
                IsOverlayScope = ReadBoolean(layoutNode, "overlayScope"),
                Visible = ReadNullableBoolean(layoutNode, "visible"),
                Enabled = ReadNullableBoolean(layoutNode, "enabled"),
                ShowOldValueRestoreButton = ReadNullableBoolean(layoutNode, "showOldValueRestoreButton"),
                ValidationTrigger = ReadNullableEnum<ValidationTrigger>(layoutNode, "validationTrigger"),
                InteractionMode = ReadNullableEnum<InteractionMode>(layoutNode, "interactionMode"),
                DensityMode = ReadNullableEnum<DensityMode>(layoutNode, "densityMode"),
                FieldChromeMode = ReadNullableEnum<FieldChromeMode>(layoutNode, "fieldChromeMode"),
                CaptionPlacement = ReadNullableEnum<CaptionPlacement>(layoutNode, "captionPlacement")
            };

            ValidateNullableEnumValue<FieldWidthHint>(layoutNode, "widthHint", "Layout", diagnostics);
            ValidateNullableEnumValue<ValidationTrigger>(layoutNode, "validationTrigger", "Layout", diagnostics);
            ValidateNullableEnumValue<InteractionMode>(layoutNode, "interactionMode", "Layout", diagnostics);
            ValidateNullableEnumValue<DensityMode>(layoutNode, "densityMode", "Layout", diagnostics);
            ValidateNullableEnumValue<FieldChromeMode>(layoutNode, "fieldChromeMode", "Layout", diagnostics);
            ValidateNullableEnumValue<CaptionPlacement>(layoutNode, "captionPlacement", "Layout", diagnostics);
            ValidateNullableBooleanValue(layoutNode, "overlayScope", "Layout", diagnostics);
            ValidateExclusiveWidth("Layout", layout.Width, layout.WidthHint, diagnostics);

            var type = ReadScalar(layoutNode, "type");
            if (!string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(layout.Name))
            {
                layout.Name = type;
            }

            if (string.IsNullOrWhiteSpace(layout.Name))
            {
                layout.Name = layout.Id;
            }

            foreach (var item in ReadLayoutItems(layoutNode, diagnostics))
            {
                layout.Items.Add(item);
            }

            return layout;
        }

        private static IEnumerable<ILayoutItemDefinition> ReadLayoutItems(YamlMappingNode node, List<string> diagnostics)
        {
            if (!TryGetMappingChild(node, "items", out var itemsNode) || !(itemsNode is YamlSequenceNode items))
            {
                yield break;
            }

            foreach (var itemNode in items)
            {
                if (!(itemNode is YamlMappingNode itemMapping))
                {
                    diagnostics.Add("Each layout item must be a mapping.");
                    continue;
                }

                var item = ParseLayoutItem(itemMapping, diagnostics);
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        private static ILayoutItemDefinition ParseLayoutItem(YamlMappingNode itemNode, List<string> diagnostics)
        {
            ValidateAllowedKeys(
                itemNode,
                "Layout item",
                diagnostics,
                "id",
                "name",
                "type",
                "field",
                "fieldRef",
                "width",
                "widthHint",
                "weight",
                "overlayScope",
                "visible",
                "enabled",
                "showOldValueRestoreButton",
                "validationTrigger",
                "interactionMode",
                "densityMode",
                "fieldChromeMode",
                "captionPlacement",
                "items",
                "caption",
                "captionKey",
                "showBorder",
                "variant");

            if (TryGetMappingChild(itemNode, "field", out var inlineFieldNode))
            {
                ValidateNoOverlayScopeOnLeafLayoutItem(itemNode, "Inline field layout item", diagnostics);
                if (inlineFieldNode is YamlScalarNode inlineFieldScalar && !string.IsNullOrWhiteSpace(inlineFieldScalar.Value))
                {
                    return new YamlFieldReferenceDefinition
                    {
                        Id = ReadScalar(itemNode, "id"),
                        Name = FirstNonEmpty(ReadScalar(itemNode, "name"), inlineFieldScalar.Value),
                        FieldRef = inlineFieldScalar.Value
                    };
                }

                if (inlineFieldNode is YamlMappingNode inlineFieldMapping)
                {
                    var field = ParseFieldDefinition(inlineFieldMapping, diagnostics);
                    return new YamlInlineFieldLayoutItemDefinition
                    {
                        Id = ReadScalar(itemNode, "id"),
                        Name = FirstNonEmpty(ReadScalar(itemNode, "name"), field.Id),
                        Field = field
                    };
                }

                diagnostics.Add("Layout item 'field' must be either a scalar field id or a mapping field definition.");
                return null;
            }

            var fieldRef = ReadScalar(itemNode, "fieldRef");
            if (!string.IsNullOrWhiteSpace(fieldRef))
            {
                ValidateNoOverlayScopeOnLeafLayoutItem(itemNode, string.Format(CultureInfo.InvariantCulture, "Field reference '{0}'", fieldRef), diagnostics);
                return new YamlFieldReferenceDefinition
                {
                    Id = ReadScalar(itemNode, "id"),
                    Name = FirstNonEmpty(ReadScalar(itemNode, "name"), fieldRef),
                    FieldRef = fieldRef
                };
            }

            var type = ReadScalar(itemNode, "type");
            if (string.IsNullOrWhiteSpace(type))
            {
                diagnostics.Add("Layout item is missing 'type', 'field', or 'fieldRef'.");
                return null;
            }

            switch (type.Trim())
            {
                case "Row":
                    return ParseRow(itemNode, diagnostics);
                case "Column":
                    return ParseColumn(itemNode, diagnostics);
                case "Container":
                    return ParseContainer(itemNode, diagnostics);
                case "Badge":
                    return ParseBadge(itemNode, diagnostics);
                case "Button":
                    return ParseButton(itemNode, diagnostics);
                default:
                    diagnostics.Add(string.Format(CultureInfo.InvariantCulture, "Unsupported layout item type '{0}'.", type));
                    return null;
            }
        }

        private static YamlRowDefinition ParseRow(YamlMappingNode itemNode, List<string> diagnostics)
        {
            var row = new YamlRowDefinition
            {
                Id = ReadScalar(itemNode, "id"),
                Name = ReadScalar(itemNode, "name"),
                Width = ReadNullableDouble(itemNode, "width"),
                WidthHint = ReadNullableEnum<FieldWidthHint>(itemNode, "widthHint"),
                Weight = ReadNullableDouble(itemNode, "weight"),
                IsOverlayScope = ReadBoolean(itemNode, "overlayScope"),
                Visible = ReadNullableBoolean(itemNode, "visible"),
                Enabled = ReadNullableBoolean(itemNode, "enabled"),
                ShowOldValueRestoreButton = ReadNullableBoolean(itemNode, "showOldValueRestoreButton"),
                ValidationTrigger = ReadNullableEnum<ValidationTrigger>(itemNode, "validationTrigger"),
                InteractionMode = ReadNullableEnum<InteractionMode>(itemNode, "interactionMode"),
                DensityMode = ReadNullableEnum<DensityMode>(itemNode, "densityMode"),
                FieldChromeMode = ReadNullableEnum<FieldChromeMode>(itemNode, "fieldChromeMode"),
                CaptionPlacement = ReadNullableEnum<CaptionPlacement>(itemNode, "captionPlacement")
            };

            var rowScope = string.Format(CultureInfo.InvariantCulture, "Row '{0}'", FirstNonEmpty(row.Id, row.Name) ?? "<unnamed>");
            ValidateNullableEnumValue<FieldWidthHint>(itemNode, "widthHint", rowScope, diagnostics);
            ValidateNullableEnumValue<ValidationTrigger>(itemNode, "validationTrigger", rowScope, diagnostics);
            ValidateNullableEnumValue<InteractionMode>(itemNode, "interactionMode", rowScope, diagnostics);
            ValidateNullableEnumValue<DensityMode>(itemNode, "densityMode", rowScope, diagnostics);
            ValidateNullableEnumValue<FieldChromeMode>(itemNode, "fieldChromeMode", rowScope, diagnostics);
            ValidateNullableEnumValue<CaptionPlacement>(itemNode, "captionPlacement", rowScope, diagnostics);
            ValidateNullableBooleanValue(itemNode, "overlayScope", rowScope, diagnostics);
            ValidateExclusiveWidth(rowScope, row.Width, row.WidthHint, diagnostics);

            if (string.IsNullOrWhiteSpace(row.Name))
            {
                row.Name = row.Id;
            }

            foreach (var item in ReadLayoutItems(itemNode, diagnostics))
            {
                row.Items.Add(item);
            }

            return row;
        }

        private static YamlColumnDefinition ParseColumn(YamlMappingNode itemNode, List<string> diagnostics)
        {
            var column = new YamlColumnDefinition
            {
                Id = ReadScalar(itemNode, "id"),
                Name = ReadScalar(itemNode, "name"),
                Width = ReadNullableDouble(itemNode, "width"),
                WidthHint = ReadNullableEnum<FieldWidthHint>(itemNode, "widthHint"),
                Weight = ReadNullableDouble(itemNode, "weight"),
                IsOverlayScope = ReadBoolean(itemNode, "overlayScope"),
                Visible = ReadNullableBoolean(itemNode, "visible"),
                Enabled = ReadNullableBoolean(itemNode, "enabled"),
                ShowOldValueRestoreButton = ReadNullableBoolean(itemNode, "showOldValueRestoreButton"),
                ValidationTrigger = ReadNullableEnum<ValidationTrigger>(itemNode, "validationTrigger"),
                InteractionMode = ReadNullableEnum<InteractionMode>(itemNode, "interactionMode"),
                DensityMode = ReadNullableEnum<DensityMode>(itemNode, "densityMode"),
                FieldChromeMode = ReadNullableEnum<FieldChromeMode>(itemNode, "fieldChromeMode"),
                CaptionPlacement = ReadNullableEnum<CaptionPlacement>(itemNode, "captionPlacement")
            };

            var columnScope = string.Format(CultureInfo.InvariantCulture, "Column '{0}'", FirstNonEmpty(column.Id, column.Name) ?? "<unnamed>");
            ValidateNullableEnumValue<FieldWidthHint>(itemNode, "widthHint", columnScope, diagnostics);
            ValidateNullableEnumValue<ValidationTrigger>(itemNode, "validationTrigger", columnScope, diagnostics);
            ValidateNullableEnumValue<InteractionMode>(itemNode, "interactionMode", columnScope, diagnostics);
            ValidateNullableEnumValue<DensityMode>(itemNode, "densityMode", columnScope, diagnostics);
            ValidateNullableEnumValue<FieldChromeMode>(itemNode, "fieldChromeMode", columnScope, diagnostics);
            ValidateNullableEnumValue<CaptionPlacement>(itemNode, "captionPlacement", columnScope, diagnostics);
            ValidateNullableBooleanValue(itemNode, "overlayScope", columnScope, diagnostics);
            ValidateExclusiveWidth(columnScope, column.Width, column.WidthHint, diagnostics);

            if (string.IsNullOrWhiteSpace(column.Name))
            {
                column.Name = column.Id;
            }

            foreach (var item in ReadLayoutItems(itemNode, diagnostics))
            {
                column.Items.Add(item);
            }

            return column;
        }

        private static YamlContainerDefinition ParseContainer(YamlMappingNode itemNode, List<string> diagnostics)
        {
            var container = new YamlContainerDefinition
            {
                Id = ReadScalar(itemNode, "id"),
                Name = ReadScalar(itemNode, "name"),
                CaptionKey = FirstNonEmpty(ReadScalar(itemNode, "captionKey"), ReadScalar(itemNode, "caption")),
                Width = ReadNullableDouble(itemNode, "width"),
                WidthHint = ReadNullableEnum<FieldWidthHint>(itemNode, "widthHint"),
                Weight = ReadNullableDouble(itemNode, "weight"),
                IsOverlayScope = ReadBoolean(itemNode, "overlayScope"),
                ShowBorder = ReadBoolean(itemNode, "showBorder", true),
                Variant = ReadNullableEnum<ContainerVariant>(itemNode, "variant"),
                Visible = ReadNullableBoolean(itemNode, "visible"),
                Enabled = ReadNullableBoolean(itemNode, "enabled"),
                ShowOldValueRestoreButton = ReadNullableBoolean(itemNode, "showOldValueRestoreButton"),
                ValidationTrigger = ReadNullableEnum<ValidationTrigger>(itemNode, "validationTrigger"),
                InteractionMode = ReadNullableEnum<InteractionMode>(itemNode, "interactionMode"),
                DensityMode = ReadNullableEnum<DensityMode>(itemNode, "densityMode"),
                FieldChromeMode = ReadNullableEnum<FieldChromeMode>(itemNode, "fieldChromeMode"),
                CaptionPlacement = ReadNullableEnum<CaptionPlacement>(itemNode, "captionPlacement")
            };

            var containerScope = string.Format(CultureInfo.InvariantCulture, "Container '{0}'", FirstNonEmpty(container.Id, container.Name) ?? "<unnamed>");
            ValidateNullableEnumValue<FieldWidthHint>(itemNode, "widthHint", containerScope, diagnostics);
            ValidateNullableEnumValue<ValidationTrigger>(itemNode, "validationTrigger", containerScope, diagnostics);
            ValidateNullableEnumValue<InteractionMode>(itemNode, "interactionMode", containerScope, diagnostics);
            ValidateNullableEnumValue<DensityMode>(itemNode, "densityMode", containerScope, diagnostics);
            ValidateNullableEnumValue<FieldChromeMode>(itemNode, "fieldChromeMode", containerScope, diagnostics);
            ValidateNullableEnumValue<CaptionPlacement>(itemNode, "captionPlacement", containerScope, diagnostics);
            ValidateNullableEnumValue<ContainerVariant>(itemNode, "variant", containerScope, diagnostics);
            ValidateNullableBooleanValue(itemNode, "overlayScope", containerScope, diagnostics);
            ValidateExclusiveWidth(containerScope, container.Width, container.WidthHint, diagnostics);

            if (string.IsNullOrWhiteSpace(container.Name))
            {
                container.Name = container.Id;
            }

            foreach (var item in ReadLayoutItems(itemNode, diagnostics))
            {
                container.Items.Add(item);
            }

            return container;
        }

        private static YamlBadgeDefinition ParseBadge(YamlMappingNode itemNode, List<string> diagnostics)
        {
            var badge = new YamlBadgeDefinition
            {
                Id = ReadScalar(itemNode, "id"),
                Name = ReadScalar(itemNode, "name"),
                TextKey = FirstNonEmpty(ReadScalar(itemNode, "textKey"), ReadScalar(itemNode, "text")),
                IconKey = FirstNonEmpty(ReadScalar(itemNode, "iconKey"), ReadScalar(itemNode, "icon")),
                ToolTipKey = FirstNonEmpty(
                    FirstNonEmpty(ReadScalar(itemNode, "toolTipKey"), ReadScalar(itemNode, "tooltipKey")),
                    FirstNonEmpty(ReadScalar(itemNode, "toolTip"), ReadScalar(itemNode, "tooltip"))),
                Tone = ReadNullableEnum<BadgeTone>(itemNode, "tone"),
                Variant = ReadNullableEnum<BadgeVariant>(itemNode, "variant"),
                Size = ReadNullableEnum<BadgeSize>(itemNode, "size"),
                IconPlacement = ReadNullableEnum<IconPlacement>(itemNode, "iconPlacement"),
                Width = ReadNullableDouble(itemNode, "width"),
                WidthHint = ReadNullableEnum<FieldWidthHint>(itemNode, "widthHint"),
                Weight = ReadNullableDouble(itemNode, "weight"),
                Visible = ReadNullableBoolean(itemNode, "visible"),
                Enabled = ReadNullableBoolean(itemNode, "enabled"),
                ShowOldValueRestoreButton = ReadNullableBoolean(itemNode, "showOldValueRestoreButton"),
                ValidationTrigger = ReadNullableEnum<ValidationTrigger>(itemNode, "validationTrigger"),
                InteractionMode = ReadNullableEnum<InteractionMode>(itemNode, "interactionMode"),
                DensityMode = ReadNullableEnum<DensityMode>(itemNode, "densityMode"),
                FieldChromeMode = ReadNullableEnum<FieldChromeMode>(itemNode, "fieldChromeMode"),
                CaptionPlacement = ReadNullableEnum<CaptionPlacement>(itemNode, "captionPlacement")
            };

            var badgeScope = string.Format(CultureInfo.InvariantCulture, "Badge '{0}'", FirstNonEmpty(badge.Id, badge.Name) ?? "<unnamed>");
            ValidateAllowedKeys(
                itemNode,
                badgeScope,
                diagnostics,
                "type",
                "id",
                "name",
                "text",
                "textKey",
                "icon",
                "iconKey",
                "toolTip",
                "toolTipKey",
                "tooltip",
                "tooltipKey",
                "tone",
                "variant",
                "size",
                "iconPlacement",
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
                "captionPlacement");

            ValidateNullableEnumValue<BadgeTone>(itemNode, "tone", badgeScope, diagnostics);
            ValidateNullableEnumValue<BadgeVariant>(itemNode, "variant", badgeScope, diagnostics);
            ValidateNullableEnumValue<BadgeSize>(itemNode, "size", badgeScope, diagnostics);
            ValidateNullableEnumValue<IconPlacement>(itemNode, "iconPlacement", badgeScope, diagnostics);
            ValidateNullableEnumValue<FieldWidthHint>(itemNode, "widthHint", badgeScope, diagnostics);
            ValidateNullableEnumValue<ValidationTrigger>(itemNode, "validationTrigger", badgeScope, diagnostics);
            ValidateNullableEnumValue<InteractionMode>(itemNode, "interactionMode", badgeScope, diagnostics);
            ValidateNullableEnumValue<DensityMode>(itemNode, "densityMode", badgeScope, diagnostics);
            ValidateNullableEnumValue<FieldChromeMode>(itemNode, "fieldChromeMode", badgeScope, diagnostics);
            ValidateNullableEnumValue<CaptionPlacement>(itemNode, "captionPlacement", badgeScope, diagnostics);
            ValidateExclusiveWidth(badgeScope, badge.Width, badge.WidthHint, diagnostics);

            if (string.IsNullOrWhiteSpace(badge.Name))
            {
                badge.Name = badge.Id;
            }

            return badge;
        }

        private static YamlButtonDefinition ParseButton(YamlMappingNode itemNode, List<string> diagnostics)
        {
            var button = new YamlButtonDefinition
            {
                Id = ReadScalar(itemNode, "id"),
                Name = ReadScalar(itemNode, "name"),
                TextKey = FirstNonEmpty(ReadScalar(itemNode, "textKey"), ReadScalar(itemNode, "text")),
                IconKey = FirstNonEmpty(ReadScalar(itemNode, "iconKey"), ReadScalar(itemNode, "icon")),
                ToolTipKey = FirstNonEmpty(
                    FirstNonEmpty(ReadScalar(itemNode, "toolTipKey"), ReadScalar(itemNode, "tooltipKey")),
                    FirstNonEmpty(ReadScalar(itemNode, "toolTip"), ReadScalar(itemNode, "tooltip"))),
                CommandId = FirstNonEmpty(ReadScalar(itemNode, "commandId"), ReadScalar(itemNode, "id")),
                Tone = ReadNullableEnum<ButtonTone>(itemNode, "tone"),
                Variant = ReadNullableEnum<ButtonVariant>(itemNode, "variant"),
                Size = ReadNullableEnum<ButtonSize>(itemNode, "size"),
                IconPlacement = ReadNullableEnum<IconPlacement>(itemNode, "iconPlacement"),
                Width = ReadNullableDouble(itemNode, "width"),
                WidthHint = ReadNullableEnum<FieldWidthHint>(itemNode, "widthHint"),
                Weight = ReadNullableDouble(itemNode, "weight"),
                Visible = ReadNullableBoolean(itemNode, "visible"),
                Enabled = ReadNullableBoolean(itemNode, "enabled"),
                ShowOldValueRestoreButton = ReadNullableBoolean(itemNode, "showOldValueRestoreButton"),
                ValidationTrigger = ReadNullableEnum<ValidationTrigger>(itemNode, "validationTrigger"),
                InteractionMode = ReadNullableEnum<InteractionMode>(itemNode, "interactionMode"),
                DensityMode = ReadNullableEnum<DensityMode>(itemNode, "densityMode"),
                FieldChromeMode = ReadNullableEnum<FieldChromeMode>(itemNode, "fieldChromeMode"),
                CaptionPlacement = ReadNullableEnum<CaptionPlacement>(itemNode, "captionPlacement")
            };

            var buttonScope = string.Format(CultureInfo.InvariantCulture, "Button '{0}'", FirstNonEmpty(button.Id, button.Name) ?? "<unnamed>");
            ValidateAllowedKeys(
                itemNode,
                buttonScope,
                diagnostics,
                "type",
                "id",
                "name",
                "text",
                "textKey",
                "icon",
                "iconKey",
                "toolTip",
                "toolTipKey",
                "tooltip",
                "tooltipKey",
                "commandId",
                "tone",
                "variant",
                "size",
                "iconPlacement",
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
                "captionPlacement");

            ValidateNullableEnumValue<ButtonTone>(itemNode, "tone", buttonScope, diagnostics);
            ValidateNullableEnumValue<ButtonVariant>(itemNode, "variant", buttonScope, diagnostics);
            ValidateNullableEnumValue<ButtonSize>(itemNode, "size", buttonScope, diagnostics);
            ValidateNullableEnumValue<IconPlacement>(itemNode, "iconPlacement", buttonScope, diagnostics);
            ValidateNullableEnumValue<FieldWidthHint>(itemNode, "widthHint", buttonScope, diagnostics);
            ValidateNullableEnumValue<ValidationTrigger>(itemNode, "validationTrigger", buttonScope, diagnostics);
            ValidateNullableEnumValue<InteractionMode>(itemNode, "interactionMode", buttonScope, diagnostics);
            ValidateNullableEnumValue<DensityMode>(itemNode, "densityMode", buttonScope, diagnostics);
            ValidateNullableEnumValue<FieldChromeMode>(itemNode, "fieldChromeMode", buttonScope, diagnostics);
            ValidateNullableEnumValue<CaptionPlacement>(itemNode, "captionPlacement", buttonScope, diagnostics);
            ValidateExclusiveWidth(buttonScope, button.Width, button.WidthHint, diagnostics);

            if (string.IsNullOrWhiteSpace(button.Name))
            {
                button.Name = button.Id;
            }

            return button;
        }

        private static string ReadScalar(YamlMappingNode node, string key)
        {
            if (!TryGetMappingChild(node, key, out var valueNode))
            {
                return null;
            }

            if (!(valueNode is YamlScalarNode scalar))
            {
                return null;
            }

            return scalar.Value;
        }

        private static bool ReadBoolean(YamlMappingNode node, string key, bool defaultValue = false)
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            bool parsed;
            return bool.TryParse(value, out parsed) ? parsed : defaultValue;
        }

        private static bool? ReadNullableBoolean(YamlMappingNode node, string key)
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            bool parsed;
            if (bool.TryParse(value, out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static double? ReadNullableDouble(YamlMappingNode node, string key)
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            double parsed;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static TEnum ReadEnum<TEnum>(YamlMappingNode node, string key, TEnum defaultValue)
            where TEnum : struct
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            TEnum parsed;
            if (Enum.TryParse<TEnum>(value, true, out parsed))
            {
                return parsed;
            }

            int numericValue;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue) &&
                Enum.IsDefined(typeof(TEnum), numericValue))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
            }

            return defaultValue;
        }

        private static TEnum? ReadNullableEnum<TEnum>(YamlMappingNode node, string key)
            where TEnum : struct
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            TEnum parsed;
            if (Enum.TryParse<TEnum>(value, true, out parsed))
            {
                return parsed;
            }

            int numericValue;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue) &&
                Enum.IsDefined(typeof(TEnum), numericValue))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
            }

            return null;
        }

        private static int? ReadNullableInt(YamlMappingNode node, string key)
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static bool IsIntegerField(YamlMappingNode node)
        {
            var control = ReadScalar(node, "control");
            if (string.Equals(control, "YamlIntegerBox", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var valueType = ReadScalar(node, "valueType");
            return string.Equals(valueType, "int", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(valueType, "integer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDocumentEditorField(YamlMappingNode node)
        {
            var control = ReadScalar(node, "control");
            if (string.Equals(control, "YamlDocumentEditor", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var valueType = ReadScalar(node, "valueType");
            return string.Equals(valueType, "richText", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(valueType, "document", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(valueType, "documentEditor", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasScalar(YamlMappingNode node, string key)
        {
            return !string.IsNullOrWhiteSpace(ReadScalar(node, key));
        }

        private static bool TryGetMappingChild(YamlMappingNode node, string key, out YamlNode valueNode)
        {
            valueNode = null;
            if (node == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            foreach (var child in node.Children)
            {
                if (child.Key is YamlScalarNode scalar &&
                    string.Equals(scalar.Value, key, StringComparison.OrdinalIgnoreCase))
                {
                    valueNode = child.Value;
                    return true;
                }
            }

            return false;
        }

        private static string FirstNonEmpty(string first, string second)
        {
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }

            return second;
        }

        private static void ValidateExclusiveWidth(string scope, double? width, FieldWidthHint? widthHint, IList<string> diagnostics)
        {
            if (width.HasValue && widthHint.HasValue)
            {
                diagnostics.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} cannot define both 'width' and 'widthHint'.",
                    scope));
            }
        }

        private static void ValidateNullableEnumValue<TEnum>(YamlMappingNode node, string key, string scope, IList<string> diagnostics)
            where TEnum : struct
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            TEnum parsed;
            if (Enum.TryParse<TEnum>(value, true, out parsed))
            {
                return;
            }

            int numericValue;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue) &&
                Enum.IsDefined(typeof(TEnum), numericValue))
            {
                return;
            }

            diagnostics.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0} has invalid value '{1}' for '{2}'. Allowed values: {3}.",
                scope,
                value,
                key,
                string.Join(", ", Enum.GetNames(typeof(TEnum)))));
        }

        private static void ValidateNullablePositiveInteger(YamlMappingNode node, string key, string scope, IList<string> diagnostics)
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed >= 0)
            {
                return;
            }

            diagnostics.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0} has invalid value '{1}' for '{2}'. Expected a non-negative integer.",
                scope,
                value,
                key));
        }

        private static void ValidateNullableInteger(YamlMappingNode node, string key, string scope, IList<string> diagnostics)
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return;
            }

            diagnostics.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0} has invalid value '{1}' for '{2}'. Expected an integer.",
                scope,
                value,
                key));
        }

        private static void ValidateNullableBooleanValue(YamlMappingNode node, string key, string scope, IList<string> diagnostics)
        {
            var value = ReadScalar(node, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            bool parsed;
            if (bool.TryParse(value, out parsed))
            {
                return;
            }

            diagnostics.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0} has invalid value '{1}' for '{2}'. Expected a boolean value.",
                scope,
                value,
                key));
        }

        private static void ValidateNoOverlayScopeOnLeafLayoutItem(YamlMappingNode node, string scope, IList<string> diagnostics)
        {
            if (TryGetMappingChild(node, "overlayScope", out _))
            {
                diagnostics.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} cannot define 'overlayScope'. Overlay scope can only be defined on layout, row, column, or container nodes.",
                    scope));
            }
        }

        private static void ValidateNumericRange(YamlMappingNode node, string scope, IList<string> diagnostics)
        {
            var minValue = ReadNullableInt(node, "minValue");
            var maxValue = ReadNullableInt(node, "maxValue");
            if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
            {
                diagnostics.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} has invalid range. 'minValue' cannot be greater than 'maxValue'.",
                    scope));
            }
        }

        private static void ValidateAllowedKeys(YamlMappingNode node, string scope, IList<string> diagnostics, params string[] allowedKeys)
        {
            if (node == null || diagnostics == null)
            {
                return;
            }

            var allowed = new HashSet<string>(allowedKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var child in node.Children)
            {
                if (!(child.Key is YamlScalarNode scalar) || string.IsNullOrWhiteSpace(scalar.Value))
                {
                    continue;
                }

                if (!allowed.Contains(scalar.Value))
                {
                    diagnostics.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} uses unsupported property '{1}'.",
                        scope,
                        scalar.Value));
                }
            }
        }

        private static YamlDefinitionImportResult<YamlDocumentDefinition> Failure(string message)
        {
            return new YamlDefinitionImportResult<YamlDocumentDefinition>(null, new[] { message });
        }
    }
}








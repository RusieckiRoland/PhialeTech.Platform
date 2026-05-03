using System;
using System.Collections.Generic;
using System.Linq;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Definitions.Fields;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Definitions.Layouts;
using PhialeTech.YamlApp.Core.Resolved;

namespace PhialeTech.YamlApp.Core.Normalization
{
    public sealed class YamlDocumentDefinitionNormalizer
    {
        public YamlDocumentNormalizationResult Normalize(YamlDocumentDefinition source)
        {
            var diagnostics = new List<string>();

            if (source == null)
            {
                diagnostics.Add("Document definition cannot be null.");
                return new YamlDocumentNormalizationResult(null, null, new Dictionary<string, string>(), diagnostics);
            }

            var fieldMap = new Dictionary<string, IFieldDefinition>(StringComparer.OrdinalIgnoreCase);

            if (source is YamlFormDocumentDefinition formSource)
            {
                var sourceFieldMap = BuildFieldSourceMap(formSource.Fields, diagnostics, "form fields");
                var sourceActionAreaMap = BuildActionAreaSourceMap(formSource.ActionAreas, diagnostics, "form action areas");
                var sourceActionMap = BuildActionSourceMap(formSource.Actions, diagnostics, "form actions");
                var actionAreaMap = new Dictionary<string, IActionAreaDefinition>(StringComparer.OrdinalIgnoreCase);
                var actionMap = new Dictionary<string, IDocumentActionDefinition>(StringComparer.OrdinalIgnoreCase);

                foreach (var actionArea in formSource.ActionAreas)
                {
                    var resolvedActionArea = ResolveActionArea(actionArea, sourceActionAreaMap, new HashSet<string>(StringComparer.OrdinalIgnoreCase), diagnostics, "form action areas");
                    RegisterActionArea(resolvedActionArea, actionAreaMap, diagnostics, "form action areas");
                }

                foreach (var field in formSource.Fields)
                {
                    var resolvedField = ResolveField(field, sourceFieldMap, new HashSet<string>(StringComparer.OrdinalIgnoreCase), diagnostics, "form fields");
                    RegisterField(resolvedField, fieldMap, diagnostics, "form fields");
                }

                foreach (var action in formSource.Actions)
                {
                    var resolvedAction = ResolveAction(action, sourceActionMap, new HashSet<string>(StringComparer.OrdinalIgnoreCase), diagnostics, "form actions");
                    RegisterAction(resolvedAction, actionMap, diagnostics, "form actions");
                }

                var normalizedForm = new YamlFormDocumentDefinition
                {
                    Id = source.Id,
                    Name = source.Name,
                    Kind = source.Kind,
                    TopRegionChrome = source.TopRegionChrome,
                    BottomRegionChrome = source.BottomRegionChrome,
                    Width = source.Width,
                    WidthHint = source.WidthHint,
                    Visible = source.Visible,
                    Enabled = source.Enabled,
                    ShowOldValueRestoreButton = source.ShowOldValueRestoreButton,
                    ValidationTrigger = source.ValidationTrigger,
                    InteractionMode = source.InteractionMode,
                    DensityMode = source.DensityMode,
                    FieldChromeMode = source.FieldChromeMode,
                    CaptionPlacement = source.CaptionPlacement,
                    Header = CloneHeader(source.Header),
                    Footer = CloneFooter(source.Footer),
                    Layout = NormalizeLayout(source.Layout, fieldMap, diagnostics)
                };

                foreach (var field in fieldMap.Values)
                {
                    normalizedForm.Fields.Add(field);
                }

                foreach (var actionArea in actionAreaMap.Values)
                {
                    normalizedForm.ActionAreas.Add(actionArea);
                }

                foreach (var action in actionMap.Values)
                {
                    normalizedForm.Actions.Add(CloneAction(action));
                }

                if (normalizedForm.Layout != null)
                {
                    ValidateFieldReferences(normalizedForm.Layout, fieldMap, diagnostics);
                }

                ValidateActionAreaReferences(normalizedForm.Actions, actionAreaMap, diagnostics);

                var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in fieldMap)
                {
                    index[pair.Key] = pair.Key;
                }

                var resolvedFormDocument = BuildResolvedFormDocument(normalizedForm);
                return new YamlDocumentNormalizationResult(normalizedForm, resolvedFormDocument, index, diagnostics);
            }

            var normalized = new YamlDocumentDefinition
            {
                Id = source.Id,
                Name = source.Name,
                Kind = source.Kind,
                TopRegionChrome = source.TopRegionChrome,
                BottomRegionChrome = source.BottomRegionChrome,
                Width = source.Width,
                WidthHint = source.WidthHint,
                Visible = source.Visible,
                Enabled = source.Enabled,
                ShowOldValueRestoreButton = source.ShowOldValueRestoreButton,
                ValidationTrigger = source.ValidationTrigger,
                InteractionMode = source.InteractionMode,
                DensityMode = source.DensityMode,
                FieldChromeMode = source.FieldChromeMode,
                CaptionPlacement = source.CaptionPlacement,
                Header = CloneHeader(source.Header),
                Footer = CloneFooter(source.Footer),
                Layout = NormalizeLayout(source.Layout, fieldMap, diagnostics)
            };
            var resolvedDocument = BuildResolvedDocument(normalized);
            return new YamlDocumentNormalizationResult(normalized, resolvedDocument, new Dictionary<string, string>(), diagnostics);
        }

        private static YamlLayoutDefinition NormalizeLayout(YamlLayoutDefinition source, IDictionary<string, IFieldDefinition> fieldMap, List<string> diagnostics)
        {
            if (source == null)
            {
                return null;
            }

            var layout = new YamlLayoutDefinition
            {
                Id = source.Id,
                Name = source.Name,
                Width = source.Width,
                WidthHint = source.WidthHint,
                IsOverlayScope = source.IsOverlayScope,
                Visible = source.Visible,
                Enabled = source.Enabled,
                ShowOldValueRestoreButton = source.ShowOldValueRestoreButton,
                ValidationTrigger = source.ValidationTrigger,
                InteractionMode = source.InteractionMode,
                DensityMode = source.DensityMode,
                FieldChromeMode = source.FieldChromeMode,
                CaptionPlacement = source.CaptionPlacement
            };

            foreach (var item in source.Items)
            {
                var normalizedItem = NormalizeLayoutItem(item, fieldMap, diagnostics);
                if (normalizedItem != null)
                {
                    layout.Items.Add(normalizedItem);
                }
            }

            return layout;
        }

        private static ILayoutItemDefinition NormalizeLayoutItem(ILayoutItemDefinition source, IDictionary<string, IFieldDefinition> fieldMap, List<string> diagnostics)
        {
            if (source == null)
            {
                return null;
            }

            if (source is IInlineFieldLayoutItemDefinition inlineFieldItem)
            {
                var rawField = inlineFieldItem.Field;
                var field = ResolveField(rawField, fieldMap, new HashSet<string>(StringComparer.OrdinalIgnoreCase), diagnostics, "inline layout item");
                RegisterField(field, fieldMap, diagnostics, "inline layout item");

                return new YamlFieldReferenceDefinition
                {
                    Id = source.Id,
                    Name = source.Name,
                    FieldRef = field == null ? null : field.Id
                };
            }

            if (source is IFieldReferenceDefinition fieldReference)
            {
                return new YamlFieldReferenceDefinition
                {
                    Id = source.Id,
                    Name = source.Name,
                    FieldRef = fieldReference.FieldRef
                };
            }

            if (source is IRowDefinition row)
            {
                var normalized = new YamlRowDefinition
                {
                    Id = row.Id,
                    Name = row.Name,
                    Width = row.Width,
                    WidthHint = row.WidthHint,
                    IsOverlayScope = row.IsOverlayScope,
                    Visible = row.Visible,
                    Enabled = row.Enabled,
                    ShowOldValueRestoreButton = row.ShowOldValueRestoreButton,
                    ValidationTrigger = row.ValidationTrigger,
                    InteractionMode = row.InteractionMode,
                    DensityMode = row.DensityMode,
                    FieldChromeMode = row.FieldChromeMode,
                    CaptionPlacement = row.CaptionPlacement
                };

                foreach (var child in row.Items)
                {
                    var normalizedChild = NormalizeLayoutItem(child, fieldMap, diagnostics);
                    if (normalizedChild != null)
                    {
                        normalized.Items.Add(normalizedChild);
                    }
                }

                if (normalized.Items.Count == 0)
                {
                    diagnostics.Add("Row layout item must contain at least one child item.");
                }

                return normalized;
            }

            if (source is IColumnDefinition column)
            {
                var normalized = new YamlColumnDefinition
                {
                    Id = column.Id,
                    Name = column.Name,
                    Width = column.Width,
                    WidthHint = column.WidthHint,
                    IsOverlayScope = column.IsOverlayScope,
                    Visible = column.Visible,
                    Enabled = column.Enabled,
                    ShowOldValueRestoreButton = column.ShowOldValueRestoreButton,
                    ValidationTrigger = column.ValidationTrigger,
                    InteractionMode = column.InteractionMode,
                    DensityMode = column.DensityMode,
                    FieldChromeMode = column.FieldChromeMode,
                    CaptionPlacement = column.CaptionPlacement
                };

                foreach (var child in column.Items)
                {
                    var normalizedChild = NormalizeLayoutItem(child, fieldMap, diagnostics);
                    if (normalizedChild != null)
                    {
                        normalized.Items.Add(normalizedChild);
                    }
                }

                return normalized;
            }

            if (source is IContainerDefinition container)
            {
                var normalized = new YamlContainerDefinition
                {
                    Id = container.Id,
                    Name = container.Name,
                    CaptionKey = container.CaptionKey,
                    ShowBorder = container.ShowBorder,
                    Variant = container.Variant,
                    Width = container.Width,
                    WidthHint = container.WidthHint,
                    IsOverlayScope = container.IsOverlayScope,
                    Visible = container.Visible,
                    Enabled = container.Enabled,
                    ShowOldValueRestoreButton = container.ShowOldValueRestoreButton,
                    ValidationTrigger = container.ValidationTrigger,
                    InteractionMode = container.InteractionMode,
                    DensityMode = container.DensityMode,
                    FieldChromeMode = container.FieldChromeMode,
                    CaptionPlacement = container.CaptionPlacement
                };

                foreach (var child in container.Items)
                {
                    var normalizedChild = NormalizeLayoutItem(child, fieldMap, diagnostics);
                    if (normalizedChild != null)
                    {
                        normalized.Items.Add(normalizedChild);
                    }
                }

                return normalized;
            }

            if (source is IBadgeDefinition badge)
            {
                return new YamlBadgeDefinition
                {
                    Id = badge.Id,
                    Name = badge.Name,
                    TextKey = badge.TextKey,
                    Tone = badge.Tone,
                    Variant = badge.Variant,
                    Size = badge.Size,
                    Width = badge.Width,
                    WidthHint = badge.WidthHint,
                    Weight = badge.Weight,
                    Visible = badge.Visible,
                    Enabled = badge.Enabled,
                    ShowOldValueRestoreButton = badge.ShowOldValueRestoreButton,
                    ValidationTrigger = badge.ValidationTrigger,
                    InteractionMode = badge.InteractionMode,
                    DensityMode = badge.DensityMode,
                    FieldChromeMode = badge.FieldChromeMode,
                    CaptionPlacement = badge.CaptionPlacement
                };
            }

            if (source is IButtonDefinition button)
            {
                return new YamlButtonDefinition
                {
                    Id = button.Id,
                    Name = button.Name,
                    TextKey = button.TextKey,
                    CommandId = button.CommandId,
                    Tone = button.Tone,
                    Variant = button.Variant,
                    Width = button.Width,
                    WidthHint = button.WidthHint,
                    Weight = button.Weight,
                    Visible = button.Visible,
                    Enabled = button.Enabled,
                    ShowOldValueRestoreButton = button.ShowOldValueRestoreButton,
                    ValidationTrigger = button.ValidationTrigger,
                    InteractionMode = button.InteractionMode,
                    DensityMode = button.DensityMode,
                    FieldChromeMode = button.FieldChromeMode,
                    CaptionPlacement = button.CaptionPlacement
                };
            }

            diagnostics.Add(string.Format("Unsupported layout item type '{0}'.", source.GetType().FullName));
            return null;
        }

        private static YamlDocumentActionDefinition CloneAction(IDocumentActionDefinition action)
        {
            if (action == null)
            {
                return null;
            }

            return new YamlDocumentActionDefinition
            {
                Id = action.Id,
                Extends = action.Extends,
                Name = action.Name,
                CaptionKey = action.CaptionKey,
                IconKey = action.IconKey,
                Area = action.Area,
                IsPrimary = action.IsPrimary,
                Order = action.Order,
                Slot = action.Slot,
                Semantic = action.Semantic,
                Visible = action.Visible,
                Enabled = action.Enabled
            };
        }

        private static YamlActionAreaDefinition CloneActionArea(IActionAreaDefinition actionArea)
        {
            if (actionArea == null)
            {
                return null;
            }

            return new YamlActionAreaDefinition
            {
                Id = actionArea.Id,
                Extends = actionArea.Extends,
                Name = actionArea.Name,
                Placement = actionArea.Placement,
                HorizontalAlignment = actionArea.HorizontalAlignment,
                ChromeMode = actionArea.ChromeMode,
                Shared = actionArea.Shared,
                Sticky = actionArea.Sticky,
                Visible = actionArea.Visible
            };
        }

        private static YamlDocumentHeaderDefinition CloneHeader(IDocumentHeaderDefinition header)
        {
            if (header == null)
            {
                return null;
            }

            return new YamlDocumentHeaderDefinition
            {
                TitleKey = header.TitleKey,
                SubtitleKey = header.SubtitleKey,
                DescriptionKey = header.DescriptionKey,
                StatusKey = header.StatusKey,
                ContextKey = header.ContextKey,
                IconKey = header.IconKey,
                Visible = header.Visible
            };
        }

        private static YamlDocumentFooterDefinition CloneFooter(IDocumentFooterDefinition footer)
        {
            if (footer == null)
            {
                return null;
            }

            return new YamlDocumentFooterDefinition
            {
                NoteKey = footer.NoteKey,
                StatusKey = footer.StatusKey,
                SourceKey = footer.SourceKey,
                Visible = footer.Visible
            };
        }

        private static Dictionary<string, IActionAreaDefinition> BuildActionAreaSourceMap(IEnumerable<IActionAreaDefinition> actionAreas, List<string> diagnostics, string sourceName)
        {
            var actionAreaMap = new Dictionary<string, IActionAreaDefinition>(StringComparer.OrdinalIgnoreCase);
            if (actionAreas == null)
            {
                return actionAreaMap;
            }

            foreach (var actionArea in actionAreas)
            {
                if (actionArea == null)
                {
                    diagnostics.Add(string.Format("An action area from {0} is null.", sourceName));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(actionArea.Id))
                {
                    diagnostics.Add(string.Format("An action area from {0} is missing required property 'Id'.", sourceName));
                    continue;
                }

                if (actionAreaMap.ContainsKey(actionArea.Id))
                {
                    diagnostics.Add(string.Format("Duplicate action area id '{0}' was found in {1}.", actionArea.Id, sourceName));
                    continue;
                }

                actionAreaMap[actionArea.Id] = actionArea;
            }

            return actionAreaMap;
        }

        private static Dictionary<string, IDocumentActionDefinition> BuildActionSourceMap(IEnumerable<IDocumentActionDefinition> actions, List<string> diagnostics, string sourceName)
        {
            var actionMap = new Dictionary<string, IDocumentActionDefinition>(StringComparer.OrdinalIgnoreCase);
            if (actions == null)
            {
                return actionMap;
            }

            foreach (var action in actions)
            {
                if (action == null)
                {
                    diagnostics.Add(string.Format("An action from {0} is null.", sourceName));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(action.Id))
                {
                    diagnostics.Add(string.Format("An action from {0} is missing required property 'Id'.", sourceName));
                    continue;
                }

                if (actionMap.ContainsKey(action.Id))
                {
                    diagnostics.Add(string.Format("Duplicate action id '{0}' was found in {1}.", action.Id, sourceName));
                    continue;
                }

                actionMap[action.Id] = action;
            }

            return actionMap;
        }

        private static Dictionary<string, IFieldDefinition> BuildFieldSourceMap(IEnumerable<IFieldDefinition> fields, List<string> diagnostics, string sourceName)
        {
            var fieldMap = new Dictionary<string, IFieldDefinition>(StringComparer.OrdinalIgnoreCase);
            if (fields == null)
            {
                return fieldMap;
            }

            foreach (var field in fields)
            {
                if (field == null)
                {
                    diagnostics.Add(string.Format("A field from {0} is null.", sourceName));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(field.Id))
                {
                    diagnostics.Add(string.Format("A field from {0} is missing required property 'Id'.", sourceName));
                    continue;
                }

                if (fieldMap.ContainsKey(field.Id))
                {
                    diagnostics.Add(string.Format("Duplicate field id '{0}' was found in {1}.", field.Id, sourceName));
                    continue;
                }

                fieldMap[field.Id] = field;
            }

            return fieldMap;
        }

        private static IFieldDefinition ResolveField(
            IFieldDefinition field,
            IDictionary<string, IFieldDefinition> availableFields,
            ISet<string> resolutionStack,
            List<string> diagnostics,
            string sourceName)
        {
            if (field == null)
            {
                diagnostics.Add(string.Format("A field from {0} is null.", sourceName));
                return null;
            }

            if (string.IsNullOrWhiteSpace(field.Id))
            {
                diagnostics.Add(string.Format("A field from {0} is missing required property 'Id'.", sourceName));
                return field;
            }

            if (string.IsNullOrWhiteSpace(field.Extends))
            {
                return CloneField(field);
            }

            if (!resolutionStack.Add(field.Id))
            {
                diagnostics.Add(string.Format("Cyclic field inheritance was detected for field '{0}'.", field.Id));
                return CloneField(field);
            }

            try
            {
                IFieldDefinition baseField;
                if (availableFields == null || !availableFields.TryGetValue(field.Extends, out baseField) || baseField == null)
                {
                    diagnostics.Add(string.Format("Field '{0}' extends unknown base field '{1}'.", field.Id, field.Extends));
                    return CloneField(field);
                }

                var resolvedBase = ResolveField(baseField, availableFields, resolutionStack, diagnostics, sourceName);
                return MergeFields(resolvedBase, field);
            }
            finally
            {
                resolutionStack.Remove(field.Id);
            }
        }

        private static IActionAreaDefinition ResolveActionArea(
            IActionAreaDefinition actionArea,
            IDictionary<string, IActionAreaDefinition> availableActionAreas,
            ISet<string> resolutionStack,
            List<string> diagnostics,
            string sourceName)
        {
            if (actionArea == null)
            {
                diagnostics.Add(string.Format("An action area from {0} is null.", sourceName));
                return null;
            }

            if (string.IsNullOrWhiteSpace(actionArea.Id))
            {
                diagnostics.Add(string.Format("An action area from {0} is missing required property 'Id'.", sourceName));
                return actionArea;
            }

            if (string.IsNullOrWhiteSpace(actionArea.Extends))
            {
                return CloneActionArea(actionArea);
            }

            if (!resolutionStack.Add(actionArea.Id))
            {
                diagnostics.Add(string.Format("Cyclic action area inheritance was detected for action area '{0}'.", actionArea.Id));
                return CloneActionArea(actionArea);
            }

            try
            {
                if (availableActionAreas == null || !availableActionAreas.TryGetValue(actionArea.Extends, out var baseActionArea) || baseActionArea == null)
                {
                    diagnostics.Add(string.Format("Action area '{0}' extends unknown base action area '{1}'.", actionArea.Id, actionArea.Extends));
                    return CloneActionArea(actionArea);
                }

                var resolvedBase = ResolveActionArea(baseActionArea, availableActionAreas, resolutionStack, diagnostics, sourceName);
                return MergeActionAreas(resolvedBase, actionArea);
            }
            finally
            {
                resolutionStack.Remove(actionArea.Id);
            }
        }

        private static IDocumentActionDefinition ResolveAction(
            IDocumentActionDefinition action,
            IDictionary<string, IDocumentActionDefinition> availableActions,
            ISet<string> resolutionStack,
            List<string> diagnostics,
            string sourceName)
        {
            if (action == null)
            {
                diagnostics.Add(string.Format("An action from {0} is null.", sourceName));
                return null;
            }

            if (string.IsNullOrWhiteSpace(action.Id))
            {
                diagnostics.Add(string.Format("An action from {0} is missing required property 'Id'.", sourceName));
                return action;
            }

            if (string.IsNullOrWhiteSpace(action.Extends))
            {
                return CloneAction(action);
            }

            if (!resolutionStack.Add(action.Id))
            {
                diagnostics.Add(string.Format("Cyclic action inheritance was detected for action '{0}'.", action.Id));
                return CloneAction(action);
            }

            try
            {
                if (availableActions == null || !availableActions.TryGetValue(action.Extends, out var baseAction) || baseAction == null)
                {
                    diagnostics.Add(string.Format("Action '{0}' extends unknown base action '{1}'.", action.Id, action.Extends));
                    return CloneAction(action);
                }

                var resolvedBase = ResolveAction(baseAction, availableActions, resolutionStack, diagnostics, sourceName);
                return MergeActions(resolvedBase, action);
            }
            finally
            {
                resolutionStack.Remove(action.Id);
            }
        }

        private static IFieldDefinition CloneField(IFieldDefinition source)
        {
            if (source == null)
            {
                return null;
            }

            var sourceIntegerField = source as IIntegerFieldDefinition;
            if (sourceIntegerField != null)
            {
                return new YamlIntegerFieldDefinition
                {
                    Id = source.Id,
                    Name = source.Name,
                    Extends = source.Extends,
                    CaptionKey = source.CaptionKey,
                    PlaceholderKey = source.PlaceholderKey,
                    Width = source.Width,
                    WidthHint = source.WidthHint,
                    Weight = source.Weight,
                    Visible = source.Visible,
                    Enabled = source.Enabled,
                    ShowOldValueRestoreButton = source.ShowOldValueRestoreButton,
                    ValidationTrigger = source.ValidationTrigger,
                    InteractionMode = source.InteractionMode,
                    DensityMode = source.DensityMode,
                    FieldChromeMode = source.FieldChromeMode,
                    CaptionPlacement = source.CaptionPlacement,
                    IsRequired = source.IsRequired,
                    ShowLabel = source.ShowLabel,
                    ShowPlaceholder = source.ShowPlaceholder,
                    IsTouched = source.IsTouched,
                    IsPristine = source.IsPristine,
                    IsDirty = source.IsDirty,
                    IsValid = source.IsValid,
                    ErrorCode = source.ErrorCode,
                    ErrorMessage = source.ErrorMessage,
                    MinValue = sourceIntegerField.MinValue,
                    MaxValue = sourceIntegerField.MaxValue
                };
            }

            var sourceDocumentEditorField = source as IDocumentEditorFieldDefinition;
            if (sourceDocumentEditorField != null)
            {
                return new YamlDocumentEditorFieldDefinition
                {
                    Id = source.Id,
                    Name = source.Name,
                    Extends = source.Extends,
                    CaptionKey = source.CaptionKey,
                    PlaceholderKey = source.PlaceholderKey,
                    Width = source.Width,
                    WidthHint = source.WidthHint,
                    Weight = source.Weight,
                    Visible = source.Visible,
                    Enabled = source.Enabled,
                    ShowOldValueRestoreButton = source.ShowOldValueRestoreButton,
                    ValidationTrigger = source.ValidationTrigger,
                    InteractionMode = source.InteractionMode,
                    DensityMode = source.DensityMode,
                    FieldChromeMode = source.FieldChromeMode,
                    CaptionPlacement = source.CaptionPlacement,
                    OverlayMode = sourceDocumentEditorField.OverlayMode,
                    IsRequired = source.IsRequired,
                    ShowLabel = source.ShowLabel,
                    ShowPlaceholder = source.ShowPlaceholder,
                    IsTouched = source.IsTouched,
                    IsPristine = source.IsPristine,
                    IsDirty = source.IsDirty,
                    IsValid = source.IsValid,
                    ErrorCode = source.ErrorCode,
                    ErrorMessage = source.ErrorMessage,
                    Value = sourceDocumentEditorField.Value,
                    OldValue = sourceDocumentEditorField.OldValue
                };
            }

            var sourceStringField = source as IStringFieldDefinition;
            var sourceValueField = source as IValueFieldDefinition<string>;
            if (sourceStringField != null || sourceValueField != null)
            {
                return new YamlStringFieldDefinition
                {
                    Id = source.Id,
                    Name = source.Name,
                    Extends = source.Extends,
                    CaptionKey = source.CaptionKey,
                    PlaceholderKey = source.PlaceholderKey,
                    Width = source.Width,
                    WidthHint = source.WidthHint,
                    Weight = source.Weight,
                    Visible = source.Visible,
                    Enabled = source.Enabled,
                    ShowOldValueRestoreButton = source.ShowOldValueRestoreButton,
                    ValidationTrigger = source.ValidationTrigger,
                    InteractionMode = source.InteractionMode,
                    DensityMode = source.DensityMode,
                    FieldChromeMode = source.FieldChromeMode,
                    CaptionPlacement = source.CaptionPlacement,
                    IsRequired = source.IsRequired,
                    ShowLabel = source.ShowLabel,
                    ShowPlaceholder = source.ShowPlaceholder,
                    IsTouched = source.IsTouched,
                    IsPristine = source.IsPristine,
                    IsDirty = source.IsDirty,
                    IsValid = source.IsValid,
                    ErrorCode = source.ErrorCode,
                    ErrorMessage = source.ErrorMessage,
                    MaxLength = sourceStringField == null ? null : sourceStringField.MaxLength,
                    Value = sourceValueField == null ? null : sourceValueField.Value,
                    OldValue = sourceValueField == null ? null : sourceValueField.OldValue
                };
            }

            return new YamlFieldDefinition
            {
                Id = source.Id,
                Name = source.Name,
                Extends = source.Extends,
                CaptionKey = source.CaptionKey,
                PlaceholderKey = source.PlaceholderKey,
                Width = source.Width,
                WidthHint = source.WidthHint,
                Weight = source.Weight,
                Visible = source.Visible,
                Enabled = source.Enabled,
                ShowOldValueRestoreButton = source.ShowOldValueRestoreButton,
                ValidationTrigger = source.ValidationTrigger,
                InteractionMode = source.InteractionMode,
                DensityMode = source.DensityMode,
                FieldChromeMode = source.FieldChromeMode,
                CaptionPlacement = source.CaptionPlacement,
                IsRequired = source.IsRequired,
                ShowLabel = source.ShowLabel,
                ShowPlaceholder = source.ShowPlaceholder,
                IsTouched = source.IsTouched,
                IsPristine = source.IsPristine,
                IsDirty = source.IsDirty,
                IsValid = source.IsValid,
                ErrorCode = source.ErrorCode,
                ErrorMessage = source.ErrorMessage
            };
        }

        private static IActionAreaDefinition MergeActionAreas(IActionAreaDefinition baseActionArea, IActionAreaDefinition overrideActionArea)
        {
            var resolvedBase = CloneActionArea(baseActionArea);
            var resolvedOverride = CloneActionArea(overrideActionArea);
            if (resolvedBase == null)
            {
                return resolvedOverride;
            }

            if (resolvedOverride == null)
            {
                return resolvedBase;
            }

            resolvedBase.Id = FirstNonEmpty(resolvedOverride.Id, resolvedBase.Id);
            resolvedBase.Extends = FirstNonEmpty(resolvedOverride.Extends, resolvedBase.Extends);
            resolvedBase.Name = FirstNonEmpty(resolvedOverride.Name, resolvedBase.Name);
            resolvedBase.Placement = resolvedOverride.Placement ?? resolvedBase.Placement;
            resolvedBase.HorizontalAlignment = resolvedOverride.HorizontalAlignment ?? resolvedBase.HorizontalAlignment;
            resolvedBase.Shared = resolvedOverride.Shared ?? resolvedBase.Shared;
            resolvedBase.Sticky = resolvedOverride.Sticky ?? resolvedBase.Sticky;
            resolvedBase.Visible = resolvedOverride.Visible ?? resolvedBase.Visible;
            return resolvedBase;
        }

        private static IDocumentActionDefinition MergeActions(IDocumentActionDefinition baseAction, IDocumentActionDefinition overrideAction)
        {
            var resolvedBase = CloneAction(baseAction);
            var resolvedOverride = CloneAction(overrideAction);
            if (resolvedBase == null)
            {
                return resolvedOverride;
            }

            if (resolvedOverride == null)
            {
                return resolvedBase;
            }

            resolvedBase.Id = FirstNonEmpty(resolvedOverride.Id, resolvedBase.Id);
            resolvedBase.Extends = FirstNonEmpty(resolvedOverride.Extends, resolvedBase.Extends);
            resolvedBase.Name = FirstNonEmpty(resolvedOverride.Name, resolvedBase.Name);
            resolvedBase.CaptionKey = FirstNonEmpty(resolvedOverride.CaptionKey, resolvedBase.CaptionKey);
            resolvedBase.IconKey = FirstNonEmpty(resolvedOverride.IconKey, resolvedBase.IconKey);
            resolvedBase.Area = FirstNonEmpty(resolvedOverride.Area, resolvedBase.Area);
            resolvedBase.IsPrimary = resolvedOverride.IsPrimary ?? resolvedBase.IsPrimary;
            resolvedBase.Order = resolvedOverride.Order ?? resolvedBase.Order;
            resolvedBase.Slot = resolvedOverride.Slot ?? resolvedBase.Slot;
            resolvedBase.Semantic = resolvedOverride.Semantic ?? resolvedBase.Semantic;
            resolvedBase.Visible = resolvedOverride.Visible ?? resolvedBase.Visible;
            resolvedBase.Enabled = resolvedOverride.Enabled ?? resolvedBase.Enabled;
            return resolvedBase;
        }

        private static IFieldDefinition MergeFields(IFieldDefinition baseField, IFieldDefinition derivedField)
        {
            if (baseField == null)
            {
                return CloneField(derivedField);
            }

            var merged = CloneField(baseField) as YamlFieldDefinition;
            if (merged == null)
            {
                return CloneField(derivedField);
            }

            var mergedIntegerField = merged as YamlIntegerFieldDefinition;
            var mergedDocumentEditorField = merged as YamlDocumentEditorFieldDefinition;
            var mergedStringField = merged as YamlStringFieldDefinition;
            var derivedStringField = derivedField as IValueFieldDefinition<string>;

            merged.Id = derivedField.Id;
            merged.Name = FirstNonEmpty(derivedField.Name, baseField.Name);
            merged.Extends = derivedField.Extends;
            merged.CaptionKey = FirstNonEmpty(derivedField.CaptionKey, baseField.CaptionKey);
            merged.PlaceholderKey = FirstNonEmpty(derivedField.PlaceholderKey, baseField.PlaceholderKey);
            ApplyWidthOverride(derivedField.Width, derivedField.WidthHint, baseField.Width, baseField.WidthHint, out var mergedWidth, out var mergedWidthHint);
            merged.Width = mergedWidth;
            merged.WidthHint = mergedWidthHint;
            merged.Weight = derivedField.Weight ?? baseField.Weight;
            merged.Visible = derivedField.Visible ?? baseField.Visible;
            merged.Enabled = derivedField.Enabled ?? baseField.Enabled;
            merged.ShowOldValueRestoreButton = derivedField.ShowOldValueRestoreButton ?? baseField.ShowOldValueRestoreButton;
            merged.IsRequired = derivedField.IsRequired || baseField.IsRequired;
            merged.ShowLabel = derivedField.ShowLabel != baseField.ShowLabel ? derivedField.ShowLabel : baseField.ShowLabel;
            merged.ShowPlaceholder = derivedField.ShowPlaceholder != baseField.ShowPlaceholder ? derivedField.ShowPlaceholder : baseField.ShowPlaceholder;
            merged.ValidationTrigger = derivedField.ValidationTrigger ?? baseField.ValidationTrigger;
            merged.InteractionMode = derivedField.InteractionMode ?? baseField.InteractionMode;
            merged.DensityMode = derivedField.DensityMode ?? baseField.DensityMode;
            merged.FieldChromeMode = derivedField.FieldChromeMode ?? baseField.FieldChromeMode;
            merged.CaptionPlacement = derivedField.CaptionPlacement ?? baseField.CaptionPlacement;
            if (mergedDocumentEditorField != null)
            {
                var derivedDocumentEditorField = derivedField as IDocumentEditorFieldDefinition;
                var baseDocumentEditorField = baseField as IDocumentEditorFieldDefinition;
                mergedDocumentEditorField.OverlayMode = derivedDocumentEditorField != null && derivedDocumentEditorField.OverlayMode.HasValue
                    ? derivedDocumentEditorField.OverlayMode
                    : baseDocumentEditorField == null ? null : baseDocumentEditorField.OverlayMode;
            }
            merged.IsTouched = derivedField.IsTouched;
            merged.IsPristine = derivedField.IsPristine;
            merged.IsDirty = derivedField.IsDirty;
            merged.IsValid = derivedField.IsValid;
            merged.ErrorCode = FirstNonEmpty(derivedField.ErrorCode, baseField.ErrorCode);
            merged.ErrorMessage = FirstNonEmpty(derivedField.ErrorMessage, baseField.ErrorMessage);

            if (mergedDocumentEditorField != null && derivedStringField != null)
            {
                mergedDocumentEditorField.Value = derivedStringField.Value ?? mergedDocumentEditorField.Value;
                mergedDocumentEditorField.OldValue = derivedStringField.OldValue ?? mergedDocumentEditorField.OldValue;
            }

            if (mergedStringField != null)
            {
                var baseStringField = baseField as IStringFieldDefinition;
                var derivedTypedStringField = derivedField as IStringFieldDefinition;
                mergedStringField.MaxLength = derivedTypedStringField?.MaxLength ?? baseStringField?.MaxLength;
            }

            if (mergedStringField != null && derivedStringField != null)
            {
                mergedStringField.Value = derivedStringField.Value ?? mergedStringField.Value;
                mergedStringField.OldValue = derivedStringField.OldValue ?? mergedStringField.OldValue;
            }

            if (mergedIntegerField != null)
            {
                var baseIntegerField = baseField as IIntegerFieldDefinition;
                var derivedIntegerField = derivedField as IIntegerFieldDefinition;
                mergedIntegerField.MinValue = derivedIntegerField?.MinValue ?? baseIntegerField?.MinValue;
                mergedIntegerField.MaxValue = derivedIntegerField?.MaxValue ?? baseIntegerField?.MaxValue;
            }

            return merged;
        }

        private static string FirstNonEmpty(string first, string second)
        {
            return string.IsNullOrWhiteSpace(first) ? second : first;
        }

        private static void RegisterField(IFieldDefinition field, IDictionary<string, IFieldDefinition> fieldMap, List<string> diagnostics, string sourceName)
        {
            if (field == null)
            {
                diagnostics.Add(string.Format("A field from {0} is null.", sourceName));
                return;
            }

            if (string.IsNullOrWhiteSpace(field.Id))
            {
                diagnostics.Add(string.Format("A field from {0} is missing required property 'Id'.", sourceName));
                return;
            }

            if (fieldMap.ContainsKey(field.Id))
            {
                diagnostics.Add(string.Format("Duplicate field id '{0}' was found in {1}.", field.Id, sourceName));
                return;
            }

            fieldMap[field.Id] = field;
        }

        private static void RegisterActionArea(IActionAreaDefinition actionArea, IDictionary<string, IActionAreaDefinition> actionAreaMap, List<string> diagnostics, string sourceName)
        {
            if (actionArea == null)
            {
                diagnostics.Add(string.Format("An action area from {0} is null.", sourceName));
                return;
            }

            if (string.IsNullOrWhiteSpace(actionArea.Id))
            {
                diagnostics.Add(string.Format("An action area from {0} is missing required property 'Id'.", sourceName));
                return;
            }

            if (actionAreaMap.ContainsKey(actionArea.Id))
            {
                diagnostics.Add(string.Format("Duplicate action area id '{0}' was found in {1}.", actionArea.Id, sourceName));
                return;
            }

            actionAreaMap[actionArea.Id] = actionArea;
        }

        private static void RegisterAction(IDocumentActionDefinition action, IDictionary<string, IDocumentActionDefinition> actionMap, List<string> diagnostics, string sourceName)
        {
            if (action == null)
            {
                diagnostics.Add(string.Format("An action from {0} is null.", sourceName));
                return;
            }

            if (string.IsNullOrWhiteSpace(action.Id))
            {
                diagnostics.Add(string.Format("An action from {0} is missing required property 'Id'.", sourceName));
                return;
            }

            if (actionMap.ContainsKey(action.Id))
            {
                diagnostics.Add(string.Format("Duplicate action id '{0}' was found in {1}.", action.Id, sourceName));
                return;
            }

            actionMap[action.Id] = action;
        }

        private static void ValidateFieldReferences(YamlLayoutDefinition layout, IDictionary<string, IFieldDefinition> fieldMap, List<string> diagnostics)
        {
            foreach (var item in layout.Items)
            {
                ValidateFieldReference(item, fieldMap, diagnostics);
            }
        }

        private static void ValidateActionAreaReferences(IEnumerable<IDocumentActionDefinition> actions, IDictionary<string, IActionAreaDefinition> actionAreaMap, List<string> diagnostics)
        {
            if (actions == null)
            {
                return;
            }

            foreach (var action in actions)
            {
                if (action == null || string.IsNullOrWhiteSpace(action.Area))
                {
                    continue;
                }

                if (actionAreaMap == null || !actionAreaMap.ContainsKey(action.Area))
                {
                    diagnostics.Add(string.Format("Action '{0}' references unknown action area '{1}'.", action.Id ?? "<unknown>", action.Area));
                }
            }
        }

        private static void ValidateFieldReference(ILayoutItemDefinition item, IDictionary<string, IFieldDefinition> fieldMap, List<string> diagnostics)
        {
            if (item == null)
            {
                return;
            }

            if (item is IFieldReferenceDefinition fieldReference)
            {
                if (string.IsNullOrWhiteSpace(fieldReference.FieldRef))
                {
                    diagnostics.Add("A field reference layout item is missing required property 'FieldRef'.");
                }
                else if (!fieldMap.ContainsKey(fieldReference.FieldRef))
                {
                    diagnostics.Add(string.Format("Layout references unknown field '{0}'.", fieldReference.FieldRef));
                }

                return;
            }

            if (item is ILayoutContainerDefinition container)
            {
                foreach (var child in container.Items)
                {
                    ValidateFieldReference(child, fieldMap, diagnostics);
                }
            }
        }

        private static ResolvedDocumentDefinition BuildResolvedDocument(YamlDocumentDefinition normalized)
        {
            if (normalized == null)
            {
                return null;
            }

            var effectiveVisible = normalized.Visible ?? true;
            var effectiveEnabled = normalized.Enabled ?? true;
            var effectiveShowOldValueRestoreButton = normalized.ShowOldValueRestoreButton ?? false;
            var effectiveValidationTrigger = normalized.ValidationTrigger ?? Abstractions.Enums.ValidationTrigger.OnBlur;
            var effectiveInteractionMode = normalized.InteractionMode ?? Abstractions.Enums.InteractionMode.Classic;
            var effectiveDensityMode = normalized.DensityMode;
            var effectiveFieldChromeMode = normalized.FieldChromeMode ?? Abstractions.Enums.FieldChromeMode.Framed;
            var effectiveCaptionPlacement = normalized.CaptionPlacement ?? Abstractions.Enums.CaptionPlacement.Top;
            var effectiveTopRegionChrome = normalized.TopRegionChrome ?? Abstractions.Enums.DocumentRegionChromeMode.Separate;
            var effectiveBottomRegionChrome = normalized.BottomRegionChrome ?? Abstractions.Enums.DocumentRegionChromeMode.Separate;
            var effectiveWidth = normalized.Width;
            var effectiveWidthHint = normalized.WidthHint;
            var resolvedHeader = normalized.Header == null
                ? null
                : new ResolvedDocumentHeaderDefinition(normalized.Header, normalized.Header.Visible ?? true);
            var resolvedFooter = normalized.Footer == null
                ? null
                : new ResolvedDocumentFooterDefinition(normalized.Footer, normalized.Footer.Visible ?? true);

            var resolvedLayout = BuildResolvedLayout(
                normalized.Layout,
                effectiveVisible,
                effectiveEnabled,
                effectiveShowOldValueRestoreButton,
                effectiveValidationTrigger,
                effectiveInteractionMode,
                effectiveDensityMode,
                effectiveFieldChromeMode,
                effectiveCaptionPlacement,
                effectiveWidth,
                effectiveWidthHint,
                new Dictionary<string, ResolvedFieldDefinition>(StringComparer.OrdinalIgnoreCase));

            return new ResolvedDocumentDefinition(
                normalized.Id,
                normalized.Name,
                normalized.Kind,
                effectiveTopRegionChrome,
                effectiveBottomRegionChrome,
                effectiveWidth,
                effectiveWidthHint,
                effectiveVisible,
                effectiveEnabled,
                effectiveShowOldValueRestoreButton,
                effectiveValidationTrigger,
                effectiveInteractionMode,
                effectiveDensityMode,
                effectiveFieldChromeMode,
                effectiveCaptionPlacement,
                resolvedHeader,
                resolvedFooter,
                resolvedLayout);
        }

        private static ResolvedFormDocumentDefinition BuildResolvedFormDocument(YamlFormDocumentDefinition normalized)
        {
            if (normalized == null)
            {
                return null;
            }

            var effectiveFormVisible = normalized.Visible ?? true;
            var effectiveFormEnabled = normalized.Enabled ?? true;
            var effectiveFormShowOldValueRestoreButton = normalized.ShowOldValueRestoreButton ?? false;
            var effectiveFormValidationTrigger = normalized.ValidationTrigger ?? Abstractions.Enums.ValidationTrigger.OnBlur;
            var effectiveFormInteractionMode = normalized.InteractionMode ?? Abstractions.Enums.InteractionMode.Classic;
            var effectiveFormDensityMode = normalized.DensityMode;
            var effectiveFormFieldChromeMode = normalized.FieldChromeMode ?? Abstractions.Enums.FieldChromeMode.Framed;
            var effectiveFormCaptionPlacement = normalized.CaptionPlacement ?? Abstractions.Enums.CaptionPlacement.Top;
            var effectiveTopRegionChrome = normalized.TopRegionChrome ?? Abstractions.Enums.DocumentRegionChromeMode.Separate;
            var effectiveBottomRegionChrome = normalized.BottomRegionChrome ?? Abstractions.Enums.DocumentRegionChromeMode.Separate;
            var effectiveFormWidth = normalized.Width;
            var effectiveFormWidthHint = normalized.WidthHint;
            var resolvedHeader = normalized.Header == null
                ? null
                : new ResolvedDocumentHeaderDefinition(normalized.Header, normalized.Header.Visible ?? true);
            var resolvedFooter = normalized.Footer == null
                ? null
                : new ResolvedDocumentFooterDefinition(normalized.Footer, normalized.Footer.Visible ?? true);
            var resolvedFields = new List<ResolvedFieldDefinition>();
            var resolvedFieldMap = new Dictionary<string, ResolvedFieldDefinition>(StringComparer.OrdinalIgnoreCase);
            var defaultFieldWidthHint = effectiveFormWidthHint ?? Abstractions.Enums.FieldWidthHint.Medium;
            foreach (var field in normalized.Fields)
            {
                var resolvedFieldWidth = ResolveWidth(field.Width, field.WidthHint, effectiveFormWidth, defaultFieldWidthHint, out var resolvedFieldWidthHint);
                var resolvedField = new ResolvedFieldDefinition(
                    field,
                    resolvedFieldWidth,
                    resolvedFieldWidthHint,
                    field.Visible ?? effectiveFormVisible,
                    field.Enabled ?? effectiveFormEnabled,
                    field.ShowOldValueRestoreButton ?? effectiveFormShowOldValueRestoreButton,
                    field.ValidationTrigger ?? effectiveFormValidationTrigger,
                    field.InteractionMode ?? effectiveFormInteractionMode,
                    field.DensityMode ?? effectiveFormDensityMode,
                    field.FieldChromeMode ?? effectiveFormFieldChromeMode,
                    field.CaptionPlacement ?? effectiveFormCaptionPlacement);

                resolvedFields.Add(resolvedField);
                if (!string.IsNullOrWhiteSpace(resolvedField.Id))
                {
                    resolvedFieldMap[resolvedField.Id] = resolvedField;
                }
            }

            var resolvedActions = new List<ResolvedDocumentActionDefinition>();
            var resolvedActionAreas = new List<ResolvedActionAreaDefinition>();
            var resolvedActionAreaMap = new Dictionary<string, ResolvedActionAreaDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var actionArea in normalized.ActionAreas)
            {
                if (actionArea == null)
                {
                    continue;
                }

                var resolvedActionArea = new ResolvedActionAreaDefinition(
                    actionArea,
                    actionArea.Placement ?? ActionPlacement.Bottom,
                    actionArea.HorizontalAlignment ?? ActionAlignment.Right,
                    actionArea.ChromeMode ?? ActionAreaChromeMode.Explicit,
                    actionArea.Shared ?? true,
                    actionArea.Sticky ?? false,
                    actionArea.Visible ?? true);
                resolvedActionAreas.Add(resolvedActionArea);
                if (!string.IsNullOrWhiteSpace(resolvedActionArea.Id))
                {
                    resolvedActionAreaMap[resolvedActionArea.Id] = resolvedActionArea;
                }
            }

            foreach (var action in normalized.Actions)
            {
                if (action == null)
                {
                    continue;
                }

                resolvedActions.Add(new ResolvedDocumentActionDefinition(
                    action,
                    action.Visible ?? effectiveFormVisible,
                    action.Enabled ?? effectiveFormEnabled));
            }

            var resolvedLayout = BuildResolvedLayout(
                normalized.Layout,
                effectiveFormVisible,
                effectiveFormEnabled,
                effectiveFormShowOldValueRestoreButton,
                effectiveFormValidationTrigger,
                effectiveFormInteractionMode,
                effectiveFormDensityMode,
                effectiveFormFieldChromeMode,
                effectiveFormCaptionPlacement,
                effectiveFormWidth,
                effectiveFormWidthHint,
                resolvedFieldMap);
            ApplyLayoutFieldPresentation(resolvedLayout, resolvedFieldMap, resolvedFields);

            return new ResolvedFormDocumentDefinition(
                normalized.Id,
                normalized.Name,
                normalized.Kind,
                effectiveTopRegionChrome,
                effectiveBottomRegionChrome,
                effectiveFormWidth,
                effectiveFormWidthHint,
                effectiveFormVisible,
                effectiveFormEnabled,
                effectiveFormShowOldValueRestoreButton,
                effectiveFormValidationTrigger,
                effectiveFormInteractionMode,
                effectiveFormDensityMode,
                effectiveFormFieldChromeMode,
                effectiveFormCaptionPlacement,
                resolvedHeader,
                resolvedFooter,
                resolvedLayout,
                resolvedActionAreas,
                resolvedFields,
                resolvedActions,
                resolvedFieldMap);
        }

        private static ResolvedLayoutDefinition BuildResolvedLayout(
            YamlLayoutDefinition layout,
            bool inheritedVisible,
            bool inheritedEnabled,
            bool inheritedShowOldValueRestoreButton,
            ValidationTrigger inheritedValidationTrigger,
            InteractionMode inheritedInteractionMode,
            DensityMode? inheritedDensityMode,
            FieldChromeMode inheritedFieldChromeMode,
            CaptionPlacement inheritedCaptionPlacement,
            double? inheritedWidth,
            FieldWidthHint? inheritedWidthHint,
            IReadOnlyDictionary<string, ResolvedFieldDefinition> fieldMap)
        {
            if (layout == null)
            {
                return null;
            }

            var effectiveVisible = layout.Visible ?? inheritedVisible;
            var effectiveEnabled = layout.Enabled ?? inheritedEnabled;
            var effectiveShowOldValueRestoreButton = layout.ShowOldValueRestoreButton ?? inheritedShowOldValueRestoreButton;
            var effectiveValidationTrigger = layout.ValidationTrigger ?? inheritedValidationTrigger;
            var effectiveInteractionMode = layout.InteractionMode ?? inheritedInteractionMode;
            var effectiveDensityMode = layout.DensityMode ?? inheritedDensityMode;
            var effectiveFieldChromeMode = layout.FieldChromeMode ?? inheritedFieldChromeMode;
            var effectiveCaptionPlacement = layout.CaptionPlacement ?? inheritedCaptionPlacement;
            var effectiveWidth = ResolveWidth(layout.Width, layout.WidthHint, inheritedWidth, inheritedWidthHint, out var effectiveWidthHint);
            var items = new List<ResolvedLayoutItemDefinition>();

            foreach (var item in layout.Items)
            {
                var resolvedItem = BuildResolvedLayoutItem(
                    item,
                    effectiveVisible,
                    effectiveEnabled,
                    effectiveShowOldValueRestoreButton,
                    effectiveValidationTrigger,
                    effectiveInteractionMode,
                    effectiveDensityMode,
                    effectiveFieldChromeMode,
                    effectiveCaptionPlacement,
                    effectiveWidth,
                    effectiveWidthHint,
                    fieldMap);
                if (resolvedItem != null)
                {
                    items.Add(resolvedItem);
                }
            }

            return new ResolvedLayoutDefinition(
                layout.Id,
                layout.Name,
                effectiveWidth,
                effectiveWidthHint,
                layout.IsOverlayScope,
                effectiveVisible,
                effectiveEnabled,
                effectiveShowOldValueRestoreButton,
                effectiveValidationTrigger,
                effectiveInteractionMode,
                effectiveDensityMode,
                effectiveFieldChromeMode,
                effectiveCaptionPlacement,
                items);
        }

        private static ResolvedLayoutItemDefinition BuildResolvedLayoutItem(
            ILayoutItemDefinition item,
            bool inheritedVisible,
            bool inheritedEnabled,
            bool inheritedShowOldValueRestoreButton,
            ValidationTrigger inheritedValidationTrigger,
            InteractionMode inheritedInteractionMode,
            DensityMode? inheritedDensityMode,
            FieldChromeMode inheritedFieldChromeMode,
            CaptionPlacement inheritedCaptionPlacement,
            double? inheritedWidth,
            FieldWidthHint? inheritedWidthHint,
            IReadOnlyDictionary<string, ResolvedFieldDefinition> fieldMap)
        {
            if (item == null)
            {
                return null;
            }

            if (item is IFieldReferenceDefinition fieldReference)
            {
                var field = string.IsNullOrWhiteSpace(fieldReference.FieldRef) || !fieldMap.ContainsKey(fieldReference.FieldRef)
                    ? null
                    : fieldMap[fieldReference.FieldRef];

                var effectiveVisible = field == null ? inheritedVisible : field.Definition.Visible ?? inheritedVisible;
                var effectiveEnabled = field == null ? inheritedEnabled : field.Definition.Enabled ?? inheritedEnabled;
                var effectiveShowOldValueRestoreButton = field == null ? inheritedShowOldValueRestoreButton : field.Definition.ShowOldValueRestoreButton ?? inheritedShowOldValueRestoreButton;
                var effectiveValidationTrigger = field == null ? inheritedValidationTrigger : field.Definition.ValidationTrigger ?? inheritedValidationTrigger;
                var effectiveInteractionMode = field == null ? inheritedInteractionMode : field.Definition.InteractionMode ?? inheritedInteractionMode;
                var effectiveDensityMode = field == null ? inheritedDensityMode : field.Definition.DensityMode ?? inheritedDensityMode;
                var effectiveFieldChromeMode = field == null ? inheritedFieldChromeMode : field.Definition.FieldChromeMode ?? inheritedFieldChromeMode;
                var effectiveCaptionPlacement = field == null ? inheritedCaptionPlacement : field.Definition.CaptionPlacement ?? inheritedCaptionPlacement;
                FieldWidthHint? effectiveWidthHint;
                var effectiveWidth = field == null
                    ? ResolveWidth(null, null, inheritedWidth, inheritedWidthHint, out effectiveWidthHint)
                    : ResolveWidth(field.Width, field.WidthHint, inheritedWidth, inheritedWidthHint, out effectiveWidthHint);
                var effectiveField = field == null
                    ? null
                    : field.WithPresentation(
                        effectiveWidth,
                        effectiveWidthHint,
                        effectiveVisible,
                        effectiveEnabled,
                        effectiveShowOldValueRestoreButton,
                        effectiveValidationTrigger,
                        effectiveInteractionMode,
                        effectiveDensityMode,
                        effectiveFieldChromeMode,
                        effectiveCaptionPlacement);

                return new ResolvedFieldReferenceDefinition(
                    item.Id,
                    item.Name,
                    effectiveWidth,
                    effectiveWidthHint,
                    effectiveVisible,
                    effectiveEnabled,
                    effectiveShowOldValueRestoreButton,
                    effectiveValidationTrigger,
                    effectiveInteractionMode,
                    effectiveDensityMode,
                    effectiveFieldChromeMode,
                    effectiveCaptionPlacement,
                    effectiveField);
            }

            if (item is IContainerDefinition container)
            {
                var effectiveVisible = container.Visible ?? inheritedVisible;
                var effectiveEnabled = container.Enabled ?? inheritedEnabled;
                var effectiveShowOldValueRestoreButton = container.ShowOldValueRestoreButton ?? inheritedShowOldValueRestoreButton;
                var effectiveValidationTrigger = container.ValidationTrigger ?? inheritedValidationTrigger;
                var effectiveInteractionMode = container.InteractionMode ?? inheritedInteractionMode;
                var effectiveDensityMode = container.DensityMode ?? inheritedDensityMode;
                var effectiveFieldChromeMode = container.FieldChromeMode ?? inheritedFieldChromeMode;
                var effectiveCaptionPlacement = container.CaptionPlacement ?? inheritedCaptionPlacement;
                var effectiveWidth = ResolveWidth(container.Width, container.WidthHint, inheritedWidth, inheritedWidthHint, out var effectiveWidthHint);
                var items = new List<ResolvedLayoutItemDefinition>();
                foreach (var child in container.Items)
                {
                    var resolvedChild = BuildResolvedLayoutItem(
                        child,
                        effectiveVisible,
                        effectiveEnabled,
                        effectiveShowOldValueRestoreButton,
                        effectiveValidationTrigger,
                        effectiveInteractionMode,
                        effectiveDensityMode,
                        effectiveFieldChromeMode,
                        effectiveCaptionPlacement,
                        effectiveWidth,
                        effectiveWidthHint,
                        fieldMap);
                    if (resolvedChild != null)
                    {
                        items.Add(resolvedChild);
                    }
                }

                return new ResolvedContainerDefinition(
                    container.Id,
                    container.Name,
                    effectiveWidth,
                    effectiveWidthHint,
                    container.IsOverlayScope,
                    effectiveVisible,
                    effectiveEnabled,
                    effectiveShowOldValueRestoreButton,
                    effectiveValidationTrigger,
                    effectiveInteractionMode,
                    effectiveDensityMode,
                    effectiveFieldChromeMode,
                    effectiveCaptionPlacement,
                    container.CaptionKey,
                    container.ShowBorder,
                    container.Variant ?? ContainerVariant.Standard,
                    items);
            }

            if (item is IBadgeDefinition badge)
            {
                var effectiveVisible = badge.Visible ?? inheritedVisible;
                var effectiveEnabled = badge.Enabled ?? inheritedEnabled;
                var effectiveShowOldValueRestoreButton = badge.ShowOldValueRestoreButton ?? inheritedShowOldValueRestoreButton;
                var effectiveValidationTrigger = badge.ValidationTrigger ?? inheritedValidationTrigger;
                var effectiveInteractionMode = badge.InteractionMode ?? inheritedInteractionMode;
                var effectiveDensityMode = badge.DensityMode ?? inheritedDensityMode;
                var effectiveFieldChromeMode = badge.FieldChromeMode ?? inheritedFieldChromeMode;
                var effectiveCaptionPlacement = badge.CaptionPlacement ?? inheritedCaptionPlacement;
                var effectiveWidth = ResolveWidth(badge.Width, badge.WidthHint, inheritedWidth, inheritedWidthHint, out var effectiveWidthHint);

                return new ResolvedBadgeDefinition(
                    badge.Id,
                    badge.Name,
                    effectiveWidth,
                    effectiveWidthHint,
                    effectiveVisible,
                    effectiveEnabled,
                    effectiveShowOldValueRestoreButton,
                    effectiveValidationTrigger,
                    effectiveInteractionMode,
                    effectiveDensityMode,
                    effectiveFieldChromeMode,
                    effectiveCaptionPlacement,
                    badge.TextKey,
                    badge.IconKey,
                    badge.ToolTipKey,
                    badge.Tone ?? BadgeTone.Neutral,
                    badge.Variant ?? BadgeVariant.Soft,
                    badge.Size ?? BadgeSize.Regular,
                    badge.IconPlacement ?? IconPlacement.Leading);
            }

            if (item is IButtonDefinition button)
            {
                var effectiveVisible = button.Visible ?? inheritedVisible;
                var effectiveEnabled = button.Enabled ?? inheritedEnabled;
                var effectiveShowOldValueRestoreButton = button.ShowOldValueRestoreButton ?? inheritedShowOldValueRestoreButton;
                var effectiveValidationTrigger = button.ValidationTrigger ?? inheritedValidationTrigger;
                var effectiveInteractionMode = button.InteractionMode ?? inheritedInteractionMode;
                var effectiveDensityMode = button.DensityMode ?? inheritedDensityMode;
                var effectiveFieldChromeMode = button.FieldChromeMode ?? inheritedFieldChromeMode;
                var effectiveCaptionPlacement = button.CaptionPlacement ?? inheritedCaptionPlacement;
                var effectiveWidth = ResolveWidth(button.Width, button.WidthHint, inheritedWidth, inheritedWidthHint, out var effectiveWidthHint);

                return new ResolvedButtonDefinition(
                    button.Id,
                    button.Name,
                    effectiveWidth,
                    effectiveWidthHint,
                    effectiveVisible,
                    effectiveEnabled,
                    effectiveShowOldValueRestoreButton,
                    effectiveValidationTrigger,
                    effectiveInteractionMode,
                    effectiveDensityMode,
                    effectiveFieldChromeMode,
                    effectiveCaptionPlacement,
                    button.TextKey,
                    button.IconKey,
                    button.ToolTipKey,
                    button.CommandId,
                    button.Tone ?? ButtonTone.Secondary,
                    button.Variant ?? ButtonVariant.Standard,
                    button.Size ?? ButtonSize.Regular,
                    button.IconPlacement ?? IconPlacement.Leading);
            }

            if (item is IRowDefinition row)
            {
                var effectiveVisible = row.Visible ?? inheritedVisible;
                var effectiveEnabled = row.Enabled ?? inheritedEnabled;
                var effectiveShowOldValueRestoreButton = row.ShowOldValueRestoreButton ?? inheritedShowOldValueRestoreButton;
                var effectiveValidationTrigger = row.ValidationTrigger ?? inheritedValidationTrigger;
                var effectiveInteractionMode = row.InteractionMode ?? inheritedInteractionMode;
                var effectiveDensityMode = row.DensityMode ?? inheritedDensityMode;
                var effectiveFieldChromeMode = row.FieldChromeMode ?? inheritedFieldChromeMode;
                var effectiveCaptionPlacement = row.CaptionPlacement ?? inheritedCaptionPlacement;
                var effectiveWidth = ResolveWidth(row.Width, row.WidthHint, inheritedWidth, inheritedWidthHint, out var effectiveWidthHint);
                var items = new List<ResolvedLayoutItemDefinition>();
                foreach (var child in row.Items)
                {
                    var resolvedChild = BuildResolvedLayoutItem(
                        child,
                        effectiveVisible,
                        effectiveEnabled,
                        effectiveShowOldValueRestoreButton,
                        effectiveValidationTrigger,
                        effectiveInteractionMode,
                        effectiveDensityMode,
                        effectiveFieldChromeMode,
                        effectiveCaptionPlacement,
                        effectiveWidth,
                        effectiveWidthHint,
                        fieldMap);
                    if (resolvedChild != null)
                    {
                        items.Add(resolvedChild);
                    }
                }

                return new ResolvedRowDefinition(
                    row.Id,
                    row.Name,
                    effectiveWidth,
                    effectiveWidthHint,
                    row.IsOverlayScope,
                    effectiveVisible,
                    effectiveEnabled,
                    effectiveShowOldValueRestoreButton,
                    effectiveValidationTrigger,
                    effectiveInteractionMode,
                    effectiveDensityMode,
                    effectiveFieldChromeMode,
                    effectiveCaptionPlacement,
                    items);
            }

            if (item is IColumnDefinition column)
            {
                var effectiveVisible = column.Visible ?? inheritedVisible;
                var effectiveEnabled = column.Enabled ?? inheritedEnabled;
                var effectiveShowOldValueRestoreButton = column.ShowOldValueRestoreButton ?? inheritedShowOldValueRestoreButton;
                var effectiveValidationTrigger = column.ValidationTrigger ?? inheritedValidationTrigger;
                var effectiveInteractionMode = column.InteractionMode ?? inheritedInteractionMode;
                var effectiveDensityMode = column.DensityMode ?? inheritedDensityMode;
                var effectiveFieldChromeMode = column.FieldChromeMode ?? inheritedFieldChromeMode;
                var effectiveCaptionPlacement = column.CaptionPlacement ?? inheritedCaptionPlacement;
                var effectiveWidth = ResolveWidth(column.Width, column.WidthHint, inheritedWidth, inheritedWidthHint, out var effectiveWidthHint);
                var items = new List<ResolvedLayoutItemDefinition>();
                foreach (var child in column.Items)
                {
                    var resolvedChild = BuildResolvedLayoutItem(
                        child,
                        effectiveVisible,
                        effectiveEnabled,
                        effectiveShowOldValueRestoreButton,
                        effectiveValidationTrigger,
                        effectiveInteractionMode,
                        effectiveDensityMode,
                        effectiveFieldChromeMode,
                        effectiveCaptionPlacement,
                        effectiveWidth,
                        effectiveWidthHint,
                        fieldMap);
                    if (resolvedChild != null)
                    {
                        items.Add(resolvedChild);
                    }
                }

                return new ResolvedColumnDefinition(
                    column.Id,
                    column.Name,
                    effectiveWidth,
                    effectiveWidthHint,
                    column.IsOverlayScope,
                    effectiveVisible,
                    effectiveEnabled,
                    effectiveShowOldValueRestoreButton,
                    effectiveValidationTrigger,
                    effectiveInteractionMode,
                    effectiveDensityMode,
                    effectiveFieldChromeMode,
                    effectiveCaptionPlacement,
                    items);
            }

            return null;
        }

        private static void ApplyLayoutFieldPresentation(
            ResolvedLayoutDefinition layout,
            IDictionary<string, ResolvedFieldDefinition> fieldMap,
            IList<ResolvedFieldDefinition> fields)
        {
            if (layout == null || fieldMap == null || fields == null)
            {
                return;
            }

            var replacements = new Dictionary<string, ResolvedFieldDefinition>(StringComparer.OrdinalIgnoreCase);
            CollectFieldPresentation(layout.Items, replacements);
            if (replacements.Count == 0)
            {
                return;
            }

            foreach (var pair in replacements)
            {
                fieldMap[pair.Key] = pair.Value;
            }

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field != null && !string.IsNullOrWhiteSpace(field.Id) && replacements.ContainsKey(field.Id))
                {
                    fields[i] = replacements[field.Id];
                }
            }
        }

        private static void CollectFieldPresentation(
            IEnumerable<ResolvedLayoutItemDefinition> items,
            IDictionary<string, ResolvedFieldDefinition> replacements)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item is ResolvedFieldReferenceDefinition fieldReference &&
                    fieldReference.Field != null &&
                    !string.IsNullOrWhiteSpace(fieldReference.Field.Id) &&
                    !replacements.ContainsKey(fieldReference.Field.Id))
                {
                    replacements[fieldReference.Field.Id] = fieldReference.Field;
                }

                if (item is ResolvedLayoutContainerDefinition container)
                {
                    CollectFieldPresentation(container.Items, replacements);
                }
            }
        }

        private static void ApplyWidthOverride(
            double? localWidth,
            FieldWidthHint? localWidthHint,
            double? inheritedWidth,
            FieldWidthHint? inheritedWidthHint,
            out double? effectiveWidth,
            out FieldWidthHint? effectiveWidthHint)
        {
            if (localWidth.HasValue)
            {
                effectiveWidth = localWidth;
                effectiveWidthHint = null;
                return;
            }

            if (localWidthHint.HasValue)
            {
                effectiveWidth = null;
                effectiveWidthHint = localWidthHint;
                return;
            }

            effectiveWidth = inheritedWidth;
            effectiveWidthHint = inheritedWidthHint;
        }

        private static double? ResolveWidth(
            double? localWidth,
            FieldWidthHint? localWidthHint,
            double? inheritedWidth,
            FieldWidthHint? inheritedWidthHint,
            out FieldWidthHint? effectiveWidthHint)
        {
            ApplyWidthOverride(localWidth, localWidthHint, inheritedWidth, inheritedWidthHint, out var effectiveWidth, out effectiveWidthHint);
            return effectiveWidth;
        }
    }
}








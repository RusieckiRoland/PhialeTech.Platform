using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedFormDocumentDefinition : ResolvedDocumentDefinition
    {
        public ResolvedFormDocumentDefinition(
            string id,
            string name,
            DocumentKind? kind,
            double? width,
            FieldWidthHint? widthHint,
            bool visible,
            bool enabled,
            bool showOldValueRestoreButton,
            ValidationTrigger validationTrigger,
            InteractionMode interactionMode,
            DensityMode? densityMode,
            FieldChromeMode fieldChromeMode,
            CaptionPlacement captionPlacement,
            ResolvedLayoutDefinition layout,
            IReadOnlyList<ResolvedActionAreaDefinition> actionAreas,
            IReadOnlyList<ResolvedFieldDefinition> fields,
            IReadOnlyList<ResolvedDocumentActionDefinition> actions,
            IReadOnlyDictionary<string, ResolvedFieldDefinition> fieldMap)
            : base(
                id,
                name,
                kind,
                width,
                widthHint,
                visible,
                enabled,
                showOldValueRestoreButton,
                validationTrigger,
                interactionMode,
                densityMode,
                fieldChromeMode,
                captionPlacement,
                layout)
        {
            ActionAreas = actionAreas;
            Fields = fields;
            Actions = actions;
            FieldMap = fieldMap;
        }

        public IReadOnlyList<ResolvedActionAreaDefinition> ActionAreas { get; }

        public IReadOnlyList<ResolvedFieldDefinition> Fields { get; }

        public IReadOnlyList<ResolvedDocumentActionDefinition> Actions { get; }

        public IReadOnlyDictionary<string, ResolvedFieldDefinition> FieldMap { get; }
    }
}

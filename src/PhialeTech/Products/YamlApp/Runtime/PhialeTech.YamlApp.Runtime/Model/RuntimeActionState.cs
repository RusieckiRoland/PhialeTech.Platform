using PhialeTech.YamlApp.Core.Resolved;

namespace PhialeTech.YamlApp.Runtime.Model
{
    public sealed class RuntimeActionState
    {
        public RuntimeActionState(ResolvedDocumentActionDefinition action)
        {
            Action = action;
        }

        public ResolvedDocumentActionDefinition Action { get; }

        public string Id => Action == null ? null : Action.Id;

        public string Name => Action == null ? null : Action.Name;

        public bool Visible => Action != null && Action.Visible;

        public bool Enabled => Action != null && Action.Enabled;
    }
}


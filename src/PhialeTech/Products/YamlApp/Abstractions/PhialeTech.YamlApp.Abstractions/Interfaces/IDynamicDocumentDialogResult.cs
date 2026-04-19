using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IDynamicDocumentDialogResult
    {
        string DocumentId { get; }

        DynamicDocumentDialogResultKind ResultKind { get; }

        string Json { get; }

        bool IsConfirmed { get; }

        bool IsCancelled { get; }

        bool HasJson { get; }
    }
}


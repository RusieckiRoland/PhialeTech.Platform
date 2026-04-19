using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Abstractions.Results
{
    public sealed class DynamicDocumentDialogResult : IDynamicDocumentDialogResult
    {
        public DynamicDocumentDialogResult(string documentId, DynamicDocumentDialogResultKind resultKind, string json)
        {
            DocumentId = documentId;
            ResultKind = resultKind;
            Json = json;
        }

        public string DocumentId { get; }

        public DynamicDocumentDialogResultKind ResultKind { get; }

        public string Json { get; }

        public bool IsConfirmed => ResultKind == DynamicDocumentDialogResultKind.Confirmed;

        public bool IsCancelled => ResultKind == DynamicDocumentDialogResultKind.Cancelled;

        public bool HasJson => !string.IsNullOrWhiteSpace(Json);

        public static DynamicDocumentDialogResult Confirmed(string documentId, string json)
        {
            return new DynamicDocumentDialogResult(documentId, DynamicDocumentDialogResultKind.Confirmed, json);
        }

        public static DynamicDocumentDialogResult Cancelled(string documentId, string json = null)
        {
            return new DynamicDocumentDialogResult(documentId, DynamicDocumentDialogResultKind.Cancelled, json);
        }
    }
}


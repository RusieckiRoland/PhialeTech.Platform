using System;

namespace PhialeGrid.Core.Editing
{
    public sealed class GridValidationError
    {
        public GridValidationError(
            string columnId,
            string message,
            string errorCode = null,
            string messageKey = null,
            GridValidationSeverity severity = GridValidationSeverity.Error)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ErrorCode = errorCode ?? string.Empty;
            MessageKey = messageKey ?? string.Empty;
            Severity = severity;
        }

        public string ColumnId { get; }

        public string Message { get; }

        public string ErrorCode { get; }

        public string MessageKey { get; }

        public GridValidationSeverity Severity { get; }
    }
}

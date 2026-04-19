using System;

namespace PhialeTech.ReportDesigner.Abstractions
{
    public sealed class ReportDesignerErrorEventArgs : EventArgs
    {
        public ReportDesignerErrorEventArgs(string message, string detail = null)
        {
            Message = message ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Message { get; }

        public string Detail { get; }
    }
}

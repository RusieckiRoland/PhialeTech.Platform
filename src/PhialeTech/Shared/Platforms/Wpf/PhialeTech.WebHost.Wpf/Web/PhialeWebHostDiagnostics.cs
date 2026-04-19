using System;

namespace PhialeTech.WebHost.Wpf
{
    public static class PhialeWebHostDiagnostics
    {
        public static Action<string, string> Sink { get; set; }

        public static void Write(string source, string message)
        {
            Sink?.Invoke(source ?? string.Empty, message ?? string.Empty);
        }
    }
}

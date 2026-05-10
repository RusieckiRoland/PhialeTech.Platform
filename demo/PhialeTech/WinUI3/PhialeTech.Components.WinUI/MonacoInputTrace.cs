using System;
using System.IO;
using System.Text;

namespace PhialeTech.Components.WinUI
{
    internal static class MonacoInputTrace
    {
        private static readonly object Gate = new object();
        private static readonly string LogDirectoryPath;
        public static readonly string CurrentLogFilePath;

        static MonacoInputTrace()
        {
            LogDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PhialeTech",
                "ComponentsWinUI",
                "logs");
            Directory.CreateDirectory(LogDirectoryPath);
            CurrentLogFilePath = Path.Combine(
                LogDirectoryPath,
                "monaco-input-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            Write("trace", "startup", "log created");
        }

        public static void Write(string area, string name, string message)
        {
            var line = string.Format(
                "[{0:HH:mm:ss.fff}] [{1}] [{2}] {3}",
                DateTime.Now,
                area ?? string.Empty,
                name ?? string.Empty,
                message ?? string.Empty);

            lock (Gate)
            {
                File.AppendAllText(CurrentLogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        public static string SafeSnippet(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

            return normalized.Length <= 180
                ? normalized
                : normalized.Substring(0, 180) + "...";
        }
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace PhialeTech.WebHost.Wpf.ComparisonLab
{
    internal static class ComparisonLabTrace
    {
        private static readonly object Gate = new object();
        private static readonly string LogDirectory =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PhialeTech",
                "WebHostComparisonLab",
                "logs");
        private static readonly string LogFilePath =
            Path.Combine(
                LogDirectory,
                "comparison-lab-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".log");

        static ComparisonLabTrace()
        {
            Directory.CreateDirectory(LogDirectory);
            Write("trace", "session", "logging initialized; pid=" + Process.GetCurrentProcess().Id);
        }

        public static string CurrentLogFilePath => LogFilePath;

        public static void Write(string category, string source, string message)
        {
            string line =
                DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                " [" + category + "]" +
                " [" + source + "] " +
                (message ?? string.Empty);

            lock (Gate)
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
            }

            Debug.WriteLine(line);
            Trace.WriteLine(line);
        }

        public static string SafeSnippet(string value, int maxLength = 220)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            string normalized = value
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");

            if (normalized.Length <= maxLength)
                return normalized;

            return normalized.Substring(0, maxLength) + "...";
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace PhialeTech.PhialeGrid.Wpf.Diagnostics
{
    public static class PhialeGridDiagnostics
    {
        private static readonly object SyncRoot = new object();
        private static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "PhialeGrid.StateLifecycle.log");
        private static readonly Dictionary<string, GridDiagnosticSession> GridSessions = new Dictionary<string, GridDiagnosticSession>(StringComparer.Ordinal);
        private static int _nextSessionId;

        public static void Write(string source, string message)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [T{Thread.CurrentThread.ManagedThreadId}] {source}: {message}{Environment.NewLine}";
                lock (SyncRoot)
                {
                    File.AppendAllText(LogFilePath, line);
                }
            }
            catch
            {
            }
        }

        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        public static void BeginGridSession(string gridId, string reason, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(gridId))
            {
                return;
            }

            GridDiagnosticSession session;
            lock (SyncRoot)
            {
                session = new GridDiagnosticSession(
                    Interlocked.Increment(ref _nextSessionId),
                    reason ?? string.Empty,
                    isActive);
                GridSessions[gridId] = session;
            }

            Write("PhialeGridDiagnostics", $"BeginGridSession Grid={gridId}, SessionId={session.SessionId}, IsActive={session.IsActive}, Reason='{session.Reason}'.");
        }

        public static GridDiagnosticCounterSnapshot IncrementGridCounter(string gridId, string counterName)
        {
            if (string.IsNullOrWhiteSpace(gridId) || string.IsNullOrWhiteSpace(counterName))
            {
                return GridDiagnosticCounterSnapshot.Empty;
            }

            lock (SyncRoot)
            {
                if (!GridSessions.TryGetValue(gridId, out var session))
                {
                    return GridDiagnosticCounterSnapshot.Empty;
                }

                session.Counters.TryGetValue(counterName, out var current);
                current++;
                session.Counters[counterName] = current;
                return new GridDiagnosticCounterSnapshot(session.SessionId, session.IsActive, session.Reason, current);
            }
        }

        public static int GetGridCounter(string gridId, string counterName)
        {
            if (string.IsNullOrWhiteSpace(gridId) || string.IsNullOrWhiteSpace(counterName))
            {
                return 0;
            }

            lock (SyncRoot)
            {
                if (!GridSessions.TryGetValue(gridId, out var session))
                {
                    return 0;
                }

                return session.Counters.TryGetValue(counterName, out var current)
                    ? current
                    : 0;
            }
        }

        public static string DescribeGridSession(string gridId)
        {
            if (string.IsNullOrWhiteSpace(gridId))
            {
                return "Session=<none>";
            }

            lock (SyncRoot)
            {
                if (!GridSessions.TryGetValue(gridId, out var session))
                {
                    return "Session=<none>";
                }

                return $"SessionId={session.SessionId}, IsActive={session.IsActive}, Reason='{session.Reason}'";
            }
        }

        private sealed class GridDiagnosticSession
        {
            public GridDiagnosticSession(int sessionId, string reason, bool isActive)
            {
                SessionId = sessionId;
                Reason = reason ?? string.Empty;
                IsActive = isActive;
                Counters = new Dictionary<string, int>(StringComparer.Ordinal);
            }

            public int SessionId { get; }

            public string Reason { get; }

            public bool IsActive { get; }

            public Dictionary<string, int> Counters { get; }
        }
    }

    public readonly struct GridDiagnosticCounterSnapshot
    {
        public static GridDiagnosticCounterSnapshot Empty => default;

        public GridDiagnosticCounterSnapshot(int sessionId, bool isActive, string reason, int count)
        {
            SessionId = sessionId;
            IsActive = isActive;
            Reason = reason ?? string.Empty;
            Count = count;
        }

        public int SessionId { get; }

        public bool IsActive { get; }

        public string Reason { get; }

        public int Count { get; }
    }
}

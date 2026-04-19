using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PhialeTech.Components.Wpf
{
    public sealed class WebHostLoadTrace
    {
        private readonly object _gate = new object();
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly List<string> _entries = new List<string>();

        public WebHostLoadTrace(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "WebHostLoadTrace" : name;
            Mark("trace started");
        }

        public string Name { get; }

        public event EventHandler Updated;

        public void Mark(string step)
        {
            string entry = string.Format(
                "[{0,6:0} ms] {1}",
                _stopwatch.Elapsed.TotalMilliseconds,
                step ?? string.Empty);

            lock (_gate)
            {
                _entries.Add(entry);
            }

            Trace.WriteLine(Name + " " + entry);
            Updated?.Invoke(this, EventArgs.Empty);
        }

        public string GetText()
        {
            lock (_gate)
            {
                return string.Join(Environment.NewLine, _entries.ToArray());
            }
        }

        public IReadOnlyList<string> GetEntries()
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }
}

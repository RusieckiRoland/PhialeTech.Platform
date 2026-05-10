using System;
using System.Diagnostics;
using PhialeTech.PhialeGrid.Wpf.Diagnostics;

namespace PhialeTech.Components.Wpf
{
    internal sealed class ScenarioLoadTrace
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly string _name;
        private readonly string _exampleId;

        public ScenarioLoadTrace(string name, string exampleId)
        {
            _name = string.IsNullOrWhiteSpace(name) ? "ScenarioLoad" : name;
            _exampleId = string.IsNullOrWhiteSpace(exampleId) ? "<empty>" : exampleId;
            Mark("trace started");
        }

        public void Mark(string step)
        {
            var entry = string.Format(
                "[{0,6:0} ms] Example='{1}' {2}",
                _stopwatch.Elapsed.TotalMilliseconds,
                _exampleId,
                step ?? string.Empty);

            Trace.WriteLine(_name + " " + entry);
            MonacoInputTrace.Write("scenario.load", _name, entry);
            PhialeGridDiagnostics.Write(_name, entry);
        }
    }
}


using System;

namespace PhialeGis.Library.Abstractions.Modes
{
    // DSL working modes. Extend as needed (e.g., Measure, Sketch, etc.).
    public enum DslMode
    {
        Normal = 0,
        Points = 1
    }

    /// <summary>
    /// Actions that require a specific DSL mode should implement this interface.
    /// E.g. "Add LineString" requires DslMode.Points.
    /// </summary>
    public interface IDslModeProvider
    {
        DslMode RequiredMode { get; }
    }
}

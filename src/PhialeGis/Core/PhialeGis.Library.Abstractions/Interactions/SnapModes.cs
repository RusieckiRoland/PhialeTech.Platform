using System;

namespace PhialeGis.Library.Abstractions.Interactions
{
    [Flags]
    public enum SnapModes
    {
        None = 0,
        Endpoint = 1 << 0,
        Vertex = 1 << 1,
        Midpoint = 1 << 2,
        NearestOnSegment = 1 << 3,
        Grid = 1 << 4,
        Default = Endpoint | Vertex | Midpoint | NearestOnSegment | Grid
    }
}

using System;

namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Neutralny opis markera na osi przewijania viewportu.
    /// Host platformowy moze go narysowac przy pionowym lub poziomym railu bez znajomosci logiki biznesowej.
    /// </summary>
    public sealed class GridViewportTrackMarker
    {
        public GridViewportTrackMarker(
            string key,
            string targetKey,
            GridViewportTrackMarkerKind kind,
            double startRatio,
            double endRatio,
            string toolTip = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Marker key is required.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(targetKey))
            {
                throw new ArgumentException("Target key is required.", nameof(targetKey));
            }

            Key = key;
            TargetKey = targetKey;
            Kind = kind;
            StartRatio = Math.Max(0d, Math.Min(1d, startRatio));
            EndRatio = Math.Max(StartRatio, Math.Min(1d, endRatio));
            ToolTip = toolTip ?? string.Empty;
        }

        public string Key { get; }

        public string TargetKey { get; }

        public GridViewportTrackMarkerKind Kind { get; }

        public double StartRatio { get; }

        public double EndRatio { get; }

        public string ToolTip { get; }
    }
}

using System;
using PhialeGrid.Core.Regions;

namespace PhialeGrid.Core.Interaction
{
    public sealed class GridRegionCommandInput : GridInputEvent
    {
        public GridRegionCommandInput(
            DateTime timestamp,
            GridRegionCommandKind commandKind,
            GridRegionKind regionKind,
            double? requestedSize = null,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            if (!Enum.IsDefined(typeof(GridRegionCommandKind), commandKind))
            {
                throw new ArgumentOutOfRangeException(nameof(commandKind), commandKind, "Unknown grid region command kind.");
            }

            if (!Enum.IsDefined(typeof(GridRegionKind), regionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(regionKind), regionKind, "Unknown grid region kind.");
            }

            if (commandKind == GridRegionCommandKind.Resize)
            {
                if (!requestedSize.HasValue)
                {
                    throw new InvalidOperationException("Resize region commands require a requested size.");
                }

                if (requestedSize.Value <= 0d || double.IsNaN(requestedSize.Value) || double.IsInfinity(requestedSize.Value))
                {
                    throw new ArgumentOutOfRangeException(nameof(requestedSize), "Resize region commands require a positive finite requested size.");
                }
            }
            else if (requestedSize.HasValue)
            {
                throw new InvalidOperationException("Only resize region commands may carry a requested size.");
            }

            CommandKind = commandKind;
            RegionKind = regionKind;
            RequestedSize = requestedSize;
        }

        public GridRegionCommandKind CommandKind { get; }

        public GridRegionKind RegionKind { get; }

        public double? RequestedSize { get; }
    }

    public enum GridRegionCommandKind
    {
        ToggleCollapse,
        Open,
        Collapse,
        Close,
        Resize,
        Activate,
    }
}

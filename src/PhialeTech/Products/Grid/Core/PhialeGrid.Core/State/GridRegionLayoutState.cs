using System;
using PhialeGrid.Core.Regions;

namespace PhialeGrid.Core.State
{
    public sealed class GridRegionLayoutState
    {
        public GridRegionLayoutState(
            GridRegionKind regionKind,
            GridRegionState state,
            double? size,
            bool isActive,
            GridRegionPlacement? placementOverride = null)
        {
            if (!Enum.IsDefined(typeof(GridRegionKind), regionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(regionKind), regionKind, "Unknown grid region kind.");
            }

            if (!Enum.IsDefined(typeof(GridRegionState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown grid region state.");
            }

            if (size.HasValue && (size.Value <= 0d || double.IsNaN(size.Value) || double.IsInfinity(size.Value)))
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Region size must be a positive finite value when provided.");
            }

            if (isActive && state != GridRegionState.Open)
            {
                throw new InvalidOperationException("Only open regions can be active.");
            }

            if (placementOverride.HasValue && !Enum.IsDefined(typeof(GridRegionPlacement), placementOverride.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(placementOverride), placementOverride, "Unknown grid region placement.");
            }

            RegionKind = regionKind;
            State = state;
            Size = size;
            IsActive = isActive;
            PlacementOverride = placementOverride;
        }

        public GridRegionKind RegionKind { get; }

        public GridRegionState State { get; }

        public bool IsActive { get; }

        public double? Size { get; }

        public GridRegionPlacement? PlacementOverride { get; }
    }
}

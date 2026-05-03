using System;

namespace PhialeGrid.Core.Regions
{
    public sealed class GridRegionViewState
    {
        public GridRegionViewState(
            GridRegionKind regionKind,
            GridRegionHostKind hostKind,
            GridRegionPlacement placement,
            GridRegionContentKind contentKind,
            GridRegionState state,
            bool isAvailable,
            bool isActive,
            bool canCollapse,
            bool canClose,
            bool canResize,
            bool canActivate,
            double? size,
            double? minSize,
            double? maxSize)
        {
            if (!Enum.IsDefined(typeof(GridRegionKind), regionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(regionKind), regionKind, "Unknown grid region kind.");
            }

            if (!Enum.IsDefined(typeof(GridRegionHostKind), hostKind))
            {
                throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, "Unknown grid region host kind.");
            }

            if (!Enum.IsDefined(typeof(GridRegionPlacement), placement))
            {
                throw new ArgumentOutOfRangeException(nameof(placement), placement, "Unknown grid region placement.");
            }

            if (!Enum.IsDefined(typeof(GridRegionContentKind), contentKind))
            {
                throw new ArgumentOutOfRangeException(nameof(contentKind), contentKind, "Unknown grid region content kind.");
            }

            if (!Enum.IsDefined(typeof(GridRegionState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown grid region state.");
            }

            ValidatePlacement(hostKind, placement);
            ValidateSizeBounds(size, minSize, maxSize);
            ValidateStateSemantics(hostKind, state, isAvailable, isActive, canCollapse, canResize, canActivate);

            RegionKind = regionKind;
            HostKind = hostKind;
            Placement = placement;
            ContentKind = contentKind;
            State = state;
            IsAvailable = isAvailable;
            IsActive = isActive;
            CanCollapse = canCollapse;
            CanClose = canClose;
            CanResize = canResize;
            CanActivate = canActivate;
            Size = size;
            MinSize = minSize;
            MaxSize = maxSize;
        }

        public GridRegionKind RegionKind { get; }

        public GridRegionHostKind HostKind { get; }

        public GridRegionPlacement Placement { get; }

        public GridRegionContentKind ContentKind { get; }

        public GridRegionState State { get; }

        public bool IsAvailable { get; }

        public bool IsActive { get; }

        public bool CanCollapse { get; }

        public bool CanClose { get; }

        public bool CanResize { get; }

        public bool CanActivate { get; }

        public double? Size { get; }

        public double? MinSize { get; }

        public double? MaxSize { get; }

        private static void ValidatePlacement(GridRegionHostKind hostKind, GridRegionPlacement placement)
        {
            switch (hostKind)
            {
                case GridRegionHostKind.CoreSurface:
                    if (placement != GridRegionPlacement.Center)
                    {
                        throw new ArgumentException("Core surface view states must use the Center placement.", nameof(placement));
                    }

                    break;
                case GridRegionHostKind.WorkspaceBand:
                    if (placement != GridRegionPlacement.Top && placement != GridRegionPlacement.Bottom)
                    {
                        throw new ArgumentException("Workspace band view states must use the Top or Bottom placement.", nameof(placement));
                    }

                    break;
                case GridRegionHostKind.WorkspacePanel:
                    if (placement != GridRegionPlacement.Left && placement != GridRegionPlacement.Right)
                    {
                        throw new ArgumentException("Workspace panel view states must use the Left or Right placement.", nameof(placement));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, "Unsupported region host kind.");
            }
        }

        private static void ValidateSizeBounds(double? size, double? minSize, double? maxSize)
        {
            if (minSize.HasValue && (minSize.Value <= 0d || double.IsNaN(minSize.Value) || double.IsInfinity(minSize.Value)))
            {
                throw new ArgumentOutOfRangeException(nameof(minSize), "minSize must be a positive finite value when provided.");
            }

            if (maxSize.HasValue && (maxSize.Value <= 0d || double.IsNaN(maxSize.Value) || double.IsInfinity(maxSize.Value)))
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "maxSize must be a positive finite value when provided.");
            }

            if (minSize.HasValue && maxSize.HasValue && maxSize.Value < minSize.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "maxSize cannot be smaller than minSize.");
            }

            if (!size.HasValue)
            {
                return;
            }

            if (size.Value <= 0d || double.IsNaN(size.Value) || double.IsInfinity(size.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(size), "size must be a positive finite value when provided.");
            }

            if ((minSize.HasValue && size.Value < minSize.Value) || (maxSize.HasValue && size.Value > maxSize.Value))
            {
                throw new InvalidOperationException("Region view state size must stay within the declared bounds.");
            }
        }

        private static void ValidateStateSemantics(
            GridRegionHostKind hostKind,
            GridRegionState state,
            bool isAvailable,
            bool isActive,
            bool canCollapse,
            bool canResize,
            bool canActivate)
        {
            if (hostKind == GridRegionHostKind.WorkspaceBand && (canCollapse || canResize))
            {
                throw new InvalidOperationException("Workspace band view states can only close and move vertically; they cannot collapse or resize.");
            }

            if (hostKind == GridRegionHostKind.WorkspacePanel && !canResize)
            {
                throw new InvalidOperationException("Workspace panel view states require resize support.");
            }

            if (state == GridRegionState.Collapsed && !canCollapse)
            {
                throw new InvalidOperationException("A collapsed region view state requires collapse support.");
            }

            if (!isActive)
            {
                return;
            }

            if (!isAvailable)
            {
                throw new InvalidOperationException("Unavailable region view states cannot be active.");
            }

            if (!canActivate)
            {
                throw new InvalidOperationException("Active region view states require activation support.");
            }

            if (state != GridRegionState.Open)
            {
                throw new InvalidOperationException("Only open region view states can be active.");
            }
        }
    }
}

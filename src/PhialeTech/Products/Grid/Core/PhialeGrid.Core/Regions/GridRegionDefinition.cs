using System;

namespace PhialeGrid.Core.Regions
{
    public sealed class GridRegionDefinition
    {
        public GridRegionDefinition(
            GridRegionKind regionKind,
            GridRegionHostKind hostKind,
            GridRegionPlacement placement,
            GridRegionContentKind contentKind,
            GridRegionState defaultState,
            double? defaultSize,
            double? minSize,
            double? maxSize,
            bool canCollapse,
            bool canClose,
            bool canResize,
            bool canActivate)
        {
            if (minSize.HasValue && minSize.Value <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(minSize), "minSize must be positive when provided.");
            }

            if (maxSize.HasValue && maxSize.Value <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "maxSize must be positive when provided.");
            }

            if (minSize.HasValue && maxSize.HasValue && maxSize.Value < minSize.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "maxSize cannot be smaller than minSize.");
            }

            if (defaultSize.HasValue && defaultSize.Value <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultSize), "defaultSize must be positive when provided.");
            }

            ValidatePlacement(hostKind, placement);
            ValidateDefaultState(regionKind, hostKind, defaultState, canCollapse, canClose);

            RegionKind = regionKind;
            HostKind = hostKind;
            Placement = placement;
            ContentKind = contentKind;
            DefaultState = defaultState;
            DefaultSize = defaultSize;
            MinSize = minSize;
            MaxSize = maxSize;
            CanCollapse = canCollapse;
            CanClose = canClose;
            CanResize = canResize;
            CanActivate = canActivate;
        }

        public GridRegionKind RegionKind { get; }

        public GridRegionHostKind HostKind { get; }

        public GridRegionPlacement Placement { get; }

        public GridRegionContentKind ContentKind { get; }

        public GridRegionState DefaultState { get; }

        public double? DefaultSize { get; }

        public double? MinSize { get; }

        public double? MaxSize { get; }

        public bool CanCollapse { get; }

        public bool CanClose { get; }

        public bool CanResize { get; }

        public bool CanActivate { get; }

        public bool IsRequired => RegionKind == GridRegionKind.CoreGridSurface;

        private static void ValidatePlacement(GridRegionHostKind hostKind, GridRegionPlacement placement)
        {
            switch (hostKind)
            {
                case GridRegionHostKind.Surface:
                    if (placement != GridRegionPlacement.Center)
                    {
                        throw new ArgumentException("Surface regions must use the Center placement.", nameof(placement));
                    }

                    break;
                case GridRegionHostKind.Strip:
                    if (placement != GridRegionPlacement.Top && placement != GridRegionPlacement.Bottom)
                    {
                        throw new ArgumentException("Strip regions must use the Top or Bottom placement.", nameof(placement));
                    }

                    break;
                case GridRegionHostKind.Pane:
                    if (placement != GridRegionPlacement.Left && placement != GridRegionPlacement.Right)
                    {
                        throw new ArgumentException("Pane regions must use the Left or Right placement.", nameof(placement));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hostKind), hostKind, "Unsupported region host kind.");
            }
        }

        private static void ValidateDefaultState(
            GridRegionKind regionKind,
            GridRegionHostKind hostKind,
            GridRegionState defaultState,
            bool canCollapse,
            bool canClose)
        {
            if (regionKind == GridRegionKind.CoreGridSurface || hostKind == GridRegionHostKind.Surface)
            {
                if (defaultState != GridRegionState.Open)
                {
                    throw new ArgumentException("Surface regions must default to the Open state.", nameof(defaultState));
                }

                if (canCollapse || canClose)
                {
                    throw new ArgumentException("Surface regions cannot be closable or collapsible.");
                }

                return;
            }

            if (defaultState == GridRegionState.Collapsed && !canCollapse)
            {
                throw new ArgumentException("A collapsed default state requires collapse support.", nameof(defaultState));
            }

            if (defaultState == GridRegionState.Closed && !canClose)
            {
                throw new ArgumentException("A closed default state requires close support.", nameof(defaultState));
            }
        }
    }
}

using PhialeTech.ActiveLayerSelector;

namespace PhialeTech.Components.Shared.Services
{
    public static class DemoActiveLayerSelectorFactory
    {
        public static DemoActiveLayerSelectorState CreateDefaultState()
        {
            return new DemoActiveLayerSelectorState(new[]
            {
                new ActiveLayerSelectorItemState
                {
                    LayerId = "roads",
                    Name = "Roads",
                    TreePath = "Operational / Transport",
                    LayerType = "PostGIS",
                    GeometryType = "LineString",
                    IsActive = true,
                    IsVisible = true,
                    IsSelectable = true,
                    IsEditable = true,
                    IsSnappable = true,
                },
                new ActiveLayerSelectorItemState
                {
                    LayerId = "buildings",
                    Name = "Buildings",
                    TreePath = "Operational / Base",
                    LayerType = "SHP",
                    GeometryType = "Polygon",
                    IsVisible = true,
                    IsSelectable = true,
                    IsEditable = false,
                    IsSnappable = true,
                },
                new ActiveLayerSelectorItemState
                {
                    LayerId = "orthophoto",
                    Name = "Orthophoto",
                    TreePath = "Base Maps / Imagery",
                    LayerType = "WMS",
                    GeometryType = "Raster",
                    IsVisible = true,
                    IsSelectable = false,
                    IsEditable = false,
                    IsSnappable = false,
                    CanToggleSelectable = false,
                    CanToggleEditable = false,
                    CanToggleSnappable = false,
                },
                new ActiveLayerSelectorItemState
                {
                    LayerId = "parcels",
                    Name = "Parcels",
                    TreePath = "Cadastre / Parcels",
                    LayerType = "FGB",
                    GeometryType = "Polygon",
                    IsVisible = true,
                    IsSelectable = true,
                    IsEditable = false,
                    IsSnappable = true,
                },
                new ActiveLayerSelectorItemState
                {
                    LayerId = "addresses",
                    Name = "Addresses",
                    TreePath = "Operational / Base",
                    LayerType = "SHP",
                    GeometryType = "Point",
                    IsVisible = true,
                    IsSelectable = true,
                    IsEditable = false,
                    IsSnappable = false,
                },
                new ActiveLayerSelectorItemState
                {
                    LayerId = "hydrants",
                    Name = "Hydrants",
                    TreePath = "Operational / Water",
                    LayerType = "GeoPackage",
                    GeometryType = "Point",
                    IsVisible = false,
                    IsSelectable = true,
                    IsEditable = true,
                    IsSnappable = true,
                },
                new ActiveLayerSelectorItemState
                {
                    LayerId = "street-lights",
                    Name = "Street Lights",
                    TreePath = "Operational / Lighting",
                    LayerType = "PostGIS",
                    GeometryType = "Point",
                    IsVisible = true,
                    IsSelectable = true,
                    IsEditable = true,
                    IsSnappable = false,
                },
                new ActiveLayerSelectorItemState
                {
                    LayerId = "sewer-network",
                    Name = "Sewer Network",
                    TreePath = "Operational / Utilities",
                    LayerType = "PostGIS",
                    GeometryType = "LineString",
                    IsVisible = true,
                    IsSelectable = true,
                    IsEditable = true,
                    IsSnappable = true,
                },
            });
        }
    }
}


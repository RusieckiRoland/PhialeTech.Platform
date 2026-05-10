using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PhialeTech.ActiveLayerSelector;
using PhialeTech.ActiveLayerSelector.Localization;
using UniversalInput.Contracts;

namespace PhialeGis.Library.Tests.ActiveLayerSelector
{
    [TestFixture]
    public sealed class ActiveLayerSelectorViewModelTests
    {
        [Test]
        public void AttachState_ShouldBuildActiveHeaderAndInitialVisibleItems()
        {
            var state = new FakeActiveLayerSelectorState(CreateItems(7));
            var viewModel = new ActiveLayerSelectorViewModel(ActiveLayerSelectorLocalizationCatalog.LoadDefault(), "en", 5);

            viewModel.AttachState(state);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ActiveItem, Is.Not.Null);
                Assert.That(viewModel.HeaderTitle, Is.EqualTo("Roads"));
                Assert.That(viewModel.VisibleItems.Count, Is.EqualTo(5));
                Assert.That(viewModel.CanShowMore, Is.False);
                Assert.That(viewModel.HeaderCapabilities.Count, Is.EqualTo(4));
            });
        }

        [Test]
        public void HandleCommand_ToggleExpandedAndShowMore_ShouldRevealAdditionalRows()
        {
            var state = new FakeActiveLayerSelectorState(CreateItems(7));
            var viewModel = new ActiveLayerSelectorViewModel(ActiveLayerSelectorLocalizationCatalog.LoadDefault(), "en", 5);
            viewModel.AttachState(state);

            viewModel.HandleCommand(new UniversalCommandEventArgs(ActiveLayerSelectorCommandIds.ToggleExpanded, false, false, false));

            Assert.That(viewModel.CanShowMore, Is.True);
            Assert.That(viewModel.VisibleItems.Count, Is.EqualTo(5));

            viewModel.HandleCommand(new UniversalCommandEventArgs(ActiveLayerSelectorCommandIds.ShowMore, false, false, false));

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.VisibleItems.Count, Is.EqualTo(7));
                Assert.That(viewModel.CanShowMore, Is.False);
            });
        }

        [Test]
        public void HandleCommand_RowInteractions_ShouldProxyToStateByLayerId()
        {
            var state = new FakeActiveLayerSelectorState(CreateItems(4));
            var viewModel = new ActiveLayerSelectorViewModel(ActiveLayerSelectorLocalizationCatalog.LoadDefault(), "en", 5);
            viewModel.AttachState(state);
            viewModel.HandleCommand(new UniversalCommandEventArgs(ActiveLayerSelectorCommandIds.ToggleExpanded, false, false, false));

            var secondLayer = viewModel.VisibleItems[1];
            var setActive = new UniversalCommandEventArgs(ActiveLayerSelectorCommandIds.SetActive, false, false, false);
            setActive.Arguments["layerId"] = secondLayer.LayerId;
            viewModel.HandleCommand(setActive);

            var toggleVisible = new UniversalCommandEventArgs(ActiveLayerSelectorCommandIds.ToggleCapability, false, false, false);
            toggleVisible.Arguments["layerId"] = secondLayer.LayerId;
            toggleVisible.Arguments["capability"] = ActiveLayerSelectorCapabilityKind.Visible.ToString();
            viewModel.HandleCommand(toggleVisible);

            Assert.Multiple(() =>
            {
                Assert.That(state.ActiveLayerId, Is.EqualTo(secondLayer.LayerId));
                Assert.That(state.LastSetActiveLayerId, Is.EqualTo(secondLayer.LayerId));
                Assert.That(state.LastVisibilityLayerId, Is.EqualTo(secondLayer.LayerId));
                Assert.That(state.Items.Single(item => item.LayerId == secondLayer.LayerId).IsVisible, Is.False);
            });
        }

        [Test]
        public void HandleCommand_WithUnknownCommand_ShouldLeaveStateUntouched()
        {
            var state = new FakeActiveLayerSelectorState(CreateItems(4));
            var viewModel = new ActiveLayerSelectorViewModel(ActiveLayerSelectorLocalizationCatalog.LoadDefault(), "en", 5);
            viewModel.AttachState(state);

            viewModel.HandleCommand(new UniversalCommandEventArgs("unknown.command", false, false, false));

            Assert.That(state.ActiveLayerId, Is.EqualTo("layer-0"));
        }

        [Test]
        public void LocalizationCatalog_ShouldLoadEnglishAndPolishTexts()
        {
            var catalog = ActiveLayerSelectorLocalizationCatalog.LoadDefault();

            Assert.Multiple(() =>
            {
                Assert.That(catalog.GetText("en", ActiveLayerSelectorTextKeys.ShowMore), Is.EqualTo("Show more"));
                Assert.That(catalog.GetText("pl", ActiveLayerSelectorTextKeys.ShowMore), Is.EqualTo("Pokaż więcej"));
            });
        }

        private static IReadOnlyList<ActiveLayerSelectorItemState> CreateItems(int count)
        {
            var geometryTypes = new[] { "LineString", "Polygon", "Raster", "Point" };
            var names = new[] { "Roads", "Buildings", "Orthophoto", "Addresses", "Parcels", "Street lights", "Hydrants" };
            var paths = new[]
            {
                "Operational / Transport",
                "Operational / Base",
                "Base Maps / Orthophoto",
                "Operational / Base",
                "Cadastre / Parcels",
                "Operational / Lighting",
                "Operational / Water"
            };
            var sources = new[] { "PostGIS", "SHP", "WMS", "SHP", "FGB", "PostGIS", "GeoPackage" };

            return Enumerable.Range(0, count)
                .Select(index => new ActiveLayerSelectorItemState
                {
                    LayerId = "layer-" + index,
                    Name = names[index],
                    TreePath = paths[index],
                    LayerType = sources[index],
                    GeometryType = geometryTypes[index % geometryTypes.Length],
                    IsActive = index == 0,
                    IsVisible = true,
                    IsSelectable = index != 2,
                    IsEditable = index == 0 || index == 4,
                    IsSnappable = index != 2,
                    CanBecomeActive = index != 2,
                    CanToggleVisible = true,
                    CanToggleSelectable = true,
                    CanToggleEditable = index != 2,
                    CanToggleSnappable = true,
                })
                .ToArray();
        }

        private sealed class FakeActiveLayerSelectorState : IActiveLayerSelectorState
        {
            private List<ActiveLayerSelectorItemState> _items;

            public FakeActiveLayerSelectorState(IReadOnlyList<ActiveLayerSelectorItemState> items)
            {
                _items = items.Select(item => Clone(item)).ToList();
                ActiveLayerId = _items.FirstOrDefault(item => item.IsActive)?.LayerId ?? string.Empty;
            }

            public IReadOnlyList<ActiveLayerSelectorItemState> Items => _items;

            public string ActiveLayerId { get; private set; }

            public string LastSetActiveLayerId { get; private set; }

            public string LastVisibilityLayerId { get; private set; }

            public event EventHandler StateChanged;

            public void SetActiveLayer(string layerId)
            {
                LastSetActiveLayerId = layerId;
                ActiveLayerId = layerId;
                _items = _items.Select(item => Clone(item, isActive: string.Equals(item.LayerId, layerId, StringComparison.Ordinal))).ToList();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }

            public void SetLayerVisible(string layerId, bool value)
            {
                LastVisibilityLayerId = layerId;
                _items = _items.Select(item => string.Equals(item.LayerId, layerId, StringComparison.Ordinal) ? Clone(item, isVisible: value) : item).ToList();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }

            public void SetLayerSelectable(string layerId, bool value)
            {
                _items = _items.Select(item => string.Equals(item.LayerId, layerId, StringComparison.Ordinal) ? Clone(item, isSelectable: value) : item).ToList();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }

            public void SetLayerEditable(string layerId, bool value)
            {
                _items = _items.Select(item => string.Equals(item.LayerId, layerId, StringComparison.Ordinal) ? Clone(item, isEditable: value) : item).ToList();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }

            public void SetLayerSnappable(string layerId, bool value)
            {
                _items = _items.Select(item => string.Equals(item.LayerId, layerId, StringComparison.Ordinal) ? Clone(item, isSnappable: value) : item).ToList();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }

            private static ActiveLayerSelectorItemState Clone(ActiveLayerSelectorItemState item, bool? isActive = null, bool? isVisible = null, bool? isSelectable = null, bool? isEditable = null, bool? isSnappable = null)
            {
                return new ActiveLayerSelectorItemState
                {
                    LayerId = item.LayerId,
                    Name = item.Name,
                    TreePath = item.TreePath,
                    LayerType = item.LayerType,
                    GeometryType = item.GeometryType,
                    IsActive = isActive ?? item.IsActive,
                    IsVisible = isVisible ?? item.IsVisible,
                    IsSelectable = isSelectable ?? item.IsSelectable,
                    IsEditable = isEditable ?? item.IsEditable,
                    IsSnappable = isSnappable ?? item.IsSnappable,
                    CanBecomeActive = item.CanBecomeActive,
                    CanToggleVisible = item.CanToggleVisible,
                    CanToggleSelectable = item.CanToggleSelectable,
                    CanToggleEditable = item.CanToggleEditable,
                    CanToggleSnappable = item.CanToggleSnappable,
                };
            }
        }
    }
}



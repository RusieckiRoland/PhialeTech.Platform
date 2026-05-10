using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeTech.ActiveLayerSelector;
using PhialeTech.ActiveLayerSelector.Wpf.Controls;

namespace PhialeTech.ActiveLayerSelector.Wpf.Tests.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class ActiveLayerSelectorBehaviorTests
    {
        [Test]
        public void Control_ShouldShowInitialRowsAndRevealMoreOnShowMoreClick()
        {
            var control = CreateControl();
            var state = new FakeActiveLayerSelectorState(CreateItems(7));
            control.State = state;
            control.InitialVisibleItemCount = 5;
            var window = CreateWindow(control);

            try
            {
                window.Show();
                FlushDispatcher(control.Dispatcher);

                var expandButton = (Button)control.FindName("ExpandCollapseButton");
                var popup = (Popup)control.FindName("ExpandedPopup");
                Assert.That(expandButton, Is.Not.Null);
                Assert.That(popup, Is.Not.Null);
                Assert.That(popup.IsOpen, Is.False);
                Assert.That(control.ActualHeight, Is.LessThanOrEqualTo(72));
                expandButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(control.Dispatcher);

                var rowsControl = (ItemsControl)control.FindName("VisibleRowsItemsControl");
                var showMoreButton = (Button)control.FindName("ShowMoreButton");

                Assert.Multiple(() =>
                {
                    Assert.That(popup.IsOpen, Is.True);
                    Assert.That(rowsControl.Items.Count, Is.EqualTo(5));
                    Assert.That(showMoreButton.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(control.ActualHeight, Is.LessThanOrEqualTo(72));
                });

                showMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(control.Dispatcher);

                Assert.Multiple(() =>
                {
                    Assert.That(rowsControl.Items.Count, Is.EqualTo(7));
                    Assert.That(showMoreButton.Visibility, Is.EqualTo(Visibility.Collapsed));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Control_SetActiveButtonClick_ShouldUpdateHeaderAndState()
        {
            var control = CreateControl();
            var state = new FakeActiveLayerSelectorState(CreateItems(4));
            control.State = state;
            var window = CreateWindow(control);

            try
            {
                window.Show();
                FlushDispatcher(control.Dispatcher);

                var expandButton = (Button)control.FindName("ExpandCollapseButton");
                expandButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(control.Dispatcher);

                var popup = (Popup)control.FindName("ExpandedPopup");
                var setActiveButtons = FindVisualChildren<Button>(control)
                    .Where(button => button.DataContext is ActiveLayerSelectorLayerViewModel layer && layer.CanSetActive)
                    .ToArray();
                if (setActiveButtons.Length == 0 && popup?.Child != null)
                {
                    setActiveButtons = FindVisualChildren<Button>(popup.Child)
                        .Where(button => button.DataContext is ActiveLayerSelectorLayerViewModel layer && layer.CanSetActive)
                        .ToArray();
                }

                Assert.That(setActiveButtons.Length, Is.GreaterThanOrEqualTo(1));
                setActiveButtons[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(control.Dispatcher);

                var headerTitle = FindVisualChildren<TextBlock>(control).First(text => string.Equals(text.Text, "Buildings", StringComparison.Ordinal));

                Assert.Multiple(() =>
                {
                    Assert.That(state.ActiveLayerId, Is.EqualTo("layer-1"));
                    Assert.That(headerTitle, Is.Not.Null);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Control_WhenCollapsed_HeaderCapabilityButtonsShouldRemainClickable()
        {
            var control = CreateControl();
            var state = new FakeActiveLayerSelectorState(CreateItems(4, activeIndex: 2, activeVisible: false));
            control.State = state;
            var window = CreateWindow(control);

            try
            {
                window.Show();
                FlushDispatcher(control.Dispatcher);

                var headerItemsControl = (ItemsControl)control.FindName("HeaderCapabilitiesItemsControl");
                var headerButtons = FindVisualChildren<ToggleButton>(headerItemsControl).ToArray();
                var visibleButton = headerButtons.FirstOrDefault(button =>
                    button.DataContext is ActiveLayerSelectorCapabilityViewModel capability &&
                    capability.Kind == ActiveLayerSelectorCapabilityKind.Visible);

                Assert.Multiple(() =>
                {
                    Assert.That(headerItemsControl, Is.Not.Null);
                    Assert.That(headerButtons.Length, Is.EqualTo(4));
                    Assert.That(visibleButton, Is.Not.Null);
                    Assert.That(visibleButton.IsEnabled, Is.True);
                    Assert.That(((Popup)control.FindName("ExpandedPopup")).IsOpen, Is.False);
                });

                visibleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(control.Dispatcher);

                Assert.That(state.Items.First(item => item.LayerId == state.ActiveLayerId).IsVisible, Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SvgIcon_WhenVisibleCapabilityIsOff_ShouldStillResolveImageSource()
        {
            var icon = new ActiveLayerSvgIcon
            {
                Capability = ActiveLayerSelectorCapabilityKind.Visible,
                IsOn = false,
            };

            Assert.That(icon.Source, Is.Not.Null);
        }

        [Test]
        public void Control_WhenNightModeIsEnabled_ShouldUseDarkThemeColors()
        {
            var control = CreateControl();
            control.IsNightMode = true;
            control.State = new FakeActiveLayerSelectorState(CreateItems(3));
            var window = CreateWindow(control);

            try
            {
                window.Show();
                FlushDispatcher(control.Dispatcher);

                var headerChrome = (Border)control.FindName("HeaderChrome");
                var headerTitle = (TextBlock)control.FindName("HeaderTitleText");

                Assert.Multiple(() =>
                {
                    Assert.That(headerChrome, Is.Not.Null);
                    Assert.That(headerTitle, Is.Not.Null);
                    Assert.That(((SolidColorBrush)headerChrome.Background).Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#1E232D")));
                    Assert.That(((SolidColorBrush)headerTitle.Foreground).Color, Is.EqualTo((Color)ColorConverter.ConvertFromString("#F4F6FA")));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static PhialeTech.ActiveLayerSelector.Wpf.Controls.ActiveLayerSelector CreateControl()
        {
            return new PhialeTech.ActiveLayerSelector.Wpf.Controls.ActiveLayerSelector
            {
                Width = 980,
                LanguageDirectory = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "PhialeTech.ActiveLayerSelector", "Languages")
            };
        }

        private static Window CreateWindow(UIElement content)
        {
            return new Window
            {
                Width = 1100,
                Height = 700,
                Content = content,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false
            };
        }

        private static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static IReadOnlyList<ActiveLayerSelectorItemState> CreateItems(int count, int activeIndex = 0, bool activeVisible = true)
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
                    IsActive = index == activeIndex,
                    IsVisible = index == activeIndex ? activeVisible : true,
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

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T typed)
                {
                    yield return typed;
                }

                foreach (var nested in FindVisualChildren<T>(child))
                {
                    yield return nested;
                }
            }
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

            public event EventHandler StateChanged;

            public void SetActiveLayer(string layerId)
            {
                ActiveLayerId = layerId;
                _items = _items.Select(item => Clone(item, isActive: string.Equals(item.LayerId, layerId, StringComparison.Ordinal))).ToList();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }

            public void SetLayerVisible(string layerId, bool value)
            {
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





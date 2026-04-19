using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeGrid.Core;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Layout;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using PhialeTech.PhialeGrid.Wpf.Surface;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridSurfaceHostBehaviorTests
    {
        [Test]
        public void Initialize_WhenCoordinatorRequestsFocus_FocusesHost()
        {
            var coordinator = new GridSurfaceCoordinator();
            coordinator.Initialize(
                new[] { new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 } },
                new[] { new GridRowDefinition { RowKey = "row-1", Height = 20 } });

            var host = new GridSurfaceHost
            {
                Width = 400,
                Height = 200,
            };

            var window = new Window
            {
                Width = 400,
                Height = 200,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                RaiseFocusRequested(coordinator, new GridFocusRequestEventArgs
                {
                    TargetKind = GridFocusTargetKind.Grid,
                });
                FlushDispatcher(host.Dispatcher);

                Assert.That(host.IsKeyboardFocusWithin || host.IsFocused, Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Initialize_WhenHostIsFirstShown_UsesScrollViewerGeometryImmediatelyWithoutFollowUpInteraction()
        {
            var coordinator = CreateFrozenCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 420,
                Height = 240,
            };

            var window = new Window
            {
                Width = 420,
                Height = 240,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                var scrollViewer = GetScrollViewer(host);
                var snapshot = host.CurrentSnapshot;

                Assert.Multiple(() =>
                {
                    Assert.That(scrollViewer, Is.Not.Null);
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.ViewportState.ViewportWidth, Is.EqualTo(scrollViewer.ActualWidth).Within(1d));
                    Assert.That(snapshot.ViewportState.ViewportHeight, Is.EqualTo(scrollViewer.ActualHeight).Within(1d));
                    Assert.That(snapshot.ViewportState.ViewportWidth, Is.GreaterThan(150d));
                    Assert.That(snapshot.ViewportState.ViewportHeight, Is.GreaterThan(100d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Initialize_FirstRenderGeometry_DoesNotRequireLaterUserScrollToBecomeCorrect()
        {
            var coordinator = CreateFrozenCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 420,
                Height = 240,
            };

            var window = new Window
            {
                Width = 420,
                Height = 240,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                var before = host.CurrentSnapshot;
                host.HandleWheelForTesting(new UniversalInput.Contracts.UniversalPointerWheelChangedEventArgs(
                    120,
                    new UniversalInput.Contracts.UniversalPoint { X = 120, Y = 80 }));
                FlushDispatcher(host.Dispatcher);
                var after = host.CurrentSnapshot;

                Assert.Multiple(() =>
                {
                    Assert.That(before, Is.Not.Null);
                    Assert.That(after, Is.Not.Null);
                    Assert.That(before.ViewportState.ViewportWidth, Is.EqualTo(after.ViewportState.ViewportWidth).Within(0.1d));
                    Assert.That(before.ViewportState.ViewportHeight, Is.EqualTo(after.ViewportState.ViewportHeight).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void PopupEditorInputScope_WhenMarked_TreatsPopupSourceAsCellEditorInput()
        {
            var popupRoot = new Border();
            var itemBorder = new Border();
            var text = new TextBlock { Text = "Municipality" };
            itemBorder.Child = text;
            popupRoot.Child = itemBorder;
            GridCellEditorInputScope.SetIsEditorOwnedPopupElement(popupRoot, true);

            try
            {
                var method = typeof(GridSurfaceHost).GetMethod("IsCellEditorInputSource", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(method, Is.Not.Null);

                var result = (bool)method.Invoke(null, new object[] { text });
                Assert.That(result, Is.True);
            }
            finally
            {
                GridCellEditorInputScope.SetIsEditorOwnedPopupElement(popupRoot, false);
            }
        }

        [Test]
        public void Initialize_WithFrozenRegionsAndScroll_KeepsFrozenPresenterAnchoredWhilePanelCountersNativeScroll()
        {
            var coordinator = CreateFrozenCoordinator();

            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                coordinator.SetScrollPosition(50, 10);
                FlushDispatcher(host.Dispatcher);

                var transform = host.SurfacePanelForTesting.RenderTransform as TranslateTransform;
                var frozenCell = FindPresenter(host.SurfacePanelForTesting, "row-1", "col-1");
                var scrollableCell = FindPresenter(host.SurfacePanelForTesting, "row-2", "col-2");
                var topChromeHeight = host.CurrentSnapshot.ViewportState.ColumnHeaderHeight + host.CurrentSnapshot.ViewportState.FilterRowHeight;

                Assert.Multiple(() =>
                {
                    Assert.That(transform, Is.Not.Null);
                    Assert.That(transform.X, Is.EqualTo(50d));
                    Assert.That(transform.Y, Is.EqualTo(10d));
                    Assert.That(Canvas.GetLeft(frozenCell), Is.EqualTo(40d));
                    Assert.That(Canvas.GetTop(frozenCell), Is.EqualTo(topChromeHeight).Within(0.1d));
                    Assert.That(Canvas.GetLeft(scrollableCell), Is.EqualTo(140d));
                    Assert.That(Canvas.GetTop(scrollableCell), Is.EqualTo(topChromeHeight + 20d).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void HandleWheel_WithFrozenRegions_SyncsScrollViewerAndKeepsFrozenPresenterAnchored()
        {
            var coordinator = CreateFrozenCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                host.HandleWheelForTesting(new UniversalInput.Contracts.UniversalPointerWheelChangedEventArgs(
                    999,
                    new UniversalInput.Contracts.UniversalPoint { X = 160, Y = 80 }));
                FlushDispatcher(host.Dispatcher);

                var frozenCell = FindPresenter(host.SurfacePanelForTesting, "row-1", "col-1");
                var scrollableCell = FindPresenter(host.SurfacePanelForTesting, "row-4", "col-2");
                var expectedVerticalOffset = host.CurrentSnapshot.ViewportState.MaxVerticalOffset;
                var topChromeHeight = host.CurrentSnapshot.ViewportState.ColumnHeaderHeight + host.CurrentSnapshot.ViewportState.FilterRowHeight;
                Assert.Multiple(() =>
                {
                    Assert.That(host.CurrentSnapshot.ViewportState.VerticalOffset, Is.EqualTo(expectedVerticalOffset));
                    Assert.That(GetScrollViewer(host).VerticalOffset, Is.EqualTo(expectedVerticalOffset).Within(0.1d));
                    Assert.That(Canvas.GetLeft(frozenCell), Is.EqualTo(40d));
                    Assert.That(Canvas.GetTop(frozenCell), Is.EqualTo(topChromeHeight).Within(0.1d));
                    Assert.That(scrollableCell, Is.Not.Null);
                    Assert.That(Canvas.GetTop(scrollableCell), Is.GreaterThan(Canvas.GetTop(frozenCell)));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void LostMouseCapture_DuringPendingHeaderInteraction_CancelsPendingReorder()
        {
            var coordinator = CreateFrozenCoordinator();
            GridColumnReorderRequestedEventArgs reorder = null;
            coordinator.ColumnReorderRequested += (_, args) => reorder = args;

            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                GridSurfaceTestHost.PointerDownViaRoutedUi(host, 90, 15);

                host.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
                {
                    RoutedEvent = UIElement.LostMouseCaptureEvent,
                    Source = host,
                });

                GridSurfaceTestHost.PointerMoveViaRoutedUi(host, 190, 15);
                GridSurfaceTestHost.PointerUpViaRoutedUi(host, 190, 15);

                Assert.That(reorder, Is.Null);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void LostFocus_DuringPendingHeaderInteraction_CancelsPendingReorder()
        {
            var coordinator = CreateFrozenCoordinator();
            GridColumnReorderRequestedEventArgs reorder = null;
            coordinator.ColumnReorderRequested += (_, args) => reorder = args;

            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                GridSurfaceTestHost.PointerDownViaRoutedUi(host, 90, 15);

                host.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, host));

                GridSurfaceTestHost.PointerMoveViaRoutedUi(host, 190, 15);
                GridSurfaceTestHost.PointerUpViaRoutedUi(host, 190, 15);

                Assert.That(reorder, Is.Null);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void PreviewMouseWheel_WhenDeltaIsPositive_ScrollsTowardsTop()
        {
            var coordinator = CreateFrozenCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                coordinator.SetScrollPosition(0, 40);
                FlushDispatcher(host.Dispatcher);
                var before = host.CurrentSnapshot.ViewportState.VerticalOffset;

                host.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, 120)
                {
                    RoutedEvent = UIElement.PreviewMouseWheelEvent,
                    Source = host,
                });
                FlushDispatcher(host.Dispatcher);

                var after = host.CurrentSnapshot.ViewportState.VerticalOffset;
                Assert.That(after, Is.LessThan(before));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ScrollBarThumb_ShouldBypassSurfaceInputHandling()
        {
            var method = typeof(GridSurfaceHost).GetMethod("ShouldBypassSurfaceInput", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var thumb = new Thumb();
            var bypass = (bool)method.Invoke(null, new object[] { thumb });

            Assert.That(bypass, Is.True);
        }

        [Test]
        public void RowIndicatorChild_ShouldBypassSurfaceInputHandling()
        {
            var method = typeof(GridSurfaceHost).GetMethod("ShouldBypassSurfaceInput", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var indicatorSlot = new Border();
            AutomationProperties.SetAutomationId(indicatorSlot, "surface.row-indicator.row-1");
            var indicatorGlyph = new Border();
            indicatorSlot.Child = indicatorGlyph;

            var bypass = (bool)method.Invoke(null, new object[] { indicatorGlyph });

            Assert.That(bypass, Is.True);
        }

        [Test]
        public void ComboBoxEditor_ShouldBeRecognizedAsCellEditorInputSource()
        {
            var method = typeof(GridSurfaceHost).GetMethod("IsCellEditorInputSource", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var comboBox = new ComboBox();
            var isEditor = (bool)method.Invoke(null, new object[] { comboBox });

            Assert.That(isEditor, Is.True);
        }

        [Test]
        public void DatePickerEditor_ShouldBeRecognizedAsCellEditorInputSource()
        {
            var method = typeof(GridSurfaceHost).GetMethod("IsCellEditorInputSource", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var datePicker = new DatePicker();
            var isEditor = (bool)method.Invoke(null, new object[] { datePicker });

            Assert.That(isEditor, Is.True);
        }

        [Test]
        public void RequestBringIntoView_FromActiveCellEditor_IsHandledByHostToPreserveViewport()
        {
            var coordinator = CreateEditableCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                coordinator.SetCurrentCell("row-1", "col-2");
                coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.F2));
                FlushDispatcher(host.Dispatcher);

                var editor = GridSurfaceTestHost.FindDescendant<TextBox>(host);
                Assert.That(editor, Is.Not.Null, "Expected active editor inside the surface host.");
                if (editor == null)
                {
                    return;
                }

                var args = (RequestBringIntoViewEventArgs)Activator.CreateInstance(
                    typeof(RequestBringIntoViewEventArgs),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { editor, new Rect(0, 0, 12, 12) },
                    culture: null);
                Assert.That(args, Is.Not.Null);
                if (args == null)
                {
                    return;
                }

                args.RoutedEvent = FrameworkElement.RequestBringIntoViewEvent;
                args.Source = editor;

                host.RaiseEvent(args);

                Assert.That(args.Handled, Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void PreviewMouseUp_OnCellEditorSource_ReleasesExistingPointerCaptureSession()
        {
            var coordinator = CreateFrozenCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                var editor = new ComboBox
                {
                    Width = 80,
                    Height = 24,
                };
                host.SurfacePanelForTesting.Children.Add(editor);
                Canvas.SetLeft(editor, 20d);
                Canvas.SetTop(editor, 20d);
                FlushDispatcher(host.Dispatcher);

                InvokeBeginMousePointerCapture(host, new Point(24d, 24d));
                Assert.That(GetActivePointerCaptureSession(host), Is.Not.Null);

                editor.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseUpEvent,
                    Source = editor,
                });
                FlushDispatcher(host.Dispatcher);

                Assert.That(GetActivePointerCaptureSession(host), Is.Null);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ScrollChanged_WhenEditorHasFocus_RestoresProgrammaticHorizontalOffset()
        {
            var coordinator = CreateEditableCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 180,
                Height = 120,
            };

            var window = new Window
            {
                Width = 180,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                coordinator.SetScrollPosition(80d, 0d);
                coordinator.SetCurrentCell("row-1", "col-2");
                coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.F2));
                FlushDispatcher(host.Dispatcher);

                var editor = GridSurfaceTestHost.FindDescendant<TextBox>(host);
                var scrollViewer = GetScrollViewer(host);

                Assert.Multiple(() =>
                {
                    Assert.That(editor, Is.Not.Null, "Expected active editor inside the surface host.");
                    Assert.That(scrollViewer, Is.Not.Null);
                });

                if (editor == null || scrollViewer == null)
                {
                    return;
                }

                editor.Focus();
                Keyboard.Focus(editor);
                FlushDispatcher(host.Dispatcher);

                var protectedOffset = host.CurrentSnapshot.ViewportState.HorizontalOffset;
                scrollViewer.ScrollToHorizontalOffset(0d);
                FlushDispatcher(host.Dispatcher);

                Assert.Multiple(() =>
                {
                    Assert.That(scrollViewer.HorizontalOffset, Is.EqualTo(protectedOffset).Within(0.1d));
                    Assert.That(host.CurrentSnapshot.ViewportState.HorizontalOffset, Is.EqualTo(protectedOffset).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ScrollChanged_WhenHorizontalScrollbarIsDraggedDuringEdit_AllowsUserHorizontalScroll()
        {
            var coordinator = CreateEditableCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 180,
                Height = 120,
            };

            var window = new Window
            {
                Width = 180,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                coordinator.SetScrollPosition(80d, 0d);
                coordinator.SetCurrentCell("row-1", "col-2");
                coordinator.ProcessInput(new GridKeyInput(DateTime.UtcNow, GridKey.F2));
                FlushDispatcher(host.Dispatcher);

                var editor = GridSurfaceTestHost.FindDescendant<TextBox>(host);
                var scrollViewer = GetScrollViewer(host);
                var horizontalScrollBar = FindHorizontalScrollBar(scrollViewer);

                Assert.Multiple(() =>
                {
                    Assert.That(editor, Is.Not.Null, "Expected active editor inside the surface host.");
                    Assert.That(scrollViewer, Is.Not.Null);
                    Assert.That(horizontalScrollBar, Is.Not.Null, "Expected horizontal scrollbar inside the surface host.");
                });

                if (editor == null || scrollViewer == null || horizontalScrollBar == null)
                {
                    return;
                }

                editor.Focus();
                Keyboard.Focus(editor);
                FlushDispatcher(host.Dispatcher);

                horizontalScrollBar.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseDownEvent,
                    Source = horizontalScrollBar,
                });
                scrollViewer.ScrollToHorizontalOffset(0d);
                FlushDispatcher(host.Dispatcher);
                horizontalScrollBar.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseUpEvent,
                    Source = horizontalScrollBar,
                });
                FlushDispatcher(host.Dispatcher);

                Assert.Multiple(() =>
                {
                    Assert.That(scrollViewer.HorizontalOffset, Is.EqualTo(0d).Within(0.1d));
                    Assert.That(host.CurrentSnapshot.ViewportState.HorizontalOffset, Is.EqualTo(0d).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CoordinatorScrollIntoView_WhenSnapshotOffsetChanges_HostSyncsScrollViewerWithoutHostSpecificWorkaround()
        {
            var coordinator = CreateFrozenCoordinator();
            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                var result = coordinator.TryScrollCellIntoView("row-4", "col-4");
                FlushDispatcher(host.Dispatcher);

                var scrollViewer = GetScrollViewer(host);

                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.True);
                    Assert.That(host.CurrentSnapshot.ViewportState.HorizontalOffset, Is.EqualTo(120d).Within(0.1d));
                    Assert.That(host.CurrentSnapshot.ViewportState.VerticalOffset, Is.GreaterThan(0d));
                    Assert.That(scrollViewer, Is.Not.Null);
                    Assert.That(scrollViewer.HorizontalOffset, Is.EqualTo(120d).Within(0.1d));
                    Assert.That(scrollViewer.VerticalOffset, Is.EqualTo(host.CurrentSnapshot.ViewportState.VerticalOffset).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SnapshotWithViewportTrackMarkers_RendersMarkerRailInHost()
        {
            var coordinator = CreateFrozenCoordinator();
            coordinator.SetEditedRows(new[] { "row-3" });
            coordinator.SetInvalidRows(new[] { "row-4" });
            coordinator.SetRowIndicatorToolTips(new System.Collections.Generic.Dictionary<string, string>
            {
                ["row-3"] = "Edited row",
                ["row-4"] = "Invalid row",
            });

            var host = new GridSurfaceHost
            {
                Width = 280,
                Height = 120,
            };

            var window = new Window
            {
                Width = 280,
                Height = 120,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                var markerHost = host.FindName("VerticalTrackMarkerHost") as FrameworkElement;
                var markerCanvas = host.FindName("VerticalTrackMarkerCanvas") as Canvas;

                Assert.Multiple(() =>
                {
                    Assert.That(markerHost, Is.Not.Null);
                    Assert.That(markerCanvas, Is.Not.Null);
                    Assert.That(markerHost?.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(markerHost?.Margin.Top, Is.EqualTo(
                        host.CurrentSnapshot.ViewportState.ColumnHeaderHeight +
                        host.CurrentSnapshot.ViewportState.FilterRowHeight).Within(0.1d));
                    Assert.That(markerCanvas?.Children.Count, Is.EqualTo(2));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static GridSurfaceCoordinator CreateFrozenCoordinator()
        {
            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = new TestCellAccessor()
                    .Add("row-1", "col-1", "A")
                    .Add("row-1", "col-2", "B")
                    .Add("row-1", "col-3", "C")
                    .Add("row-1", "col-4", "D")
                    .Add("row-2", "col-1", "E")
                    .Add("row-2", "col-2", "F")
                    .Add("row-2", "col-3", "G")
                    .Add("row-2", "col-4", "H")
                    .Add("row-3", "col-1", "I")
                    .Add("row-3", "col-2", "J")
                    .Add("row-3", "col-3", "K")
                    .Add("row-3", "col-4", "L")
                    .Add("row-4", "col-1", "M")
                    .Add("row-4", "col-2", "N")
                    .Add("row-4", "col-3", "O")
                    .Add("row-4", "col-4", "P"),
                FrozenColumnCount = 1,
                FrozenRowCount = 1,
            };
            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100, IsFrozen = true },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 80 },
                    new GridColumnDefinition { ColumnKey = "col-3", Header = "Col 3", Width = 60 },
                    new GridColumnDefinition { ColumnKey = "col-4", Header = "Col 4", Width = 120 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 20 },
                    new GridRowDefinition { RowKey = "row-2", Height = 30 },
                    new GridRowDefinition { RowKey = "row-3", Height = 40 },
                    new GridRowDefinition { RowKey = "row-4", Height = 50 },
                });
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateEditableCoordinator()
        {
            var accessor = new TestEditableCellAccessor();
            accessor.Add("row-1", "col-1", "Alpha");
            accessor.Add("row-1", "col-2", "Oliwa Segment 3");

            var coordinator = new GridSurfaceCoordinator
            {
                CellValueProvider = accessor,
                EditCellAccessor = accessor,
            };

            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 180 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 24 },
                });

            return coordinator;
        }

        private static void RaiseFocusRequested(GridSurfaceCoordinator coordinator, GridFocusRequestEventArgs args)
        {
            var field = typeof(GridSurfaceCoordinator).GetField("FocusRequested", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected FocusRequested backing field to exist.");

            var handler = field.GetValue(coordinator) as EventHandler<GridFocusRequestEventArgs>;
            Assert.That(handler, Is.Not.Null, "Expected host to subscribe to coordinator.FocusRequested.");

            handler(coordinator, args);
        }

        private static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static object GetActivePointerCaptureSession(GridSurfaceHost host)
        {
            var field = typeof(GridSurfaceHost).GetField("_activePointerCapture", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(host);
        }

        private static void InvokeBeginMousePointerCapture(GridSurfaceHost host, Point position)
        {
            var method = typeof(GridSurfaceHost).GetMethod("BeginMousePointerCapture", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(host, new object[] { position });
        }

        private static GridCellPresenter FindPresenter(GridSurfacePanel panel, string rowKey, string columnKey)
        {
            foreach (var layer in panel.Children)
            {
                if (layer is not Canvas canvas)
                {
                    continue;
                }

                foreach (var child in canvas.Children)
                {
                    if (child is GridCellPresenter presenter &&
                        presenter.CellData?.RowKey == rowKey &&
                        presenter.CellData?.ColumnKey == columnKey)
                    {
                        return presenter;
                    }
                }
            }

            return null;
        }

        private static ScrollViewer GetScrollViewer(GridSurfaceHost host)
        {
            return host.FindName("ScrollViewer") as ScrollViewer;
        }

        private static ScrollBar FindHorizontalScrollBar(DependencyObject root)
        {
            return FindDescendants(root)
                .OfType<ScrollBar>()
                .FirstOrDefault(scrollBar => scrollBar.Orientation == Orientation.Horizontal);
        }

        private static System.Collections.Generic.IEnumerable<DependencyObject> FindDescendants(DependencyObject root)
        {
            if (root == null)
            {
                yield break;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                yield return child;

                foreach (var descendant in FindDescendants(child))
                {
                    yield return descendant;
                }
            }
        }

        private class TestCellAccessor : PhialeGrid.Core.Rendering.IGridCellValueProvider
        {
            private readonly System.Collections.Generic.Dictionary<string, object> _values =
                new System.Collections.Generic.Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            public TestCellAccessor Add(string rowKey, string columnKey, object value)
            {
                _values[rowKey + "_" + columnKey] = value;
                return this;
            }

            public bool TryGetValue(string rowKey, string columnKey, out object value)
            {
                return _values.TryGetValue(rowKey + "_" + columnKey, out value);
            }
        }

        private sealed class TestEditableCellAccessor : TestCellAccessor, PhialeGrid.Core.Editing.IGridEditCellAccessor
        {
            public void SetValue(string rowKey, string columnKey, object value)
            {
                Add(rowKey, columnKey, value);
            }
        }
    }
}

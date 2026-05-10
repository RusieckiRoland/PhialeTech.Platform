using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface;
using UniversalInput.Contracts;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Surface
{
    internal static class GridSurfaceTestHost
    {
        private static readonly object LiveInputSync = new object();

        public static Window CreateHostWindow(
            FrameworkElement content,
            double width = 1280,
            double height = 720)
        {
            return new Window
            {
                Width = width,
                Height = height,
                Content = content,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };
        }

        public static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        public static void FlushDispatcher(DispatcherObject dispatcherObject)
        {
            Assert.That(dispatcherObject, Is.Not.Null);
            FlushDispatcher(dispatcherObject.Dispatcher);
        }

        public static GridSurfaceHost FindSurfaceHost(WpfGrid grid)
        {
            var surfaceHost = (GridSurfaceHost)grid.FindName("SurfaceHost");
            Assert.That(surfaceHost, Is.Not.Null, "SurfaceHost should be the only runtime renderer.");
            return surfaceHost;
        }

        public static void ClickBoundsCenter(GridSurfaceHost surfaceHost, GridBounds bounds, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            var x = bounds.X + (bounds.Width / 2d);
            var y = bounds.Y + (bounds.Height / 2d);
            ClickPoint(surfaceHost, x, y, modifiers);
        }

        public static void ClickPoint(GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            GridSurfaceLowLevelInputHost.ClickPoint(surfaceHost, x, y, modifiers);
        }

        public static void DoubleClickPoint(GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            GridSurfaceLowLevelInputHost.DoubleClickPoint(surfaceHost, x, y, modifiers);
        }

        public static void DragPoint(GridSurfaceHost surfaceHost, double pressX, double pressY, double moveX, double moveY, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            GridSurfaceLowLevelInputHost.DragPoint(surfaceHost, pressX, pressY, moveX, moveY, modifiers);
        }

        public static void SendText(GridSurfaceHost surfaceHost, string text)
        {
            GridSurfaceLowLevelInputHost.SendText(surfaceHost, text);
        }

        public static void SendKey(GridSurfaceHost surfaceHost, string key, bool isDown = true)
        {
            GridSurfaceLowLevelInputHost.SendKey(surfaceHost, key, isDown);
        }

        public static void ClickPointViaRoutedUi(GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            ClickPointViaRoutedUi((FrameworkElement)surfaceHost, x, y, modifiers);
        }

        public static void ClickPointViaRoutedUi(FrameworkElement root, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            var surfaceHost = ResolveSurfaceHost(root);
            ExecuteRoutedMouseInput(root, modifiers, (probe, position) =>
            {
                RaisePreviewMouseDown(surfaceHost, probe, position);
                RaisePreviewMouseUp(surfaceHost, probe, position);
            }, x, y);
        }

        public static void DoubleClickPointViaRoutedUi(GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            DoubleClickPointViaRoutedUi((FrameworkElement)surfaceHost, x, y, modifiers);
        }

        public static void DoubleClickPointViaRoutedUi(FrameworkElement root, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            var surfaceHost = ResolveSurfaceHost(root);
            ExecuteRoutedMouseInput(root, modifiers, (probe, position) =>
            {
                RaisePreviewMouseDown(surfaceHost, probe, position, clickCount: 2);
                RaisePreviewMouseUp(surfaceHost, probe, position);
            }, x, y);
        }

        public static void RightClickPointViaRoutedUi(FrameworkElement root, double x, double y)
        {
            Assert.That(root, Is.Not.Null);

            lock (LiveInputSync)
            {
                ActivateForInput(root);
                root.Dispatcher.Invoke(() =>
                {
                    var source = FindInputSourceAtPoint(root, x, y);
                    RaiseRightClick(source);
                });

                FlushDispatcher(root);
            }
        }

        public static FrameworkElement FindVisibleElementAtPoint(FrameworkElement root, double x, double y)
        {
            Assert.That(root, Is.Not.Null);
            FrameworkElement element = null;
            root.Dispatcher.Invoke(() =>
            {
                element = FindInputSourceAtPoint(root, x, y);
            });
            return element;
        }

        public static void RightClickElementViaRoutedUi(FrameworkElement element)
        {
            Assert.That(element, Is.Not.Null);

            lock (LiveInputSync)
            {
                ActivateForInput(element);
                element.Dispatcher.Invoke(() =>
                {
                    RaiseRightClick(element);
                });

                FlushDispatcher(element);
            }
        }

        public static void PointerDownViaRoutedUi(GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None, int clickCount = 1)
        {
            PointerDownViaRoutedUi((FrameworkElement)surfaceHost, x, y, modifiers, clickCount);
        }

        public static void PointerDownViaRoutedUi(FrameworkElement root, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None, int clickCount = 1)
        {
            var surfaceHost = ResolveSurfaceHost(root);
            ExecuteRoutedMouseInput(root, modifiers, (probe, position) =>
            {
                RaisePreviewMouseDown(surfaceHost, probe, position, clickCount);
            }, x, y);
        }

        public static void PointerMoveViaRoutedUi(GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            PointerMoveViaRoutedUi((FrameworkElement)surfaceHost, x, y, modifiers);
        }

        public static void PointerMoveViaRoutedUi(FrameworkElement root, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            var surfaceHost = ResolveSurfaceHost(root);
            lock (LiveInputSync)
            {
                ActivateForInput(root);
                try
                {
                    PressModifiers(modifiers);
                    RaisePreviewMouseMove(surfaceHost, root, new Point(x, y));
                }
                finally
                {
                    ReleaseModifiers(modifiers);
                    FlushDispatcher(root);
                }
            }
        }

        public static void PointerUpViaRoutedUi(GridSurfaceHost surfaceHost, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            PointerUpViaRoutedUi((FrameworkElement)surfaceHost, x, y, modifiers);
        }

        public static void PointerUpViaRoutedUi(FrameworkElement root, double x, double y, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            var surfaceHost = ResolveSurfaceHost(root);
            lock (LiveInputSync)
            {
                ActivateForInput(root);
                try
                {
                    PressModifiers(modifiers);
                    RaisePreviewMouseUp(surfaceHost, root, new Point(x, y));
                }
                finally
                {
                    ReleaseModifiers(modifiers);
                    FlushDispatcher(root);
                }
            }
        }

        public static void DragPointViaRoutedUi(GridSurfaceHost surfaceHost, double pressX, double pressY, double moveX, double moveY, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            DragPointViaRoutedUi((FrameworkElement)surfaceHost, pressX, pressY, moveX, moveY, modifiers);
        }

        public static void DragPointViaRoutedUi(FrameworkElement root, double pressX, double pressY, double moveX, double moveY, UniversalModifierKeys modifiers = UniversalModifierKeys.None)
        {
            PointerDownViaRoutedUi(root, pressX, pressY, modifiers);
            PointerMoveViaRoutedUi(root, moveX, moveY, modifiers);
            PointerUpViaRoutedUi(root, moveX, moveY, modifiers);
        }

        public static FrameworkElement FindVisibleElementAtPoint(GridSurfaceHost surfaceHost, double x, double y)
        {
            return FindInputSourceAtPoint(surfaceHost, x, y);
        }

        public static T FindVisibleAncestorAtPoint<T>(GridSurfaceHost surfaceHost, double x, double y)
            where T : FrameworkElement
        {
            return FindVisibleAncestorAtPoint<T>((FrameworkElement)surfaceHost, x, y);
        }

        public static T FindVisibleAncestorAtPoint<T>(FrameworkElement root, double x, double y)
            where T : FrameworkElement
        {
            var current = FindInputSourceAtPoint(root, x, y) as DependencyObject;
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        public static void SendTextViaRoutedUi(GridSurfaceHost surfaceHost, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            lock (LiveInputSync)
            {
                ActivateForInput(surfaceHost);

                surfaceHost.Dispatcher.Invoke(() =>
                {
                    var target = GetKeyboardTarget(surfaceHost);
                    if (target is TextBox textBox)
                    {
                        textBox.SelectedText = text;
                        return;
                    }

                    foreach (var character in text)
                    {
                        var composition = new TextComposition(InputManager.Current, target, character.ToString());
                        var args = new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice, composition)
                        {
                            RoutedEvent = UIElement.PreviewTextInputEvent,
                            Source = target,
                        };

                        target.RaiseEvent(args);
                    }
                });

                FlushDispatcher(surfaceHost);
            }
        }

        public static void SendKeyViaRoutedUi(GridSurfaceHost surfaceHost, string key, bool isDown = true)
        {
            lock (LiveInputSync)
            {
                ActivateForInput(surfaceHost);
                surfaceHost.Dispatcher.Invoke(() =>
                {
                    var target = GetKeyboardTarget(surfaceHost);
                    if (target is TextBox textBox)
                    {
                        switch (ParseKey(key))
                        {
                            case Key.Delete:
                                DeleteSelectedOrNextCharacter(textBox);
                                return;
                            case Key.Back:
                                DeleteSelectedOrPreviousCharacter(textBox);
                                return;
                            case Key.Enter:
                            case Key.Escape:
                                GridSurfaceLowLevelInputHost.SendKey(surfaceHost, key, isDown);
                                return;
                        }
                    }

                    var presentationSource = PresentationSource.FromVisual(target);
                    Assert.That(presentationSource, Is.Not.Null, "Expected live presentation source for keyboard input.");

                    var routedEvent = isDown ? Keyboard.PreviewKeyDownEvent : Keyboard.PreviewKeyUpEvent;
                    var keyArgs = new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        presentationSource,
                        Environment.TickCount,
                        ParseKey(key))
                    {
                        RoutedEvent = routedEvent,
                        Source = target,
                    };

                    target.RaiseEvent(keyArgs);
                });

                FlushDispatcher(surfaceHost);
            }
        }

        public static T FindDescendant<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T typed)
                {
                    return typed;
                }

                var nested = FindDescendant<T>(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
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

        public static T FindElementByAutomationId<T>(DependencyObject root, string automationId)
            where T : FrameworkElement
        {
            return EnumerateDescendants(root)
                .OfType<T>()
                .FirstOrDefault(candidate => string.Equals(
                    AutomationProperties.GetAutomationId(candidate),
                    automationId,
                    System.StringComparison.OrdinalIgnoreCase));
        }

        public static string ReadVisibleText(DependencyObject root)
        {
            if (root == null)
            {
                return string.Empty;
            }

            if (root is TextBlock textBlock)
            {
                return textBlock.Text ?? string.Empty;
            }

            if (root is ContentControl contentControl && contentControl.Content is string contentText)
            {
                return contentText;
            }

            return string.Join(
                " ",
                EnumerateDescendants(root)
                    .OfType<TextBlock>()
                    .Select(text => text.Text?.Trim())
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Distinct(System.StringComparer.Ordinal));
        }

        public static void SaveElementScreenshot(FrameworkElement element, string path)
        {
            Assert.That(element, Is.Not.Null);
            Assert.That(path, Is.Not.Null.And.Not.Empty);

            element.Dispatcher.Invoke(() =>
            {
                element.UpdateLayout();
                FlushDispatcher(element);

                var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
                var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
                var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(element);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                using (var stream = File.Create(path))
                {
                    encoder.Save(stream);
                }
            });
        }

        private static IEnumerable<DependencyObject> EnumerateDescendants(DependencyObject root)
        {
            return EnumerateDescendants(root, new HashSet<DependencyObject>());
        }

        private static IEnumerable<DependencyObject> EnumerateDescendants(DependencyObject root, ISet<DependencyObject> visited)
        {
            if (root == null)
            {
                yield break;
            }

            if (!visited.Add(root))
            {
                yield break;
            }

            yield return root;

            if (root is ContentControl contentControl && contentControl.Content is DependencyObject contentRoot)
            {
                foreach (var descendant in EnumerateDescendants(contentRoot, visited))
                {
                    yield return descendant;
                }
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                foreach (var descendant in EnumerateDescendants(child, visited))
                {
                    yield return descendant;
                }
            }
        }

        private static void ExecuteRoutedMouseInput(
            FrameworkElement root,
            UniversalModifierKeys modifiers,
            Action<FrameworkElement, Point> action,
            double x,
            double y)
        {
            lock (LiveInputSync)
            {
                ActivateForInput(root);
                try
                {
                    PressModifiers(modifiers);
                    FrameworkElement source = null;
                    root.Dispatcher.Invoke(() => source = FindInputSourceAtPoint(root, x, y));
                    action(source, new Point(x, y));
                }
                finally
                {
                    ReleaseModifiers(modifiers);
                    FlushDispatcher(root);
                }
            }
        }

        private static void ActivateForInput(GridSurfaceHost surfaceHost)
        {
            surfaceHost.Dispatcher.Invoke(() =>
            {
                EnsurePlatformTestAdapters(surfaceHost);
                var window = Window.GetWindow(surfaceHost);
                Assert.That(window, Is.Not.Null, "Expected GridSurfaceHost to be attached to a live window.");
                window.Activate();
                window.Focus();
                surfaceHost.Focus();
                Keyboard.Focus(surfaceHost);
            });

            FlushDispatcher(surfaceHost);
        }

        private static void ActivateForInput(FrameworkElement root)
        {
            root.Dispatcher.Invoke(() =>
            {
                if (root is GridSurfaceHost surfaceHost)
                {
                    EnsurePlatformTestAdapters(surfaceHost);
                }

                var window = Window.GetWindow(root);
                Assert.That(window, Is.Not.Null, "Expected test element to be attached to a live window.");
                window.Activate();
                window.Focus();
                root.Focus();
                Keyboard.Focus(root);
            });

            FlushDispatcher(root);
        }

        private static GridSurfaceHost ResolveSurfaceHost(FrameworkElement root)
        {
            if (root is GridSurfaceHost surfaceHost)
            {
                return surfaceHost;
            }

            var grid = FindAncestor<WpfGrid>(root);
            if (grid != null)
            {
                return FindSurfaceHost(grid);
            }

            throw new AssertionException("Expected routed UI input root to belong to a live PhialeGrid surface.");
        }

        private static T FindAncestor<T>(DependencyObject candidate)
            where T : DependencyObject
        {
            while (candidate != null)
            {
                if (candidate is T match)
                {
                    return match;
                }

                candidate = VisualTreeHelper.GetParent(candidate);
            }

            return null;
        }

        private static UIElement GetKeyboardTarget(GridSurfaceHost surfaceHost)
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            if (focused is UIElement focusedElement && IsDescendantOrSelf(surfaceHost, focused))
            {
                return focusedElement;
            }

            var editor = FindDescendant<TextBox>(surfaceHost);
            if (editor != null)
            {
                editor.Focus();
                Keyboard.Focus(editor);
                return editor;
            }

            surfaceHost.Focus();
            Keyboard.Focus(surfaceHost);
            return surfaceHost;
        }

        private static bool IsDescendantOrSelf(DependencyObject root, DependencyObject candidate)
        {
            while (candidate != null)
            {
                if (ReferenceEquals(root, candidate))
                {
                    return true;
                }

                candidate = VisualTreeHelper.GetParent(candidate);
            }

            return false;
        }

        private static Key ParseKey(string key)
        {
            Assert.That(Enum.TryParse<Key>(key, true, out var parsedKey), Is.True, "Unknown WPF key: " + key);
            return parsedKey;
        }

        private static void DeleteSelectedOrNextCharacter(TextBox textBox)
        {
            Assert.That(textBox, Is.Not.Null);

            if (textBox.SelectionLength > 0)
            {
                textBox.SelectedText = string.Empty;
                return;
            }

            if (textBox.SelectionStart >= textBox.Text.Length)
            {
                return;
            }

            textBox.Text = textBox.Text.Remove(textBox.SelectionStart, 1);
            textBox.SelectionStart = Math.Min(textBox.SelectionStart, textBox.Text.Length);
            textBox.SelectionLength = 0;
        }

        private static void DeleteSelectedOrPreviousCharacter(TextBox textBox)
        {
            Assert.That(textBox, Is.Not.Null);

            if (textBox.SelectionLength > 0)
            {
                textBox.SelectedText = string.Empty;
                return;
            }

            if (textBox.SelectionStart <= 0)
            {
                return;
            }

            var removeIndex = textBox.SelectionStart - 1;
            textBox.Text = textBox.Text.Remove(removeIndex, 1);
            textBox.SelectionStart = removeIndex;
            textBox.SelectionLength = 0;
        }

        private static FrameworkElement FindInputSourceAtPoint(GridSurfaceHost surfaceHost, double x, double y)
        {
            var result = VisualTreeHelper.HitTest(surfaceHost.SurfacePanelForTesting, new Point(x, y));
            Assert.That(result, Is.Not.Null, $"Expected a live surface element at ({x:0.##}, {y:0.##}).");

            var current = result.VisualHit as DependencyObject;
            while (current != null)
            {
                if (current is FrameworkElement element &&
                    element.IsVisible &&
                    element.IsLoaded &&
                    element.ActualWidth > 0 &&
                    element.ActualHeight > 0)
                {
                    return element;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            Assert.Fail($"Expected a visible FrameworkElement at ({x:0.##}, {y:0.##}).");
            return null;
        }

        private static FrameworkElement FindInputSourceAtPoint(FrameworkElement root, double x, double y)
        {
            var result = VisualTreeHelper.HitTest(root, new Point(x, y));
            Assert.That(result, Is.Not.Null, $"Expected a live element at ({x:0.##}, {y:0.##}).");

            var current = result.VisualHit as DependencyObject;
            while (current != null)
            {
                if (current is FrameworkElement element &&
                    element.IsVisible &&
                    element.IsLoaded &&
                    element.ActualWidth > 0 &&
                    element.ActualHeight > 0)
                {
                    return element;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            Assert.Fail($"Expected a visible FrameworkElement at ({x:0.##}, {y:0.##}).");
            return null;
        }

        private static void RaiseRightClick(UIElement source)
        {
            var previewArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right)
            {
                RoutedEvent = UIElement.PreviewMouseRightButtonDownEvent,
                Source = source,
            };

            source.RaiseEvent(previewArgs);

            var bubbleArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right)
            {
                RoutedEvent = UIElement.MouseRightButtonDownEvent,
                Source = source,
            };

            source.RaiseEvent(bubbleArgs);
        }

        private static void PressModifiers(UniversalModifierKeys modifiers)
        {
            if ((modifiers & UniversalModifierKeys.Shift) == UniversalModifierKeys.Shift)
            {
                SendKeyboard(VirtualKeyShort.Shift, isKeyDown: true);
            }

            if ((modifiers & UniversalModifierKeys.Control) == UniversalModifierKeys.Control)
            {
                SendKeyboard(VirtualKeyShort.Control, isKeyDown: true);
            }

            if ((modifiers & UniversalModifierKeys.Alt) == UniversalModifierKeys.Alt)
            {
                SendKeyboard(VirtualKeyShort.Menu, isKeyDown: true);
            }
        }

        private static void ReleaseModifiers(UniversalModifierKeys modifiers)
        {
            if ((modifiers & UniversalModifierKeys.Alt) == UniversalModifierKeys.Alt)
            {
                SendKeyboard(VirtualKeyShort.Menu, isKeyDown: false);
            }

            if ((modifiers & UniversalModifierKeys.Control) == UniversalModifierKeys.Control)
            {
                SendKeyboard(VirtualKeyShort.Control, isKeyDown: false);
            }

            if ((modifiers & UniversalModifierKeys.Shift) == UniversalModifierKeys.Shift)
            {
                SendKeyboard(VirtualKeyShort.Shift, isKeyDown: false);
            }
        }

        private static void SendKeyboard(VirtualKeyShort key, bool isKeyDown)
        {
            keybd_event((byte)key, 0, isKeyDown ? 0u : 0x0002u, UIntPtr.Zero);
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private enum VirtualKeyShort : ushort
        {
            Shift = 0x10,
            Control = 0x11,
            Menu = 0x12,
        }

        private static void RaisePreviewMouseDown(GridSurfaceHost host, UIElement source, Point position, int clickCount = 1)
        {
            RaiseMouseEvent(host, source, position, () =>
            {
                var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseDownEvent,
                    Source = source,
                };
                SetClickCount(args, clickCount);
                source.RaiseEvent(args);
            });
        }

        private static void RaisePreviewMouseMove(GridSurfaceHost host, UIElement source, Point position)
        {
            RaiseMouseEvent(host, source, position, () =>
            {
                var args = new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
                {
                    RoutedEvent = UIElement.PreviewMouseMoveEvent,
                    Source = source,
                };
                source.RaiseEvent(args);
            });
        }

        private static void RaisePreviewMouseUp(GridSurfaceHost host, UIElement source, Point position)
        {
            RaiseMouseEvent(host, source, position, () =>
            {
                var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                {
                    RoutedEvent = UIElement.PreviewMouseUpEvent,
                    Source = source,
                };
                source.RaiseEvent(args);
            });
        }

        private static void RaiseMouseEvent(GridSurfaceHost host, UIElement source, Point position, Action raise)
        {
            host.Dispatcher.Invoke(() =>
            {
                var resolver = GetOrCreatePointerResolver(host);
                resolver.SetPosition(position);
                try
                {
                    raise();
                }
                finally
                {
                    resolver.ClearPosition();
                }
            });
        }

        private static void SetClickCount(MouseButtonEventArgs args, int clickCount)
        {
            var field = typeof(MouseButtonEventArgs).GetField("_count", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(args, clickCount);
        }

        private static RoutedPointerPositionResolver GetOrCreatePointerResolver(GridSurfaceHost host)
        {
            if (host.PointerPositionResolver is RoutedPointerPositionResolver resolver)
            {
                return resolver;
            }

            resolver = new RoutedPointerPositionResolver();
            host.PointerPositionResolver = resolver;
            return resolver;
        }

        private static void EnsurePlatformTestAdapters(GridSurfaceHost host)
        {
            GetOrCreatePointerResolver(host);
            if (host.PointerCaptureController is RoutedPointerCaptureController)
            {
                return;
            }

            host.PointerCaptureController = new RoutedPointerCaptureController();
        }

        private sealed class RoutedPointerPositionResolver : IGridSurfacePointerPositionResolver
        {
            private Point? _position;

            public Point ResolvePosition(MouseEventArgs args, IInputElement relativeTo, DependencyObject originalSource)
            {
                return _position ?? args.GetPosition(relativeTo);
            }

            public void SetPosition(Point position)
            {
                _position = position;
            }

            public void ClearPosition()
            {
                _position = null;
            }
        }

        private sealed class RoutedPointerCaptureController : IGridSurfacePointerCaptureController
        {
            public bool TryCaptureMouse(UIElement target)
            {
                return true;
            }

            public void ReleaseMouse(UIElement target)
            {
            }

            public bool TryCaptureTouch(UIElement target, TouchDevice touchDevice)
            {
                return true;
            }

            public void ReleaseTouch(TouchDevice touchDevice)
            {
            }
        }
    }
}


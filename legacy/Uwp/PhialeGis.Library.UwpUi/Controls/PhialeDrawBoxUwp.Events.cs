//PhialeDrawBoxUwp.Events.cs

using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using UniversalInput.Contracts;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.UwpUi.Conversions;
using SkiaSharp.Views.UWP;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace PhialeGis.Library.UwpUi.Controls
{
    public partial class PhialeDrawBoxUwp 
    {
        internal sealed partial class RedrawableAdapter : IRenderingComposition, IUserInteractive
        {
            public event EventHandler<object> SurfaceShifted;
            public event EventHandler<UniversalPointerRoutedEventArgs> PointerPressedUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs> PointerMovedUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs> PointerEnteredUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs> PointerReleasedUniversal;
            public event EventHandler<UniversalManipulationStartingRoutedEventArgs> ManipulationStartingUniversal;
            public event EventHandler<UniversalManipulationStartedRoutedEventArgs> ManipulationStartedUniversal;
            public event EventHandler<UniversalManipulationDeltaRoutedEventArgs> ManipulationDeltaUniversal;
            public event EventHandler<UniversalManipulationCompletedRoutedEventArgs> ManipulationCompletedUniversal;

            private SKXamlCanvas _skiaCanvas => _owner._skiaCanvas;


            public void RaiseSurfaceShifted(double dx, double dy) => SurfaceShifted?.Invoke(this, new SurfaceMovement(dx, dy));

            private bool TryHandlePointer(UniversalPointerRoutedEventArgs uni, PointerPhase phase)
            {
                var manager = _owner.InteractionManagerInternal;
                if (manager == null || uni?.Pointer == null) return false;
                manager.UpdateCursorPosition(this, uni.Pointer.Position.X, uni.Pointer.Position.Y);

                var input = new ActionPointerInput
                {
                    ScreenPosition = uni.Pointer.Position,
                    PointerDeviceType = uni.Pointer.PointerDeviceType,
                    PointerId = uni.Pointer.PointerId,
                    Button = ResolvePointerButton(uni, phase),
                    TargetDraw = this,
                    HasModelPosition = false,
                    TimestampUtc = DateTime.UtcNow
                };

                switch (phase)
                {
                    case PointerPhase.Down:
                        return manager.TryHandleInteractivePointerDown(input);
                    case PointerPhase.Move:
                        return manager.TryHandleInteractivePointerMove(input);
                    case PointerPhase.Up:
                        return manager.TryHandleInteractivePointerUp(input);
                    default:
                        return false;
                }
            }

            private static PointerButton ResolvePointerButton(UniversalPointerRoutedEventArgs uni, PointerPhase phase)
            {
                if (phase != PointerPhase.Down) return PointerButton.None;
                var p = uni?.Pointer?.Properties;
                if (p == null) return PointerButton.None;

                if (p.IsRightButtonPressed) return PointerButton.Secondary;
                if (p.IsMiddleButtonPressed) return PointerButton.Middle;
                if (p.IsLeftButtonPressed) return PointerButton.Primary;
                return PointerButton.None;
            }

            private void TryShowActionContextMenu()
            {
                var manager = _owner.InteractionManagerInternal;
                if (manager == null || _skiaCanvas == null) return;

                if (!manager.TryConsumePendingContextMenu(this, out var payload) || payload == null)
                    return;

                var items = payload.Items ?? Array.Empty<ActionContextMenuItem>();
                if (items.Length == 0)
                    return;

                var flyout = new MenuFlyout();
                for (var i = 0; i < items.Length; i++)
                {
                    var entry = items[i];
                    if (entry == null) continue;

                    if (entry.IsSeparator)
                    {
                        flyout.Items.Add(new MenuFlyoutSeparator());
                        continue;
                    }

                    var commandId = entry.CommandId ?? string.Empty;
                    var item = new MenuFlyoutItem
                    {
                        Text = entry.Label ?? string.Empty,
                        IsEnabled = entry.Enabled
                    };

                    item.Click += (_, __) =>
                    {
                        try { manager.TryHandleInteractiveMenuCommand(this, commandId); }
                        catch { /* best-effort */ }
                    };

                    flyout.Items.Add(item);
                }

                if (flyout.Items.Count == 0)
                    return;

                flyout.ShowAt(_skiaCanvas, new FlyoutShowOptions
                {
                    Position = new Point(payload.ScreenPosition.X, payload.ScreenPosition.Y)
                });
            }

            public void OnPointerPressed(PointerRoutedEventArgs e)
            {
                var handle = PointerPressedUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                var isSecondary = ResolvePointerButton(uniEvent, PointerPhase.Down) == PointerButton.Secondary;
                if (TryHandlePointer(uniEvent, PointerPhase.Down))
                {
                    e.Handled = true;
                    if (isSecondary) TryShowActionContextMenu();
                    return;
                }
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                ProcessCustomResponse(uniEvent.Metadata);
            }

            public  void OnPointerMoved(PointerRoutedEventArgs e)
            {
               
                var handle = PointerMovedUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Move)) { e.Handled = true; return; }
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                ProcessCustomResponse(uniEvent.Metadata);
            }

            public  void OnPointerEntered(PointerRoutedEventArgs e)
            {
                
                var handle = PointerEnteredUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                ProcessCustomResponse(uniEvent.Metadata);
            }

            public void OnPointerReleased(PointerRoutedEventArgs e)
            {
                
                var handle = PointerReleasedUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Up)) { e.Handled = true; return; }
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                ProcessCustomResponse(uniEvent.Metadata);
            }

            public void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
            {
                
                var handle = ManipulationStartedUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                handle?.Invoke(_owner, uniEvent);
                ProcessCustomResponse(uniEvent.Metadata);

            }

            public void OnManipulationStarting(ManipulationStartingRoutedEventArgs e)
            {
                
                var handle = ManipulationStartingUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                handle?.Invoke(_owner, uniEvent);              

            }

            public void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
            {
                
                var handle = ManipulationDeltaUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                handle?.Invoke(_owner, uniEvent);
                _owner.ManipulationMode = ManipulationModes.Scale;
                ProcessCustomResponse(uniEvent.Metadata);

            }

            public void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
            {
                
                var handle = ManipulationCompletedUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                handle?.Invoke(_owner, uniEvent);
                ProcessCustomResponse(uniEvent.Metadata);
            }


            //=======================================================//
            public void ProcessCustomResponse(UniversalMetadata respose)
            {
                if (respose == null) return;
                if (respose.ResetManipulationMode)
                {
                    _owner.ManipulationMode = _owner.DefaultManipulationMode;
                }
            }
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);

            
            var pos = e.GetCurrentPoint(_skiaCanvas);
            var device = e.Pointer?.PointerDeviceType.ToString() ?? "Unknown";
            Debug.WriteLine($"[PhialeDrawBoxAvalonia] Generating PointerPressed → UniversalPointerRoutedEventArgs " +
                            $"(device={device}, x={pos.Position.X:0.##}, y={pos.Position.Y:0.##})");

            Redrawable.OnPointerPressed(e);
        }

        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            var pos = e.GetCurrentPoint(_skiaCanvas);
            var device = e.Pointer?.PointerDeviceType.ToString() ?? "Unknown";
            Debug.WriteLine($"[PhialeDrawBoxAvalonia] Generating PointerMoved → UniversalPointerRoutedEventArgs " +
                            $"(device={device}, x={pos.Position.X:0.##}, y={pos.Position.Y:0.##})");

            base.OnPointerMoved(e);
            Redrawable.OnPointerMoved(e);
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);

            // Debug: mirror of OnPointerMoved — log device and coordinates
            var pos = e.GetCurrentPoint(_skiaCanvas);
            var device = e.Pointer?.PointerDeviceType.ToString() ?? "Unknown";
            Debug.WriteLine($"[PhialeDrawBoxAvalonia] Generating PointerEntered → UniversalPointerRoutedEventArgs " +
                            $"(device={device}, x={pos.Position.X:0.##}, y={pos.Position.Y:0.##})");

            Redrawable.OnPointerEntered(e);
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            Redrawable.OnPointerReleased(e);
        }

        protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
        {
            base.OnManipulationStarted(e);
            Redrawable.OnManipulationStarted(e);

        }

        protected override void OnManipulationStarting(ManipulationStartingRoutedEventArgs e)
        {
            base.OnManipulationStarting(e);
            Redrawable.OnManipulationStarting(e);

        }

        protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
        {
            base.OnManipulationDelta(e);
            Redrawable.OnManipulationDelta(e);

        }

        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            base.OnManipulationCompleted(e);
            Redrawable.OnManipulationCompleted(e);
        }  

       
        private enum PointerPhase { Down, Move, Up }
    }
}


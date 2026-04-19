//PhialeDrawBox.Events.cs
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using UniversalInput.Contracts;
using PhialeGis.Library.WpfUi.Converters;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PhialeGis.Library.WpfUi.Controls
{
    public partial class PhialeDrawBox 
    {
        internal sealed partial class RedrawableAdapter : IRenderingComposition, IUserInteractive
        {
            private  UIElement _skiaCanvas => _owner._skiaCanvas;

            public event EventHandler<object> SurfaceShifted;
            public event EventHandler<UniversalPointerRoutedEventArgs> PointerPressedUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs> PointerMovedUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs> PointerEnteredUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs> PointerReleasedUniversal;
            public event EventHandler<UniversalManipulationStartingRoutedEventArgs> ManipulationStartingUniversal;
            public event EventHandler<UniversalManipulationStartedRoutedEventArgs> ManipulationStartedUniversal;
            public event EventHandler<UniversalManipulationDeltaRoutedEventArgs> ManipulationDeltaUniversal;
            public event EventHandler<UniversalManipulationCompletedRoutedEventArgs> ManipulationCompletedUniversal;

            public void RaiseSurfaceShifted(double dx, double dy) => SurfaceShifted?.Invoke(this, new SurfaceMovement(dx, dy));

            private bool TryHandlePointer(UniversalPointerRoutedEventArgs uni, PointerPhase phase)
            {
                var manager = _owner.GisInteractionManager;
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
                var manager = _owner.GisInteractionManager;
                if (manager == null) return;

                if (!manager.TryConsumePendingContextMenu(this, out var payload) || payload == null)
                    return;

                var items = payload.Items ?? Array.Empty<ActionContextMenuItem>();
                if (items.Length == 0)
                    return;

                var contextMenu = new ContextMenu
                {
                    PlacementTarget = _skiaCanvas,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint,
                    HorizontalOffset = payload.ScreenPosition.X,
                    VerticalOffset = payload.ScreenPosition.Y
                };

                for (var i = 0; i < items.Length; i++)
                {
                    var entry = items[i];
                    if (entry == null) continue;

                    if (entry.IsSeparator)
                    {
                        contextMenu.Items.Add(new Separator());
                        continue;
                    }

                    var menuItem = new MenuItem
                    {
                        Header = entry.Label ?? string.Empty,
                        IsEnabled = entry.Enabled
                    };

                    var commandId = entry.CommandId ?? string.Empty;
                    menuItem.Click += (_, __) =>
                    {
                        try { manager.TryHandleInteractiveMenuCommand(this, commandId); }
                        catch { /* best-effort */ }
                    };

                    contextMenu.Items.Add(menuItem);
                }

                if (contextMenu.Items.Count > 0)
                    contextMenu.IsOpen = true;
            }

            //===========================
            #region Counterpart UWP PoiterPressed
            public void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (!_owner.ForwardMouseEventsOnPenOrTouchInteraction && e.StylusDevice != null)
                { return; }
                ;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Down)) { e.Handled = true; return; }
                var handle = PointerPressedUniversal;
                handle?.Invoke(_owner, uniEvent);
                //  e.Handled = true;
            }

            public void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
            {
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Down))
                {
                    e.Handled = true;
                    TryShowActionContextMenu();
                    return;
                }
                var handle = PointerPressedUniversal;
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
            }

            public void OnTouchDown(TouchEventArgs e)
            {
                
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Down)) { e.Handled = true; return; }
                var handle = PointerPressedUniversal;
                handle?.Invoke(_owner, uniEvent);
                // e.Handled = true;
            }

            public void OnStylusDown(StylusDownEventArgs e)
            {
                
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Down)) { e.Handled = true; return; }
                var handle = PointerPressedUniversal;
                handle?.Invoke(_owner, uniEvent);
                // e.Handled = true;
                System.Diagnostics.Debug.WriteLine("OnStylusDown");
            }

#endregion Counterpart UWP PoiterPressed

            #region Counterpart UWP PointerMove

            public void OnMouseMove(MouseEventArgs e)
            {
                                              
                if (e.StylusDevice != null) return;

                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Move)) { e.Handled = true; return; }
                var handle = PointerMovedUniversal;
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("OnMouseMove");
            }

            public void OnTouchMove(TouchEventArgs e)
            {
                
                if (e.TouchDevice == null) return;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Move)) { e.Handled = true; return; }
                var handle = PointerMovedUniversal;
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("OnTouchMove");
            }

            public void OnStylusMove(StylusEventArgs e)
            {
                
                if (e.StylusDevice == null) return;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Move)) { e.Handled = true; return; }
                var handle = PointerMovedUniversal;
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("OnStylusMove");
            }

            #endregion Counterpart UWP PointerMove

            #region Counterpart UWP PointerEnter

            public void OnMouseEnter(MouseEventArgs e)
            {
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                var handle = PointerEnteredUniversal;
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("OnMouseEnter");
            }

            public void OnTouchEnter(TouchEventArgs e)
            {
               
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                var handle = PointerEnteredUniversal;
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("OnTouchEnter");

            }

            public void OnStylusEnter(StylusEventArgs e)
            {
               
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                var handle = PointerEnteredUniversal;
                handle?.Invoke(_owner, uniEvent);
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("OnStylusEnter");

            }

            #endregion Counterpart UWP PointerEnter

            public void OnStylusUp(StylusEventArgs e)
            {
                
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Up)) { e.Handled = true; return; }
                var handle = PointerReleasedUniversal;
                handle?.Invoke(_owner, uniEvent);
                // e.Handled = true;
                System.Diagnostics.Debug.WriteLine("OnStylusUp");
            }

            public void OnTouchUp(TouchEventArgs e)
            {
                
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Up)) { e.Handled = true; return; }
                var handle = PointerReleasedUniversal;
                handle?.Invoke(_owner, uniEvent);
                //  e.Handled = true;

            }

            public void OnMouseLeftButtonUp(MouseButtonEventArgs e)
            {
                
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Up)) { e.Handled = true; return; }
                var handle = PointerReleasedUniversal;
                handle?.Invoke(_owner, uniEvent);
                //   e.Handled = true;

            }

            public void OnManipulationStarted(ManipulationStartedEventArgs e)
            {
                
                var handle = ManipulationStartedUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
            }

            public void OnManipulationDelta(ManipulationDeltaEventArgs e)
            {
               
                var handle = ManipulationDeltaUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                handle?.Invoke(_owner, uniEvent);
            }

            public void OnManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e)
            {
                //base.OnManipulationDelta(e);
                //var handle = ManipulationDeltaUnivarsal;
                //// var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
            }

            public void OnManipulationCompleted(ManipulationCompletedEventArgs e)
            {
                
                var handle = ManipulationCompletedUniversal;
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                handle?.Invoke(_owner, uniEvent);
            }

            public void OnManipulationStarting(ManipulationStartingEventArgs e)
            {
                
                var handle = ManipulationStartingUniversal;


                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                uniEvent.ManipulationMode = UniversalEventConverter.ToManipulationModeUni(e.Mode);
                handle?.Invoke(_owner, uniEvent);

            }

            //===========================

        }

        #region Counterpart UWP PoiterPressed

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Redrawable.OnMouseLeftButtonDown(sender,e);
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Redrawable.OnMouseRightButtonDown(sender, e);
        }

        protected override void OnTouchDown(TouchEventArgs e)
        {
            base.OnTouchDown(e);
            Redrawable.OnTouchDown(e);
        }

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            base.OnStylusDown(e);
            Redrawable.OnStylusDown(e);
        }

        #endregion Counterpart UWP PoiterPressed

        #region Counterpart UWP PointerMove

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!this.ForwardMouseEventsOnPenOrTouchInteraction && e.StylusDevice != null)
            { return; };
            base.OnMouseMove(e);
            Redrawable.OnMouseMove(e);
        }

        protected override void OnTouchMove(TouchEventArgs e)
        {
            base.OnTouchMove(e);
            Redrawable.OnTouchMove(e);
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            base.OnStylusMove(e);
            Redrawable.OnStylusMove(e);
        }

        #endregion Counterpart UWP PointerMove

        #region Counterpart UWP PointerEnter

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if (!this.ForwardMouseEventsOnPenOrTouchInteraction && e.StylusDevice != null)
            { return; };

            base.OnMouseEnter(e);
            Redrawable.OnMouseEnter(e);
        }

        protected override void OnTouchEnter(TouchEventArgs e)
        {
            base.OnTouchEnter(e);
            Redrawable.OnTouchEnter(e);
            
        }

        protected override void OnStylusEnter(StylusEventArgs e)
        {
            base.OnStylusEnter(e);
            Redrawable.OnStylusEnter(e);
            
        }

        #endregion Counterpart UWP PointerEnter

        protected override void OnStylusUp(StylusEventArgs e)
        {
            base.OnStylusUp(e);
            Redrawable.OnStylusUp(e);
        }

        protected override void OnTouchUp(TouchEventArgs e)
        {
            base.OnTouchUp(e);
            Redrawable.OnTouchUp(e);
     
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            Redrawable.OnMouseLeftButtonUp(e);
       
        }

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            base.OnManipulationStarted(e);
            Redrawable.OnManipulationStarted(e);
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);
            Redrawable.OnManipulationDelta(e);
        }

        protected override void OnManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e)
        {
            //base.OnManipulationDelta(e);
            //var handle = ManipulationDeltaUnivarsal;
            //// var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            base.OnManipulationCompleted(e);
            Redrawable.OnManipulationCompleted(e);
        }

        protected override void OnManipulationStarting(ManipulationStartingEventArgs e)
        {
            base.OnManipulationStarting(e);
            Redrawable.OnManipulationStarting(e);
          
        }

        private enum PointerPhase { Down, Move, Up }
    }
}


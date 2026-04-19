// PhialeDrawBoxAvalonia.Events.cs
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using UniversalInput.Contracts;
using PhialeGis.Library.AvaloniaUi.Converters;
using PhialeGis.Library.Core.Interactions;
using System;
using System.Diagnostics;

namespace PhialeGis.Library.AvaloniaUi.Controls
{
    public sealed partial class PhialeDrawBoxAvalonia
    {
        internal sealed partial class RedrawableAdapter : IRenderingComposition, IUserInteractive
        {
            private Control _skiaCanvas => _owner;

            public event EventHandler<object>? SurfaceShifted;

            public event EventHandler<UniversalPointerRoutedEventArgs>? PointerPressedUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs>? PointerMovedUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs>? PointerEnteredUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs>? PointerReleasedUniversal;

            public event EventHandler<UniversalManipulationStartingRoutedEventArgs>? ManipulationStartingUniversal;
            public event EventHandler<UniversalManipulationStartedRoutedEventArgs>? ManipulationStartedUniversal;
            public event EventHandler<UniversalManipulationDeltaRoutedEventArgs>? ManipulationDeltaUniversal;
            public event EventHandler<UniversalManipulationCompletedRoutedEventArgs>? ManipulationCompletedUniversal;

            public void RaiseSurfaceShifted(double dx, double dy) =>
                SurfaceShifted?.Invoke(this, new SurfaceMovement(dx, dy));

            // ---------------- Pointer events (Avalonia args) ----------------

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
                    PlacementTarget = _owner,
                    Placement = PlacementMode.Pointer
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

                    var commandId = entry.CommandId ?? string.Empty;
                    var menuItem = new MenuItem
                    {
                        Header = entry.Label ?? string.Empty,
                        IsEnabled = entry.Enabled
                    };

                    menuItem.Click += (_, __) =>
                    {
                        try { manager.TryHandleInteractiveMenuCommand(this, commandId); }
                        catch { /* best-effort */ }
                    };

                    contextMenu.Items.Add(menuItem);
                }

                if (contextMenu.Items.Count > 0)
                    contextMenu.Open(_owner);
            }

            public void OnPointerPressed(PointerPressedEventArgs e)
            {
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                var isSecondary = ResolvePointerButton(uniEvent, PointerPhase.Down) == PointerButton.Secondary;
                if (TryHandlePointer(uniEvent, PointerPhase.Down))
                {
                    e.Handled = true;
                    if (isSecondary) TryShowActionContextMenu();
                    return;
                }
                PointerPressedUniversal?.Invoke(_owner, uniEvent);
                e.Handled = true;
                ProcessCustomResponse(uniEvent.Metadata);
            }

            public void OnPointerMoved(PointerEventArgs e)
            {
                // Debug log: what universal event we are generating
                var pos = e.GetPosition(_skiaCanvas);
                var device = e.Pointer?.Type.ToString() ?? "Unknown";
                Debug.WriteLine($"[PhialeDrawBoxAvalonia] Generating PointerMoved → UniversalPointerRoutedEventArgs " +
                                $"(device={device}, x={pos.X:0.##}, y={pos.Y:0.##})");

                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Move)) { e.Handled = true; return; }
                PointerMovedUniversal?.Invoke(_owner, uniEvent);
                e.Handled = true;
                ProcessCustomResponse(uniEvent.Metadata);
            }

            public void OnPointerEntered(PointerEventArgs e)
            {
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                PointerEnteredUniversal?.Invoke(_owner, uniEvent);
                e.Handled = true;
                ProcessCustomResponse(uniEvent.Metadata);
            }

            public void OnPointerReleased(PointerReleasedEventArgs e)
            {
                var uniEvent = UniversalEventConverter.Convert(e, _skiaCanvas);
                if (TryHandlePointer(uniEvent, PointerPhase.Up)) { e.Handled = true; return; }
                PointerReleasedUniversal?.Invoke(_owner, uniEvent);
                e.Handled = true;
                ProcessCustomResponse(uniEvent.Metadata);
            }

            // -------- Manipulation events (signature parity with UWP) --------
            // Avalonia does not provide OnManipulation* overrides. These methods
            // remain for parity and are expected to be called by higher-level logic.

            public void OnManipulationStarting(UniversalManipulationStartingRoutedEventArgs e)
            {
                ManipulationStartingUniversal?.Invoke(_owner, e);
            }

            public void OnManipulationStarted(UniversalManipulationStartedRoutedEventArgs e)
            {
                ManipulationStartedUniversal?.Invoke(_owner, e);
                ProcessCustomResponse(e.Metadata);
            }

            public void OnManipulationDelta(UniversalManipulationDeltaRoutedEventArgs e)
            {
                ManipulationDeltaUniversal?.Invoke(_owner, e);
                ProcessCustomResponse(e.Metadata);
            }

            public void OnManipulationCompleted(UniversalManipulationCompletedRoutedEventArgs e)
            {
                ManipulationCompletedUniversal?.Invoke(_owner, e);
                ProcessCustomResponse(e.Metadata);
            }

            // ---------------- Post-processing (shared) ----------------

            public void ProcessCustomResponse(UniversalMetadata response)
            {
                if (response == null) return;
                if (response.ResetManipulationMode)
                {
                    _owner.ResetManipulationState();
                }
            }
        }

        // ---------------- Control-level overrides (Avalonia) ----------------

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Redrawable.OnPointerPressed(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            Redrawable.OnPointerMoved(e);
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            Redrawable.OnPointerEntered(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            Redrawable.OnPointerReleased(e);
        }

        // Avalonia has no OnManipulation* overrides; keep parity helper.
        internal void ResetManipulationState()
        {
            // Intentionally empty on Avalonia platform layer.
            // Higher-level logic can reset its own state if needed.
        }

        private enum PointerPhase { Down, Move, Up }
    }
}


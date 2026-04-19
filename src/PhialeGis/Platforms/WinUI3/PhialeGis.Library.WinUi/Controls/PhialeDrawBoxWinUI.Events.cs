// PhialeGis.Library.WinUi/Controls/PhialeDrawBoxWinUI.Events.cs
using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using UniversalInput.Contracts;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.WinUi.Conversions;
using SkiaSharp.Views.Windows;
using Windows.Foundation;

namespace PhialeGis.Library.WinUi.Controls
{
    public sealed partial class PhialeDrawBoxWinUI
    {
        // Extend adapter with input (partial)
        internal sealed partial class RedrawableAdapter : IUserInteractive
        {
            public event EventHandler<object>? SurfaceShifted;
            public event EventHandler<UniversalPointerRoutedEventArgs>? PointerPressedUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs>? PointerMovedUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs>? PointerEnteredUniversal;
            public event EventHandler<UniversalPointerRoutedEventArgs>? PointerReleasedUniversal;

            public event EventHandler<UniversalManipulationStartingRoutedEventArgs>? ManipulationStartingUniversal;
            public event EventHandler<UniversalManipulationStartedRoutedEventArgs>? ManipulationStartedUniversal;
            public event EventHandler<UniversalManipulationDeltaRoutedEventArgs>? ManipulationDeltaUniversal;
            public event EventHandler<UniversalManipulationCompletedRoutedEventArgs>? ManipulationCompletedUniversal;

            private SKXamlCanvas? _sk => _owner._skiaCanvas;

            internal void RaiseSurfaceShifted(double dx, double dy)
                => SurfaceShifted?.Invoke(this, new SurfaceMovement(dx, dy));

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
                var canvas = _sk;
                if (manager == null || canvas == null) return;

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

                flyout.ShowAt(canvas, new FlyoutShowOptions
                {
                    Position = new Point(payload.ScreenPosition.X, payload.ScreenPosition.Y)
                });
            }

            // native → universal
            public void OnPointerPressed(PointerRoutedEventArgs e)
            {
                if (_sk == null) return;
                var uni = UniversalEventConverterWinUi.Convert(e, _sk);
                var isSecondary = ResolvePointerButton(uni, PointerPhase.Down) == PointerButton.Secondary;
                if (TryHandlePointer(uni, PointerPhase.Down))
                {
                    e.Handled = true;
                    if (isSecondary) TryShowActionContextMenu();
                    return;
                }
                PointerPressedUniversal?.Invoke(_owner, uni);
                e.Handled = true;
                ProcessCustomResponse(uni.Metadata);
            }
            public void OnPointerMoved(PointerRoutedEventArgs e)
            {
                if (_sk == null) return;
                var uni = UniversalEventConverterWinUi.Convert(e, _sk);
                if (TryHandlePointer(uni, PointerPhase.Move)) { e.Handled = true; return; }
                PointerMovedUniversal?.Invoke(_owner, uni);
                e.Handled = true;
                ProcessCustomResponse(uni.Metadata);
            }
            public void OnPointerEntered(PointerRoutedEventArgs e)
            {
                if (_sk == null) return;
                var uni = UniversalEventConverterWinUi.Convert(e, _sk);
                PointerEnteredUniversal?.Invoke(_owner, uni);
                e.Handled = true;
                ProcessCustomResponse(uni.Metadata);
            }
            public void OnPointerReleased(PointerRoutedEventArgs e)
            {
                if (_sk == null) return;
                var uni = UniversalEventConverterWinUi.Convert(e, _sk);
                if (TryHandlePointer(uni, PointerPhase.Up)) { e.Handled = true; return; }
                PointerReleasedUniversal?.Invoke(_owner, uni);
                e.Handled = true;
                ProcessCustomResponse(uni.Metadata);
            }

            public void OnManipulationStarting(ManipulationStartingRoutedEventArgs e)
            {
                if (_sk == null) return;
                var uni = UniversalEventConverterWinUi.Convert(e, _sk);
                ManipulationStartingUniversal?.Invoke(_owner, uni);
            }
            public void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
            {
                if (_sk == null) return;
                var uni = UniversalEventConverterWinUi.Convert(e, _sk);
                ManipulationStartedUniversal?.Invoke(_owner, uni);
                ProcessCustomResponse(uni.Metadata);
            }
            public void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
            {
                if (_sk == null) return;
                var uni = UniversalEventConverterWinUi.Convert(e, _sk);
                ManipulationDeltaUniversal?.Invoke(_owner, uni);
                ProcessCustomResponse(uni.Metadata);
            }
            public void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
            {
                if (_sk == null) return;
                var uni = UniversalEventConverterWinUi.Convert(e, _sk);
                ManipulationCompletedUniversal?.Invoke(_owner, uni);
                ProcessCustomResponse(uni.Metadata);
            }

            private void ProcessCustomResponse(UniversalMetadata meta)
            {
                if (meta?.ResetManipulationMode == true)
                    _owner.ManipulationMode = _owner.DefaultManipulationMode;
            }
        }

        // Control overrides → adapter
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        { base.OnPointerPressed(e); Redrawable.OnPointerPressed(e); }

        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        { base.OnPointerMoved(e); Redrawable.OnPointerMoved(e); }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        { base.OnPointerEntered(e); Redrawable.OnPointerEntered(e); }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        { base.OnPointerReleased(e); Redrawable.OnPointerReleased(e); }

        protected override void OnManipulationStarting(ManipulationStartingRoutedEventArgs e)
        { base.OnManipulationStarting(e); Redrawable.OnManipulationStarting(e); }

        protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
        { base.OnManipulationStarted(e); Redrawable.OnManipulationStarted(e); }

        protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
        { base.OnManipulationDelta(e); Redrawable.OnManipulationDelta(e); }

        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        { base.OnManipulationCompleted(e); Redrawable.OnManipulationCompleted(e); }

        // helper used by base file
        private void Redrawable_RaiseSurfaceShifted(double dx, double dy) => Redrawable.RaiseSurfaceShifted(dx, dy);

        private enum PointerPhase { Down, Move, Up }
    }
}


using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Input;
using PhialeGis.Library.Abstractions.Ui.Enums;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Graphics;
using UniversalInput.Contracts;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PhialeGis.Library.Core.Interactions.Activities
{
    internal abstract class BaseActivity
    {
        private readonly Subject<CoreInputEvent> _inputSubject = new Subject<CoreInputEvent>();

        protected ViewportManager _viewPortManager;
        protected EventHandler<RedrawEventArgs> _redrawRequested;
        public ICursorManager CursorManager { get; set; }

        public IObservable<CoreInputEvent> EventObservable => _inputSubject.AsObservable();

        protected bool isActive;

        protected CorePoint ManipulationBasePoint { get; set; }
        protected CorePoint StartingBasePoint { get; set; }

        public Action FinishActivityAction { get; set; }

        public abstract FsmBehavior Behavior { get; }

        protected BaseActivity()
        {
            isActive = false;
        }

        public virtual void Activate()
        {
            isActive = true;
        }

        public virtual void Deactivate()
        {
            isActive = false;
        }

        public virtual void HandleInitialInput(CoreInputEvent input)
        {
            if (input == null) return;
            input.ResetManipulationMode = true;

            switch (input.Kind)
            {
                case CoreInputKind.PointerPressed:
                    if (input.Pointer != null) OnCorePointerPressed(input.Pointer);
                    break;
                case CoreInputKind.PointerMoved:
                    if (input.Pointer != null) OnCorePointerMoved(input.Pointer);
                    break;
                case CoreInputKind.PointerReleased:
                    if (input.Pointer != null) OnCorePointerReleased(input.Pointer);
                    break;
                case CoreInputKind.ManipulationStarting:
                    if (input.Manipulation != null) OnCoreManipulationStarting(input.Manipulation);
                    break;
                case CoreInputKind.ManipulationStarted:
                    if (input.Manipulation != null) OnCoreManipulationStarted(input.Manipulation);
                    break;
                case CoreInputKind.ManipulationDelta:
                    if (input.Manipulation != null) OnCoreManipulationDelta(input.Manipulation);
                    break;
                case CoreInputKind.ManipulationCompleted:
                    if (input.Manipulation != null) OnCoreManipulationCompleted(input.Manipulation);
                    break;
                default:
                    Emit(new CoreInputEvent { Kind = input.Kind, DeviceType = input.DeviceType, ResetManipulationMode = true });
                    break;
            }
        }

        protected virtual void OnCorePointerPressed(CorePointerInput input)
        {
            Emit(new CoreInputEvent { Kind = CoreInputKind.PointerPressed, DeviceType = input.DeviceType, Pointer = input });
        }

        protected virtual void OnCorePointerMoved(CorePointerInput input)
        {
            Emit(new CoreInputEvent { Kind = CoreInputKind.PointerMoved, DeviceType = input.DeviceType, Pointer = input });
        }

        protected virtual void OnCorePointerEntered(CorePointerInput input)
        {
            Emit(new CoreInputEvent { Kind = CoreInputKind.PointerEntered, DeviceType = input.DeviceType, Pointer = input });
        }

        protected virtual void OnCorePointerReleased(CorePointerInput input)
        {
            Emit(new CoreInputEvent { Kind = CoreInputKind.PointerReleased, DeviceType = input.DeviceType, Pointer = input });
        }

        protected virtual void OnCoreManipulationStarting(CoreManipulationInput input)
        {
            if (input.HasPivotCenter)
                StartingBasePoint = input.PivotCenter;

            Emit(new CoreInputEvent { Kind = CoreInputKind.ManipulationStarting, DeviceType = input.DeviceType, Manipulation = input });
        }

        protected virtual void OnCoreManipulationStarted(CoreManipulationInput input)
        {
            ManipulationBasePoint = input.Position;
            Emit(new CoreInputEvent { Kind = CoreInputKind.ManipulationStarted, DeviceType = input.DeviceType, Manipulation = input });
        }

        protected virtual void OnCoreManipulationDelta(CoreManipulationInput input)
        {
            Emit(new CoreInputEvent { Kind = CoreInputKind.ManipulationDelta, DeviceType = input.DeviceType, Manipulation = input });
        }

        protected virtual void OnCoreManipulationCompleted(CoreManipulationInput input)
        {
            Emit(new CoreInputEvent { Kind = CoreInputKind.ManipulationCompleted, DeviceType = input.DeviceType, Manipulation = input });
        }

        private void Emit(CoreInputEvent input)
        {
            _inputSubject.OnNext(input);
        }

        public void Initialize(IUserInteractive userInteractive,
            ViewportManager viewportManager,
            EventHandler<RedrawEventArgs> redrawRequested)
        {
            if (userInteractive == null) return;

            userInteractive.PointerMovedUniversal += OnPointerMoved;
            userInteractive.PointerPressedUniversal += OnPointerPressed;
            userInteractive.PointerEnteredUniversal += OnPointerEntered;
            userInteractive.PointerReleasedUniversal += OnPointerReleased;
            userInteractive.ManipulationStartedUniversal += OnManipulationStarted;
            userInteractive.ManipulationStartingUniversal += OnManipulationStarting;
            userInteractive.ManipulationDeltaUniversal += OnManipulationDelta;
            userInteractive.ManipulationCompletedUniversal += OnManipulationCompleted;

            _viewPortManager = viewportManager;
            _redrawRequested = redrawRequested;
        }

        public void Finalize(IUserInteractive userInteractive)
        {
            if (userInteractive == null) return;

            userInteractive.PointerMovedUniversal -= OnPointerMoved;
            userInteractive.PointerPressedUniversal -= OnPointerPressed;
            userInteractive.PointerEnteredUniversal -= OnPointerEntered;
            userInteractive.PointerReleasedUniversal -= OnPointerReleased;
            userInteractive.ManipulationStartedUniversal -= OnManipulationStarted;
            userInteractive.ManipulationStartingUniversal -= OnManipulationStarting;
            userInteractive.ManipulationDeltaUniversal -= OnManipulationDelta;
            userInteractive.ManipulationCompletedUniversal -= OnManipulationCompleted;
        }

        private void OnPointerPressed(object sender, UniversalPointerRoutedEventArgs e)
        {
            OnCorePointerPressed(ToCorePointerInput(e));
        }

        private void OnPointerEntered(object sender, UniversalPointerRoutedEventArgs e)
        {
            OnCorePointerEntered(ToCorePointerInput(e));
        }

        private void OnPointerMoved(object sender, UniversalPointerRoutedEventArgs e)
        {
            OnCorePointerMoved(ToCorePointerInput(e));
        }

        private void OnPointerReleased(object sender, UniversalPointerRoutedEventArgs e)
        {
            OnCorePointerReleased(ToCorePointerInput(e));
        }

        private void OnManipulationStarting(object sender, UniversalManipulationStartingRoutedEventArgs e)
        {
            OnCoreManipulationStarting(ToCoreManipulationInput(e));
        }

        private void OnManipulationStarted(object sender, UniversalManipulationStartedRoutedEventArgs e)
        {
            OnCoreManipulationStarted(ToCoreManipulationInput(e));
        }

        private void OnManipulationDelta(object sender, UniversalManipulationDeltaRoutedEventArgs e)
        {
            OnCoreManipulationDelta(ToCoreManipulationInput(e));
        }

        private void OnManipulationCompleted(object sender, UniversalManipulationCompletedRoutedEventArgs e)
        {
            OnCoreManipulationCompleted(ToCoreManipulationInput(e));
        }

        private static CorePointerInput ToCorePointerInput(UniversalPointerRoutedEventArgs e)
        {
            return new CorePointerInput
            {
                PointerId = e?.Pointer?.PointerId ?? 0,
                DeviceType = ToCoreDeviceType(e?.Pointer?.PointerDeviceType ?? DeviceType.Other),
                Position = ToCorePoint(e?.Pointer?.Position ?? default)
            };
        }

        private static CoreManipulationInput ToCoreManipulationInput(UniversalManipulationStartingRoutedEventArgs e)
        {
            var hasPivot = e?.Pivot != null;
            return new CoreManipulationInput
            {
                DeviceType = ToCoreDeviceType(e?.PointerDeviceType ?? DeviceType.Other),
                Position = default,
                HasPivotCenter = hasPivot,
                PivotCenter = hasPivot ? ToCorePoint(e.Pivot.Center) : default,
                Scale = 1d
            };
        }

        private static CoreManipulationInput ToCoreManipulationInput(UniversalManipulationStartedRoutedEventArgs e)
        {
            return new CoreManipulationInput
            {
                DeviceType = ToCoreDeviceType(e?.PointerDeviceType ?? DeviceType.Other),
                Position = ToCorePoint(e?.Position ?? default),
                HasPivotCenter = false,
                PivotCenter = default,
                Scale = e?.Cumulative != null ? e.Cumulative.Scale : 1d
            };
        }

        private static CoreManipulationInput ToCoreManipulationInput(UniversalManipulationDeltaRoutedEventArgs e)
        {
            return new CoreManipulationInput
            {
                DeviceType = ToCoreDeviceType(e?.PointerDeviceType ?? DeviceType.Other),
                Position = ToCorePoint(e?.Position ?? default),
                HasPivotCenter = false,
                PivotCenter = default,
                Scale = e?.Delta != null ? e.Delta.Scale : 1d
            };
        }

        private static CoreManipulationInput ToCoreManipulationInput(UniversalManipulationCompletedRoutedEventArgs e)
        {
            return new CoreManipulationInput
            {
                DeviceType = ToCoreDeviceType(e?.PointerDeviceType ?? DeviceType.Other),
                Position = ToCorePoint(e?.Position ?? default),
                HasPivotCenter = false,
                PivotCenter = default,
                Scale = e?.Cumulative != null ? e.Cumulative.Scale : 1d
            };
        }

        private static CorePoint ToCorePoint(UniversalPoint point)
        {
            return new CorePoint { X = point.X, Y = point.Y };
        }

        private static CoreDeviceType ToCoreDeviceType(DeviceType deviceType)
        {
            switch (deviceType)
            {
                case DeviceType.Touch:
                    return CoreDeviceType.Touch;
                case DeviceType.Pen:
                    return CoreDeviceType.Pen;
                case DeviceType.Mouse:
                    return CoreDeviceType.Mouse;
                case DeviceType.MultiTouch:
                    return CoreDeviceType.MultiTouch;
                default:
                    return CoreDeviceType.Other;
            }
        }
    }
}

using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Input;
using PhialeGis.Library.Abstractions.Ui.Enums;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Graphics;
using PhialeGis.Library.Core.Interactions.Activities;
using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Interactions
{
    internal class InteractionMonitor
    {
        /// <summary>
        /// Dictionary for FSM table
        /// Type - Inherited interface for current action, String - event name.
        /// </summary>

        private IDisposable _currentSubscription;
        private Dictionary<(FsmBehavior, CoreInputKind, CoreDeviceType), Func<BaseActivity>> stateTransitions;
        private IUserInteractive _userInteractive;
        private ViewportManager _viewportManager;
        private ICursorManager _cursorManager;
        private EventHandler<RedrawEventArgs> _redrawRequested;

        public BaseActivity CurrentActivity { get; private set; }
        public BaseActivity SuspendedActivity { get; private set; }

        public InteractionMonitor(IUserInteractive userInteractive,
            ViewportManager viewportManager,ICursorManager cursorManager, EventHandler<RedrawEventArgs> redrawRequested)
        {
            _viewportManager = viewportManager;
            _redrawRequested = redrawRequested;
            _userInteractive = userInteractive;
            _cursorManager = cursorManager;
            ApplyDefaultActivity();
            InitializeStateTransitions();
        }

        public void ApplyDefaultActivity()
        {
            if (CurrentActivity != null)
            { CurrentActivity.Finalize(_userInteractive); };
            CurrentActivity = new DefaultActivity();
            InitializeActivity();
        }

        private void InitializeStateTransitions()
        {
            stateTransitions = new Dictionary<(FsmBehavior, CoreInputKind, CoreDeviceType), Func<BaseActivity>>
        {
            { (FsmBehavior.Idle, CoreInputKind.PointerPressed, CoreDeviceType.Pen), () => new PanActivity() },
            { (FsmBehavior.Idle, CoreInputKind.PointerPressed, CoreDeviceType.Mouse), () => new PanActivity() },
            { (FsmBehavior.Idle, CoreInputKind.PointerPressed, CoreDeviceType.Touch), () => new PanActivity() },
            { (FsmBehavior.Regular, CoreInputKind.ManipulationDelta, CoreDeviceType.MultiTouch), () => new MultiTouchActivity() },
            { (FsmBehavior.Idle, CoreInputKind.ManipulationDelta, CoreDeviceType.MultiTouch), () => new MultiTouchActivity() },
            { (FsmBehavior.Regular, CoreInputKind.ManipulationCompleted, CoreDeviceType.Pen), () => new DefaultActivity() },
        };  
        }

        private void InitializeActivity()
        {
            CurrentActivity.Initialize(_userInteractive, _viewportManager, _redrawRequested);
            CurrentActivity.CursorManager = _cursorManager;
            CurrentActivity.FinishActivityAction = () =>
            {
                ApplyDefaultActivity();
            };
            _currentSubscription = CurrentActivity.EventObservable.Subscribe(HandleEvent);
        }

        public void HandleEvent(CoreInputEvent inputEvent)
        {
            var transition = (CurrentActivity.Behavior, inputEvent.Kind, inputEvent.DeviceType);
            if (stateTransitions.TryGetValue(transition, out var activityFunc))
            {
                CurrentActivity?.Finalize(_userInteractive);
                BaseActivity activity = activityFunc.Invoke();
                CurrentActivity = activity;
                InitializeActivity();
                _currentSubscription?.Dispose();
                _currentSubscription = activity.EventObservable.Subscribe(HandleEvent);
                activity.Activate();
                activity?.HandleInitialInput(inputEvent);
            }
        }

        private FsmBehavior DetermineNewStatus(BaseActivity activity)
        {
            return activity.Behavior;
        }
    }
}

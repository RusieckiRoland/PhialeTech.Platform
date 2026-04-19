using System;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Modes;

namespace PhialeGis.Library.Core.Interactions
{
    /// <summary>
    /// Application-level FSM. Owns the current action, exposes required DSL mode,
    /// and routes input to the action. No per-action logic here.
    /// </summary>
    public sealed class MainInteractionFsm
    {
        public enum FsmState { Idle = 0, Running = 1, Suspended = 2 }

        public FsmState State { get; private set; } = FsmState.Idle;
        public Guid CurrentActionId { get; private set; } = Guid.Empty;
        public string CurrentActionName { get; private set; }
        public DslMode RequiredMode { get; private set; } = DslMode.Normal;
        public object CurrentTargetDraw { get; private set; }

        public IInteractionAction CurrentAction => _action;
        private IInteractionAction _action;

        // Lifecycle events (re-emitted from underlying action)
        public event EventHandler<object> OnStart;
        public event EventHandler<object> OnChange;
        public event EventHandler<object> OnFinish;
        public event EventHandler<object> OnSuspend;

        /// <summary>Starts a new interactive action in this FSM instance.</summary>
        public void StartAction(IInteractionAction action, object targetDraw, string languageId = null)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            CurrentActionId = Guid.NewGuid();
            CurrentActionName = action.Name;
            CurrentTargetDraw = targetDraw;

            // Determine required DSL mode from the action (or fall back to Normal).
            RequiredMode = (action is IDslModeProvider mp) ? mp.RequiredMode : DslMode.Normal;

            // Wire action → re-emit via FSM
            _action.Changed += (s, payload) => OnChange?.Invoke(this, payload);
            _action.Finished += (s, payload) =>
            {
                State = FsmState.Idle;
                OnFinish?.Invoke(this, payload);
                _action = null;
                CurrentActionId = Guid.Empty;
                CurrentActionName = null;
                RequiredMode = DslMode.Normal; // reset mode on finish
                CurrentTargetDraw = null;
            };
            _action.Suspended += (s, payload) =>
            {
                State = FsmState.Suspended;
                if (payload is ActionSuspendPayload asp)
                {
                    if (asp.ActionId == Guid.Empty) asp.ActionId = CurrentActionId;
                    if (string.IsNullOrEmpty(asp.ActionName)) asp.ActionName = CurrentActionName;
                    if (asp.TargetDraw == null) asp.TargetDraw = CurrentTargetDraw;
                }
                OnSuspend?.Invoke(this, payload);
            };

            // Enter Running and notify start
            State = FsmState.Running;
            OnStart?.Invoke(this, new StartPayload
            {
                ActionId = CurrentActionId,
                ActionName = CurrentActionName ?? string.Empty,
                TargetDraw = targetDraw
            });

            _action.Start(new ActionContext
            {
                ActionId = CurrentActionId,
                TargetDraw = targetDraw,
                LanguageId = string.IsNullOrWhiteSpace(languageId) ? "en" : languageId
            });
        }

        /// <summary>Routes a line of user input to the current action.</summary>
        public void HandleInput(string line)
        {
            if (State != FsmState.Running || _action == null) return;
            _action.HandleInput(line ?? string.Empty);
        }

        /// <summary>Suspends the current action.</summary>
        public void Suspend(string reason)
        {
            if (State != FsmState.Running || _action == null) return;
            if (!_action.CanBeSuspended) return;
            _action.Suspend(reason ?? string.Empty);
        }

        /// <summary>Resumes a previously suspended action.</summary>
        public void Resume()
        {
            if (State != FsmState.Suspended || _action == null) return;
            State = FsmState.Running;
            _action.Resume();
        }

        /// <summary>Cancels the current action. Always returns to Idle.</summary>
        public void Cancel()
        {
            if (_action != null)
            {
                _action.Cancel(); // action should emit Finished(Success=false)
            }
            else
            {
                State = FsmState.Idle;
                CurrentActionId = Guid.Empty;
                CurrentActionName = null;
                RequiredMode = DslMode.Normal;
                CurrentTargetDraw = null;
            }
        }

        // Lightweight payload for consumers (UI/bridge).
        public sealed class StartPayload
        {
            public Guid ActionId { get; set; }
            public string ActionName { get; set; }
            public object TargetDraw { get; set; }
        }
    }
}

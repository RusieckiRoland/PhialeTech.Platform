using System;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Modes;

namespace PhialeGis.Library.Actions.Base
{
    /// <summary>
    /// Minimal base class for interactive actions:
    /// - Owns standard events (Changed/Finished/Suspended)
    /// - Exposes protected helpers to emit events
    /// - Provides SetPrompt() for editor prompt propagation
    /// Derived classes only implement Start/HandleInput/Resume/Cancel specifics.
    /// </summary>
    public abstract class InteractiveActionBase : IInteractionAction, IDslModeProvider
    {
        public abstract string Name { get; }
        public virtual bool CanBeSuspended => true;

        /// <summary>Required DSL mode for this action (default: Normal).</summary>
        public virtual DslMode RequiredMode => DslMode.Normal;

        public event EventHandler<object> Changed;
        public event EventHandler<object> Finished;
        public event EventHandler<object> Suspended;

        /// <summary>Last prompt set by this action (optional read by derived).</summary>
        protected DslPromptDto CurrentPrompt { get; private set; }

        /// <summary>
        /// Sets the current prompt and emits a Changed(ActionPromptPayload) event.
        /// This is the only supported way for actions to update the editor prompt.
        /// </summary>
        protected void SetPrompt(string modeText, string chipHtml, string kind = "idle")
        {
            CurrentPrompt = new DslPromptDto
            {
                ModeText = modeText ?? string.Empty,
                ChipHtml = chipHtml ?? string.Empty,
                Kind = string.IsNullOrWhiteSpace(kind) ? "idle" : kind
            };
            EmitChanged(new ActionPromptPayload { Prompt = CurrentPrompt });
        }

        /// <summary>Utility to emit a generic Changed payload.</summary>
        protected void EmitChanged(object payload)
        {
            var h = Changed;
            if (h != null) h(this, payload);
        }

        /// <summary>Utility to emit Finished with a standard payload.</summary>
        protected void EmitFinished(ActionFinishPayload payload)
        {
            var h = Finished;
            if (h != null) h(this, payload);
        }

        /// <summary>Utility to emit Suspended with a reason.</summary>
        protected void EmitSuspended(string reason)
        {
            var h = Suspended;
            if (h != null) h(this, new ActionSuspendPayload { Reason = reason ?? string.Empty });
        }

        // Derived classes must implement interactive lifecycle:
        public abstract void Start(ActionContext ctx);
        public abstract void HandleInput(string line);
        public virtual void Suspend(string reason) => EmitSuspended(reason);
        public virtual void Resume() { /* default no-op */ }
        public virtual void Cancel() =>
            EmitFinished(new ActionFinishPayload { Success = false, Message = "Canceled" });
    }
}

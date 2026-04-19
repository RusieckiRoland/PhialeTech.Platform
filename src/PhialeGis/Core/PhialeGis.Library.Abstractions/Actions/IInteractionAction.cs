using PhialeGis.Library.Abstractions.Actions;
using System;

namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Application-level interactive action (e.g., AddLineString).
    /// Encapsulates interaction logic; FSM only routes input and listens to events.
    /// </summary>
    public interface IInteractionAction
    {
        string Name { get; }
        bool CanBeSuspended { get; }

        event EventHandler<object> Changed;   // payload: ActionChangePayload
        event EventHandler<object> Finished;  // payload: ActionFinishPayload
        event EventHandler<object> Suspended; // payload: ActionSuspendPayload

        void Start(ActionContext ctx);
        void HandleInput(string line);
        void Suspend(string reason);
        void Resume();
        void Cancel();
    }
}

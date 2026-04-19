using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Interactions;

namespace PhialeGis.Library.Core.Interactions
{
    internal sealed class ActionSession
    {
        public ActionSession(IEditorInteractive editor, object inputTargetDraw, MainInteractionFsm fsm)
        {
            SessionId = Guid.NewGuid();
            Editor = editor;
            InputTargetDraw = inputTargetDraw;
            Fsm = fsm;
        }

        public Guid SessionId { get; }

        public IEditorInteractive Editor { get; }

        public object InputTargetDraw { get; set; }

        public MainInteractionFsm Fsm { get; }

        public string LastPromptText { get; set; } = string.Empty;
    }

    internal sealed class ActionSessionRegistry
    {
        private readonly List<ActionSession> _stack = new List<ActionSession>();

        public ActionSession GetActive()
        {
            if (_stack.Count == 0)
                return null;

            return _stack[_stack.Count - 1];
        }

        public ActionSession GetPrevious()
        {
            if (_stack.Count < 2)
                return null;

            return _stack[_stack.Count - 2];
        }

        public ActionSession GetByEditor(IEditorInteractive editor)
        {
            if (editor == null)
                return null;

            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_stack[i].Editor, editor))
                    return _stack[i];
            }

            return null;
        }

        public ActionSession GetByInputTarget(object targetDraw)
        {
            if (targetDraw == null)
                return null;

            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_stack[i].InputTargetDraw, targetDraw))
                    return _stack[i];
            }

            return null;
        }

        public bool IsActive(ActionSession session)
        {
            var active = GetActive();
            return active != null && ReferenceEquals(active, session);
        }

        public void Add(ActionSession session)
        {
            if (session == null)
                return;

            _stack.Add(session);
        }

        public void Remove(ActionSession session)
        {
            if (session == null)
                return;

            _stack.RemoveAll(s => ReferenceEquals(s, session));
        }

        public void TransferInputTarget(ActionSession session, object targetDraw)
        {
            if (session == null || targetDraw == null)
                return;

            session.InputTargetDraw = targetDraw;
        }
    }
}

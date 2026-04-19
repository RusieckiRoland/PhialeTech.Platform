using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Editing
{
    public interface IGridEditCommand
    {
        void Execute();

        void Undo();
    }

    public sealed class GridEditHistory
    {
        private readonly Stack<IGridEditCommand> _undoStack = new Stack<IGridEditCommand>();
        private readonly Stack<IGridEditCommand> _redoStack = new Stack<IGridEditCommand>();

        public int UndoCount => _undoStack.Count;

        public int RedoCount => _redoStack.Count;

        public void Execute(IGridEditCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
        }

        public bool Undo()
        {
            if (_undoStack.Count == 0)
            {
                return false;
            }

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            return true;
        }

        public bool Redo()
        {
            if (_redoStack.Count == 0)
            {
                return false;
            }

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            return true;
        }
    }
}

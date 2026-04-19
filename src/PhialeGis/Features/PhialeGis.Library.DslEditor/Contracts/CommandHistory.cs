// PhialeGis.Library.DslEditor/Contracts/CommandHistory.cs
using System;
using System.Collections.Generic;

namespace PhialeGis.Library.DslEditor.Contracts
{
    /// <summary>
    /// In-memory command history with draft preservation and de-duplication.
    /// </summary>
    public sealed class CommandHistory
    {
        private readonly List<string> _items = new List<string>();
        private readonly int _capacity;
        private int _index = -1;               // -1 means "beyond last" (blank input)
        private string _draft = string.Empty;  // current, not yet committed input

        public CommandHistory(int capacity = 200)
        {
            if (capacity < 1) capacity = 1;
            _capacity = capacity;
        }

        /// <summary>Pushes a new command to history; ignores empty and consecutive duplicates.</summary>
        public void Push(string command)
        {
            var c = (command ?? string.Empty).Trim();
            if (c.Length == 0)
            {
                ResetCursor();
                return;
            }

            if (_items.Count > 0 && string.Equals(_items[_items.Count - 1], c, StringComparison.Ordinal))
            {
                ResetCursor();
                return;
            }

            _items.Add(c);
            if (_items.Count > _capacity)
                _items.RemoveAt(0);

            ResetCursor();
        }

        /// <summary>Saves current draft text before browsing history.</summary>
        public void SaveDraft(string text)
        {
            _draft = text ?? string.Empty;
        }

        /// <summary>Moves to previous history item (ArrowUp). Returns null if none.</summary>
        public string Prev()
        {
            if (_items.Count == 0) return null;

            if (_index < 0)
            {
                // first time going up: save draft and go to last item
                _index = _items.Count - 1;
                return _items[_index];
            }

            if (_index > 0)
            {
                _index--;
                return _items[_index];
            }

            // already at oldest
            return _items[_index];
        }

        /// <summary>Moves to next history item (ArrowDown). Returns draft when leaving history.</summary>
        public string Next()
        {
            if (_items.Count == 0) return _draft;

            if (_index < 0)
            {
                // not in history: stay on draft
                return _draft;
            }

            if (_index < _items.Count - 1)
            {
                _index++;
                return _items[_index];
            }

            // leaving history: restore draft
            ResetCursor();
            return _draft;
        }

        /// <summary>Clears all items and draft.</summary>
        public void Clear()
        {
            _items.Clear();
            _draft = string.Empty;
            ResetCursor();
        }

        private void ResetCursor()
        {
            _index = -1;
        }
    }
}

// File: PhialeGis.Library.Core/Modes/DslContextRegistry.cs
// English comments only.
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Localization;
using PhialeGis.Library.Abstractions.Modes;

namespace PhialeGis.Library.Core.Modes
{
    /// Thread-safe per-editor context store. Immutable snapshots only.
    public sealed class DslContextRegistry : IDslContextProvider
    {
        private readonly ConcurrentDictionary<IEditorInteractive, DslContext> _byEditor;

        public DslContextRegistry()
        {
            _byEditor = new ConcurrentDictionary<IEditorInteractive, DslContext>(RefComparer.Instance);
        }

        public DslContext GetFor(IEditorInteractive editor)
        {
            if (editor == null) return CreateDefaultContext();
            DslContext ctx;
            return _byEditor.TryGetValue(editor, out ctx) && ctx != null ? ctx : CreateDefaultContext();
        }

        public void SetFor(IEditorInteractive editor, DslContext ctx)
        {
            if (editor == null || ctx == null) return;
            _byEditor[editor] = ctx; // atomic replace
        }

        public void ClearFor(IEditorInteractive editor)
        {
            if (editor == null) return;
            DslContext _;
            _byEditor.TryRemove(editor, out _);
        }

        private static DslContext CreateDefaultContext()
        {
            return new DslContext
            {
                Mode = DslMode.Normal,
                LanguageId = DslUiLocalization.NormalizeLanguageId(null)
            };
        }

        private sealed class RefComparer : IEqualityComparer<IEditorInteractive>
        {
            internal static readonly RefComparer Instance = new RefComparer();
            public bool Equals(IEditorInteractive x, IEditorInteractive y) => ReferenceEquals(x, y);
            public int GetHashCode(IEditorInteractive obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}

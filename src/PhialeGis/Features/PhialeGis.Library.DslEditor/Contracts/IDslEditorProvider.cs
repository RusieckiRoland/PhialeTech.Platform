// PhialeGis.Library.DslEditor/Contracts/IDslEditorProvider.cs
using System;

namespace PhialeGis.Library.DslEditor.Contracts
{
    /// <summary>
    /// Exposes the DSL editor manager without forcing core abstractions
    /// to reference DSL-specific types.
    /// </summary>
    public interface IDslEditorProvider
    {
        IDslEditorManager Editors { get; }
    }
}

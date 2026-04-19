using System;

namespace PhialeGis.Library.Abstractions.Interactions.Editors
{
    // Public enum must be int32 to stay WinRT-safe.
    public enum EditorMode
    {
        Command = 0,
        Script = 1
    }
}

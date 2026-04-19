using System;

namespace UniversalInput.Contracts
{
    /// <summary>
    /// Represents keyboard modifiers in a platform-neutral way.
    /// </summary>
    [Flags]
    public enum UniversalModifierKeys
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4,
        Windows = 8,
    }
}

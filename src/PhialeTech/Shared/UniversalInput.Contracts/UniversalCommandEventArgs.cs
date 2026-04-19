using System.Collections.Generic;

namespace UniversalInput.Contracts
{
    /// <summary>Generic editor command (e.g., "save", "formatDocument").</summary>
    public sealed class UniversalCommandEventArgs : IUniversalBase
    {
        /// <summary>Identifier like "ctrl+s", "formatDocument", "goToDefinition".</summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>Keyboard modifiers for shortcut-originated commands.</summary>
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }

        /// <summary>String arguments associated with the command.</summary>
        public IDictionary<string, string> Arguments { get; } = new Dictionary<string, string>();

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalCommandEventArgs(string commandId, bool ctrl, bool alt, bool shift)
        {
            CommandId = commandId ?? string.Empty;
            Ctrl = ctrl; Alt = alt; Shift = shift;
        }
    }
}

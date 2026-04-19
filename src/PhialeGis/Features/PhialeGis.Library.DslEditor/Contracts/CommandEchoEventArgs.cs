// PhialeGis.Library.DslEditor/Contracts/CommandEvents.cs
using System;

namespace PhialeGis.Library.DslEditor.Contracts
{
    /// <summary>Echo of a submitted command.</summary>
    public sealed class CommandEchoEventArgs : EventArgs
    {
        public string Command { get; }
        public CommandEchoEventArgs(string command) { Command = command ?? string.Empty; }
    }

    /// <summary>Result of a command execution.</summary>
    public sealed class CommandExecutedEventArgs : EventArgs
    {
        public bool Success { get; }
        public string Output { get; }
        public string Error { get; }

        public CommandExecutedEventArgs(bool success, string output, string error)
        {
            Success = success;
            Output = output ?? string.Empty;
            Error = error ?? string.Empty;
        }
    }
}

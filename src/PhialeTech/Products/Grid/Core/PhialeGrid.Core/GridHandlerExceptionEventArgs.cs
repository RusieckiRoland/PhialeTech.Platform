using System;

namespace PhialeGrid.Core
{
    public sealed class GridHandlerExceptionEventArgs : EventArgs
    {
        public GridHandlerExceptionEventArgs(string eventName, Exception exception)
        {
            EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public string EventName { get; }

        public Exception Exception { get; }
    }
}

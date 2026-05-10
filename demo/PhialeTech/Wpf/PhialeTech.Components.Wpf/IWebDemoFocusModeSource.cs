using System;
using System.Threading.Tasks;

namespace PhialeTech.Components.Wpf
{
    internal interface IWebDemoFocusModeSource
    {
        bool IsFocusMode { get; }

        bool ShowPrimaryFocusAction { get; }

        string PrimaryFocusActionText { get; }

        Task ExecutePrimaryFocusActionAsync();

        void ExitFocusMode();

        event EventHandler<WebDemoFocusModeChangedEventArgs> FocusModeChanged;
    }

    internal sealed class WebDemoFocusModeChangedEventArgs : EventArgs
    {
        public WebDemoFocusModeChangedEventArgs(bool isFocusMode)
        {
            IsFocusMode = isFocusMode;
        }

        public bool IsFocusMode { get; }
    }
}


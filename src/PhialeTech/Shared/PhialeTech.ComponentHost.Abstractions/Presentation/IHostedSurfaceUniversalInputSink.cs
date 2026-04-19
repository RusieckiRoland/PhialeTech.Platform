using UniversalInput.Contracts;

namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public interface IHostedSurfaceUniversalInputSink
    {
        void HandleCommand(UniversalCommandEventArgs e);

        void HandleKey(UniversalKeyEventArgs e);

        void HandlePointer(UniversalPointerRoutedEventArgs e);

        void HandleFocus(UniversalFocusChangedEventArgs e);
    }
}

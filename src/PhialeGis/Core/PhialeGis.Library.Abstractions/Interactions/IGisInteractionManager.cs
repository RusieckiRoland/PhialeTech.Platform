using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Ui.Rendering;

namespace PhialeGis.Library.Abstractions.Interactions
{
    public interface IGisInteractionManager
    {
        event System.EventHandler<ViewportInteractionStatusChangedEventArgs> ViewportInteractionStatusChanged;

        void RegisterControl(IRenderingComposition composition);

        void RegisterControl(IEditorInteractive editor);

        void RegisterControl(object compositionObj);

        void UnregisterControl(IRenderingComposition composition);

        void UnregisterControl(IEditorInteractive editor);

        void UnregisterControl(object compositionObj);

        //  void ZoomPoint(double scale, Point point, ViewportManager viewport);

        //   void ZoomRect(double scale, Rect rect, ViewportManager viewport);

        void ApplyVisualWindow(double X, double Y, double Width, double Height);

        void InvalidateAll();

        void StartInteractiveAction(IInteractionAction action, object targetDraw, IEditorInteractive source);

        bool TryHandleInteractiveInput(string line, IEditorInteractive source);

        bool CancelInteractiveAction(IEditorInteractive source);

        bool TryHandleInteractivePointerDown(ActionPointerInput input);

        bool TryHandleInteractivePointerMove(ActionPointerInput input);

        bool TryHandleInteractivePointerUp(ActionPointerInput input);

        bool TryHandleInteractiveMenuCommand(object targetDraw, string commandId);

        bool TryConsumePendingContextMenu(object targetDraw, out ActionContextMenuPayload payload);

        bool TryTakeoverInteractiveSession(object targetDraw);

        bool TryGetViewportInteractionStatus(object targetDraw, out ViewportInteractionStatus status);

        void SetActionResultCommitter(IActionResultCommitter committer);

        void SetSnapService(ISnapService snapService);

        void UpdateCursorPosition(object targetDraw, double screenX, double screenY);

        void SetIdleCursor(CursorSpec cursor);
    }
}

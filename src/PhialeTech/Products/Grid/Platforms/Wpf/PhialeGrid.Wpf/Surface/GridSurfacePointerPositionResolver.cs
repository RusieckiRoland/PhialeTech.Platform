using System.Windows;
using System.Windows.Input;

namespace PhialeTech.PhialeGrid.Wpf.Surface
{
    internal interface IGridSurfacePointerPositionResolver
    {
        Point ResolvePosition(MouseEventArgs args, IInputElement relativeTo, DependencyObject originalSource);
    }

    internal sealed class GridSurfacePointerPositionResolver : IGridSurfacePointerPositionResolver
    {
        public static GridSurfacePointerPositionResolver Default { get; } = new GridSurfacePointerPositionResolver();

        public Point ResolvePosition(MouseEventArgs args, IInputElement relativeTo, DependencyObject originalSource)
        {
            return args.GetPosition(relativeTo);
        }
    }
}

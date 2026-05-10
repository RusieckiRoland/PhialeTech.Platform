using System.Windows;

namespace PhialeTech.PhialeGrid.Wpf.Surface
{
    public interface IGridRowDetailContentFactory
    {
        FrameworkElement CreateContent(GridRowDetailWpfContext context);
    }
}

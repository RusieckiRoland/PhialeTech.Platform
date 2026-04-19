using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PhialeTech.GisStudio.Mock.Wpf;

public partial class MapViewportView : UserControl
{
    public MapViewportView()
    {
        InitializeComponent();
    }

    private void HandleMapSurfacePointerDown(object sender, MouseButtonEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.CloseOverlayNavigationDrawer();
        }
    }
}

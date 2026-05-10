using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PhialeGis.ComponentSandboxWinUi.ViewModels;

namespace PhialeGis.ComponentSandboxWinUi.Windows
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow(MainPageViewModel vm)
        {
            InitializeComponent();
            Root.DataContext = vm;        // Window nie ma DataContext
        }
    }
}

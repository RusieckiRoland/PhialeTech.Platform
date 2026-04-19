using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using PhialeGis.ComponentSandboxUwp.ViewModels;

namespace PhialeGis.ComponentSandboxUwp
{
    /// <summary>
    /// Secondary window hosting another map instance.
    /// </summary>
    public sealed partial class MapWindowPage : Page
    {
        public MapWindowPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ViewModel jest przekazywany jako parametr nawigacji przez serwis okien.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var vm = e.Parameter as MainPageViewModel;
            if (vm != null)
                DataContext = vm;
        }
    }
}

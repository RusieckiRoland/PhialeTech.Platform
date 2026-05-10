using MahAppTestApp;
using Microsoft.Extensions.DependencyInjection;
using PhialeGis.ComponentSandbox.ViewModels;
using System;
using System.Windows;

namespace PhialeGis.ComponentSandbox.Views
{
    /// <summary>
    /// Interaction logic for MapWithDslEditorWindow.xaml
    /// </summary>
    public partial class MapWithDslEditorWindow : Window
    {
        public MapWithDslEditorWindow()
        {
            InitializeComponent();

            var app = Application.Current as App;

            if ((app == null) || (app.Services == null))
            {
                throw new InvalidOperationException("App.Services is not initialized.");
            }

            var vm = app.Services.GetRequiredService<MapWithDslEditorViewModel>();
            DataContext = vm;
        }
    }
}

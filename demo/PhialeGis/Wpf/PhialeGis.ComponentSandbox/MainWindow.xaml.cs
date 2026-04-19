using DryIoc;
using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using PhialeGis.ComponentSandbox.ViewModels;
using System;
using System.Windows;

namespace MahAppTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var app = Application.Current as App
                      ?? throw new InvalidOperationException("App not ready.");

            DataContext = app.Services.GetRequiredService<MainWindowViewModel>();
        }
    }
}
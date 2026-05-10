using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.Components.Wpf;
using WpfActiveLayerSelector = PhialeTech.ActiveLayerSelector.Wpf.Controls.ActiveLayerSelector;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeTech.ActiveLayerSelector.Wpf.Tests.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class ActiveLayerSelectorDemoIntegrationTests
    {
        [Test]
        public void MainWindow_WhenActiveLayerSelectorScenarioIsSelected_ShowsSelectorSurfaceAndSupportsShowMoreAndSetActive()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var viewModel = (DemoShellViewModel)window.DataContext;
                viewModel.SelectExample("active-layer-selector");
                FlushDispatcher(window.Dispatcher);

                var selector = (WpfActiveLayerSelector)window.FindName("DemoActiveLayerSelector");
                var grid = (WpfGrid)window.FindName("DemoGrid");

                Assert.Multiple(() =>
                {
                    Assert.That(selector, Is.Not.Null);
                    Assert.That(selector.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(grid.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(viewModel.ActiveLayerSelectorState, Is.Not.Null);
                    Assert.That(viewModel.ShowGridSurface, Is.False);
                    Assert.That(viewModel.ShowActiveLayerSelectorSurface, Is.True);
                    Assert.That(selector.ActualHeight, Is.LessThanOrEqualTo(72));
                });

                var expandButton = (Button)selector.FindName("ExpandCollapseButton");
                var popup = (Popup)selector.FindName("ExpandedPopup");
                Assert.That(expandButton, Is.Not.Null);
                Assert.That(popup, Is.Not.Null);
                Assert.That(popup.IsOpen, Is.False);
                expandButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(window.Dispatcher);

                var rowsControl = (ItemsControl)selector.FindName("VisibleRowsItemsControl");
                var showMoreButton = (Button)selector.FindName("ShowMoreButton");

                Assert.Multiple(() =>
                {
                    Assert.That(popup.IsOpen, Is.True);
                    Assert.That(rowsControl, Is.Not.Null);
                    Assert.That(rowsControl.Items.Count, Is.EqualTo(5));
                    Assert.That(showMoreButton, Is.Not.Null);
                    Assert.That(showMoreButton.Visibility, Is.EqualTo(Visibility.Visible));
                });

                showMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(window.Dispatcher);

                Assert.That(rowsControl.Items.Count, Is.GreaterThan(5));

                var setActiveButtons = FindVisualChildren<Button>(selector)
                    .Where(button => button.DataContext is PhialeTech.ActiveLayerSelector.ActiveLayerSelectorLayerViewModel layer && layer.CanSetActive)
                    .ToArray();
                if (setActiveButtons.Length == 0 && popup.Child != null)
                {
                    setActiveButtons = FindVisualChildren<Button>(popup.Child)
                        .Where(button => button.DataContext is PhialeTech.ActiveLayerSelector.ActiveLayerSelectorLayerViewModel layer && layer.CanSetActive)
                        .ToArray();
                }

                Assert.That(setActiveButtons.Length, Is.GreaterThanOrEqualTo(1));

                setActiveButtons[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(window.Dispatcher);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.ActiveLayerSelectorState.ActiveLayerId, Is.EqualTo("buildings"));
                    Assert.That(FindVisualChildren<TextBlock>(selector).Any(text => string.Equals(text.Text, "Buildings", StringComparison.Ordinal)), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MainWindow_WhenActiveLayerSelectorSidebarButtonIsClicked_OpensSelectorExample()
        {
            var window = new MainWindow();

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var viewModel = (DemoShellViewModel)window.DataContext;
                var componentButton = (Button)window.FindName("ActiveLayerSelectorComponentButton");

                Assert.That(componentButton, Is.Not.Null);
                componentButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                FlushDispatcher(window.Dispatcher);

                var selector = (WpfActiveLayerSelector)window.FindName("DemoActiveLayerSelector");
                var grid = (WpfGrid)window.FindName("DemoGrid");

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.SelectedExample, Is.Not.Null);
                    Assert.That(viewModel.SelectedExample.Id, Is.EqualTo("active-layer-selector"));
                    Assert.That(viewModel.ShowActiveLayerSelectorSurface, Is.True);
                    Assert.That(selector.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(grid.Visibility, Is.EqualTo(Visibility.Collapsed));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T typed)
                {
                    yield return typed;
                }

                foreach (var nested in FindVisualChildren<T>(child))
                {
                    yield return nested;
                }
            }
        }
    }
}


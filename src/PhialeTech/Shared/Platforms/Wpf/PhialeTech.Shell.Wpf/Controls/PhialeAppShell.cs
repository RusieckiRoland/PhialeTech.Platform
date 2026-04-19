using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using PhialeTech.Shell.Abstractions.Presentation;
using PhialeTech.Shell.Presentation;
using PhialeTech.Shell.Wpf.Input;
using UniversalInput.Contracts;

namespace PhialeTech.Shell.Wpf.Controls
{
    [TemplatePart(Name = PartNavigationHost, Type = typeof(Panel))]
    [TemplatePart(Name = PartNavigationRailHost, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartNavigationRailColumn, Type = typeof(ColumnDefinition))]
    [TemplatePart(Name = PartStatusHost, Type = typeof(Panel))]
    public class PhialeAppShell : ContentControl
    {
        private const string PartNavigationHost = "PART_NavigationHost";
        private const string PartNavigationRailHost = "NavigationRailHost";
        private const string PartNavigationRailColumn = "NavigationRailColumn";
        private const string NavigationRailWidthResourceKey = "GridLength.Shell.NavigationRail.Width";
        private const string NavigationRailCollapsedResourceKey = "GridLength.Shell.NavigationRail.Collapsed";
        private const string PartStatusHost = "PART_StatusHost";

        private Panel _navigationHost;
        private FrameworkElement _navigationRailHost;
        private ColumnDefinition _navigationRailColumn;
        private Panel _statusHost;

        public static readonly DependencyProperty ShellStateProperty =
            DependencyProperty.Register(
                nameof(ShellState),
                typeof(ApplicationShellState),
                typeof(PhialeAppShell),
                new FrameworkPropertyMetadata(null, OnShellStateChanged));

        public static readonly RoutedEvent CommandInvokedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(CommandInvoked),
                RoutingStrategy.Bubble,
                typeof(EventHandler<ShellCommandInvokedRoutedEventArgs>),
                typeof(PhialeAppShell));

        static PhialeAppShell()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PhialeAppShell), new FrameworkPropertyMetadata(typeof(PhialeAppShell)));
        }

        public PhialeAppShell()
        {
            AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnAnyButtonClick));
        }

        public event EventHandler<ShellCommandInvokedRoutedEventArgs> CommandInvoked
        {
            add { AddHandler(CommandInvokedEvent, value); }
            remove { RemoveHandler(CommandInvokedEvent, value); }
        }

        public ApplicationShellState ShellState
        {
            get { return (ApplicationShellState)GetValue(ShellStateProperty); }
            set { SetValue(ShellStateProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _navigationHost = GetTemplateChild(PartNavigationHost) as Panel;
            _navigationRailHost = GetTemplateChild(PartNavigationRailHost) as FrameworkElement;
            _navigationRailColumn = GetTemplateChild(PartNavigationRailColumn) as ColumnDefinition;
            _statusHost = GetTemplateChild(PartStatusHost) as Panel;
            RebuildShellVisuals();
        }

        private static void OnShellStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var shell = (PhialeAppShell)d;
            if (e.NewValue != null)
            {
                ApplicationShellStateValidator.Validate((ApplicationShellState)e.NewValue);
            }

            shell.RebuildShellVisuals();
        }

        private void RebuildShellVisuals()
        {
            RebuildNavigation();
            RebuildStatusBar();
        }

        private void RebuildNavigation()
        {
            if (_navigationHost == null)
            {
                return;
            }

            _navigationHost.Children.Clear();
            if (ShellState == null)
            {
                return;
            }

            foreach (var item in ShellState.NavigationItems)
            {
                _navigationHost.Children.Add(CreateNavigationButton(item));
            }

            ApplyNavigationRailVisibility();
        }

        private void ApplyNavigationRailVisibility()
        {
            if (_navigationRailHost == null || _navigationRailColumn == null)
            {
                return;
            }

            var hasNavigationItems = ShellState != null && ShellState.NavigationItems.Count > 0;
            _navigationRailHost.Visibility = hasNavigationItems ? Visibility.Visible : Visibility.Collapsed;
            _navigationRailColumn.Width = ResolveNavigationRailWidth(
                hasNavigationItems ? NavigationRailWidthResourceKey : NavigationRailCollapsedResourceKey);
        }

        private void RebuildStatusBar()
        {
            if (_statusHost == null)
            {
                return;
            }

            _statusHost.Children.Clear();
            if (ShellState == null)
            {
                return;
            }

            foreach (var item in ShellState.StatusItems)
            {
                _statusHost.Children.Add(CreateStatusHost(item));
            }
        }

        private Button CreateNavigationButton(ApplicationShellNavigationItem item)
        {
            var button = new Button
            {
                Tag = item,
                IsEnabled = item.IsEnabled,
            };
            button.SetResourceReference(FrameworkElement.StyleProperty, item.IsSelected ? "Shell.NavigationButtonStyle.Selected" : "Shell.NavigationButtonStyle");

            var content = new StackPanel();
            content.SetResourceReference(FrameworkElement.StyleProperty, "Shell.NavigationButtonContentPanelStyle");

            if (!string.IsNullOrWhiteSpace(item.IconGlyph))
            {
                var icon = new TextBlock
                {
                    Text = item.IconGlyph,
                };
                icon.SetResourceReference(FrameworkElement.StyleProperty, "Shell.NavigationButtonIconTextStyle");
                content.Children.Add(icon);
            }

            var text = new TextBlock
            {
                Text = item.DisplayText,
            };
            text.SetResourceReference(FrameworkElement.StyleProperty, "Shell.NavigationButtonTextStyle");
            content.Children.Add(text);

            button.Content = content;
            return button;
        }

        private FrameworkElement CreateStatusHost(ApplicationShellStatusItem item)
        {
            var panel = new StackPanel();
            panel.SetResourceReference(FrameworkElement.StyleProperty, "Shell.StatusItemHostStyle");

            var label = new TextBlock
            {
                Text = item.LabelText,
            };
            label.SetResourceReference(FrameworkElement.StyleProperty, "Shell.StatusItemLabelTextStyle");
            panel.Children.Add(label);

            var value = new TextBlock
            {
                Text = item.ValueText,
            };
            value.SetResourceReference(FrameworkElement.StyleProperty, "Shell.StatusItemValueTextStyle");
            panel.Children.Add(value);

            return panel;
        }

        private GridLength ResolveNavigationRailWidth(string resourceKey)
        {
            var resource = TryFindResource(resourceKey);
            if (!(resource is GridLength width))
            {
                throw new InvalidOperationException(
                    string.Format("Missing required shell grid length resource '{0}'.", resourceKey));
            }

            return width;
        }

        private void OnAnyButtonClick(object sender, RoutedEventArgs e)
        {
            var button = e.OriginalSource as Button;
            var item = button == null ? null : button.Tag as ApplicationShellNavigationItem;
            if (item == null)
            {
                return;
            }

            var payload = new UniversalCommandEventArgs("shell.navigate", false, false, false);
            payload.Arguments["itemId"] = item.ItemId;
            RaiseEvent(new ShellCommandInvokedRoutedEventArgs(CommandInvokedEvent, this, payload));
            e.Handled = true;
        }
    }
}

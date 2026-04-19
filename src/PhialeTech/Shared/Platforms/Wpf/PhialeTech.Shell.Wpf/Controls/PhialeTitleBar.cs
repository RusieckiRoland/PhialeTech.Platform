using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shell;
using PhialeTech.Shell.Abstractions.Presentation;
using PhialeTech.Shell.Presentation;
using PhialeTech.Shell.Wpf.Input;
using UniversalInput.Contracts;

namespace PhialeTech.Shell.Wpf.Controls
{
    [TemplatePart(Name = PartLeadingCommandsHost, Type = typeof(Panel))]
    [TemplatePart(Name = PartTrailingCommandsHost, Type = typeof(Panel))]
    public class PhialeTitleBar : Control
    {
        private const string PartLeadingCommandsHost = "PART_LeadingCommandsHost";
        private const string PartTrailingCommandsHost = "PART_TrailingCommandsHost";

        private Panel _leadingCommandsHost;
        private Panel _trailingCommandsHost;

        public static readonly DependencyProperty ShellStateProperty =
            DependencyProperty.Register(
                nameof(ShellState),
                typeof(ApplicationShellState),
                typeof(PhialeTitleBar),
                new FrameworkPropertyMetadata(null, OnShellStateChanged));

        public static readonly RoutedEvent CommandInvokedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(CommandInvoked),
                RoutingStrategy.Bubble,
                typeof(EventHandler<ShellCommandInvokedRoutedEventArgs>),
                typeof(PhialeTitleBar));

        static PhialeTitleBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PhialeTitleBar), new FrameworkPropertyMetadata(typeof(PhialeTitleBar)));
        }

        public PhialeTitleBar()
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
            _leadingCommandsHost = GetTemplateChild(PartLeadingCommandsHost) as Panel;
            _trailingCommandsHost = GetTemplateChild(PartTrailingCommandsHost) as Panel;
            RebuildCommands();
        }

        private static void OnShellStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var titleBar = (PhialeTitleBar)d;
            if (e.NewValue != null)
            {
                ApplicationShellStateValidator.Validate((ApplicationShellState)e.NewValue);
            }

            titleBar.RebuildCommands();
        }

        private void RebuildCommands()
        {
            RebuildCommandHost(_leadingCommandsHost, ApplicationShellCommandPlacement.Leading);
            RebuildCommandHost(_trailingCommandsHost, ApplicationShellCommandPlacement.Trailing);
        }

        private void RebuildCommandHost(Panel host, ApplicationShellCommandPlacement placement)
        {
            if (host == null)
            {
                return;
            }

            host.Children.Clear();
            if (ShellState == null)
            {
                return;
            }

            foreach (var command in ShellState.TitleBarCommands)
            {
                if (command.Placement != placement)
                {
                    continue;
                }

                host.Children.Add(CreateCommandButton(command));
            }
        }

        private Button CreateCommandButton(ApplicationShellCommandItem command)
        {
            var button = new Button
            {
                Content = string.IsNullOrWhiteSpace(command.DisplayText) ? command.CommandId : command.DisplayText,
                ToolTip = string.IsNullOrWhiteSpace(command.ToolTipText) ? null : command.ToolTipText,
                Tag = command,
                IsEnabled = command.IsEnabled,
            };
            button.SetResourceReference(FrameworkElement.StyleProperty, "Shell.TitleBarCommandButtonStyle");
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            return button;
        }

        private void OnAnyButtonClick(object sender, RoutedEventArgs e)
        {
            var button = e.OriginalSource as Button;
            if (button == null)
            {
                return;
            }

            if (button.Tag is string)
            {
                HandleSystemButtonClick((string)button.Tag);
                e.Handled = true;
                return;
            }

            var command = button.Tag as ApplicationShellCommandItem;
            if (command == null)
            {
                return;
            }

            var payload = new UniversalCommandEventArgs(command.CommandId, false, false, false);
            payload.Arguments["origin"] = "titleBar";
            RaiseEvent(new ShellCommandInvokedRoutedEventArgs(CommandInvokedEvent, this, payload));
            e.Handled = true;
        }

        private void HandleSystemButtonClick(string systemAction)
        {
            var window = Window.GetWindow(this);
            if (window == null)
            {
                throw new InvalidOperationException("PhialeTitleBar system buttons require an owning Window.");
            }

            switch (systemAction)
            {
                case "system:minimize":
                    SystemCommands.MinimizeWindow(window);
                    break;
                case "system:maximize":
                    if (window.WindowState == WindowState.Maximized)
                    {
                        SystemCommands.RestoreWindow(window);
                    }
                    else
                    {
                        SystemCommands.MaximizeWindow(window);
                    }

                    break;
                case "system:close":
                    SystemCommands.CloseWindow(window);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported title bar system action: " + systemAction);
            }
        }
    }
}

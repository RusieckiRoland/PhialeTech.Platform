using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeTech.ComponentHost.Abstractions.Presentation;
using PhialeTech.ComponentHost.Wpf.Hosting;
using PhialeTech.YamlApp.Wpf.Controls.Badges;
using PhialeTech.YamlApp.Wpf.Controls.Buttons;

namespace PhialeTech.Components.Wpf.Hosting
{
    public sealed class DemoViewHostedSurfaceFactory : IWpfHostedSurfaceFactory
    {
        public bool CanCreate(IHostedSurfaceRequest request)
        {
            return request != null &&
                request.SurfaceKind == HostedSurfaceKind.View &&
                string.Equals(request.ContentKey, "demo.view.hosted-modal", System.StringComparison.OrdinalIgnoreCase);
        }

        public FrameworkElement CreateContent(IHostedSurfaceRequest request, IHostedSurfaceManager manager)
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel { Margin = new Thickness(24, 22, 24, 16) };
            var meta = new WrapPanel();
            meta.Children.Add(new TextBlock
            {
                Text = "Hosted view",
                Margin = new Thickness(0, 0, 8, 0),
                FontFamily = new FontFamily("Bahnschrift SemiBold"),
                FontSize = 12,
                Foreground = (Brush)Application.Current.FindResource("Brush.Text.Secondary"),
                VerticalAlignment = VerticalAlignment.Center,
            });
            meta.Children.Add(new YamlBadge
            {
                Text = request.PresentationMode == HostedPresentationMode.OverlaySheet ? "Overlay sheet" : "Compact modal",
                Tone = PhialeTech.YamlApp.Abstractions.Enums.BadgeTone.Accent,
                Size = PhialeTech.YamlApp.Abstractions.Enums.BadgeSize.Compact,
            });
            header.Children.Add(meta);
            header.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 0),
                Text = string.IsNullOrWhiteSpace(request.Title) ? "Reusable hosted WPF view" : request.Title,
                FontFamily = new FontFamily("Bahnschrift SemiBold"),
                FontSize = 20,
                Foreground = (Brush)Application.Current.FindResource("Brush.Text.Primary"),
            });
            header.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = "This content is a plain WPF view shown through the same modal host infrastructure as YamlApp forms.",
                FontFamily = (FontFamily)Application.Current.FindResource("Text.Support.FontFamily"),
                FontSize = (double)Application.Current.FindResource("Text.Support.FontSize"),
                Foreground = (Brush)Application.Current.FindResource("Brush.Text.Secondary"),
                TextWrapping = TextWrapping.Wrap,
            });
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var separator = new Border
            {
                Height = 1,
                Background = (Brush)Application.Current.FindResource("Brush.Border.Subtle"),
            };
            Grid.SetRow(separator, 1);
            root.Children.Add(separator);

            var body = new StackPanel { Margin = new Thickness(24, 18, 24, 18) };
            body.Children.Add(new TextBlock
            {
                Text = "Workflow note",
                FontFamily = new FontFamily("Bahnschrift SemiBold"),
                FontSize = 14,
                Foreground = (Brush)Application.Current.FindResource("Brush.Text.Primary"),
            });
            body.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Text = "OverlaySheet closes the drawer before taking over the workspace. CompactModal leaves the drawer visible under scrim but blocks the whole app surface.",
                FontSize = 13,
                Foreground = (Brush)Application.Current.FindResource("Brush.Text.Secondary"),
                TextWrapping = TextWrapping.Wrap,
            });
            body.Children.Add(new YamlBadge
            {
                Margin = new Thickness(0, 16, 0, 0),
                Text = "UniversalInput-driven host",
                Tone = PhialeTech.YamlApp.Abstractions.Enums.BadgeTone.Warning,
                Size = PhialeTech.YamlApp.Abstractions.Enums.BadgeSize.Regular,
                IconKey = "info",
            });
            Grid.SetRow(body, 2);
            root.Children.Add(body);

            var actions = new WrapPanel { Margin = new Thickness(24, 0, 24, 24) };
            actions.Children.Add(CreateActionButton("Cancel", "cancel", null, () => manager.TryCancelCurrent("demo.view.cancel")));
            actions.Children.Add(CreateActionButton("Continue", "ok", PhialeTech.YamlApp.Abstractions.Enums.ButtonTone.Primary, () => manager.TryConfirmCurrent("demo.view.confirm", request.ContentKey)));
            Grid.SetRow(actions, 3);
            root.Children.Add(actions);

            return root;
        }

        private static YamlButton CreateActionButton(string label, string iconKey, PhialeTech.YamlApp.Abstractions.Enums.ButtonTone? tone, Action action)
        {
            var button = new YamlButton
            {
                Margin = new Thickness(0, 0, 8, 0),
                Content = label,
                IconKey = iconKey,
                Variant = PhialeTech.YamlApp.Abstractions.Enums.ButtonVariant.ActionStrip,
                ToolTip = label,
                CommandId = "demo." + label.ToLowerInvariant(),
            };

            if (tone.HasValue)
            {
                button.Tone = tone.Value;
            }

            button.Invoked += (sender, args) => action();
            return button;
        }
    }
}

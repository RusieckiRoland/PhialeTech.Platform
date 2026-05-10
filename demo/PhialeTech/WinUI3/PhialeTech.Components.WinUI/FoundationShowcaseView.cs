using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.ViewModels;
using System.Collections.Generic;
using Windows.UI.Text;

namespace PhialeTech.Components.WinUI
{
    internal sealed class FoundationShowcaseView : UserControl
    {
        private readonly DemoShellViewModel _viewModel;
        private readonly ScrollViewer _scrollViewer;

        public FoundationShowcaseView(DemoShellViewModel viewModel)
        {
            _viewModel = viewModel;
            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            Content = _scrollViewer;
            Refresh();
        }

        public void Refresh()
        {
            _scrollViewer.Content = BuildContent();
        }

        private UIElement BuildContent()
        {
            var root = new StackPanel
            {
                Margin = new Thickness(0, 0, 8, 0),
                Spacing = 16,
            };

            root.Children.Add(BuildIntroCard());

            var main = new Grid();
            main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.85, GridUnitType.Star) });

            main.Children.Add(BuildTypographyCard());
            var side = new StackPanel { Spacing = 16 };
            side.Children.Add(BuildColorCard());
            side.Children.Add(BuildRhythmCard());
            Grid.SetColumn(side, 2);
            main.Children.Add(side);

            root.Children.Add(main);
            return root;
        }

        private UIElement BuildIntroCard()
        {
            var panel = new StackPanel();
            panel.Children.Add(CreateSectionTitle(_viewModel.FoundationsIntroTitle));
            panel.Children.Add(CreateBodyText(_viewModel.FoundationsIntroDescription));

            var highlights = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(0, 14, 0, 0),
            };

            foreach (var highlight in _viewModel.FoundationsHighlights)
            {
                highlights.Children.Add(CreateHighlightChip(highlight));
            }

            panel.Children.Add(highlights);

            panel.Children.Add(CreateSubsectionTitle("Workspace playground", new Thickness(0, 10, 0, 8)));
            panel.Children.Add(BuildWorkspacePreview());
            return CreateCard(panel);
        }

        private UIElement BuildTypographyCard()
        {
            var panel = new StackPanel();
            panel.Children.Add(CreateSectionTitle(_viewModel.FoundationsTypographyTitle));
            panel.Children.Add(CreateBodyText(_viewModel.FoundationsTypographyDescription, new Thickness(0, 0, 0, 14)));

            foreach (var token in _viewModel.FoundationsTypographyTokens)
            {
                panel.Children.Add(BuildTypographyToken(token));
            }

            return CreateCard(panel);
        }

        private UIElement BuildColorCard()
        {
            var panel = new StackPanel();
            panel.Children.Add(CreateSectionTitle(_viewModel.FoundationsColorsTitle));
            panel.Children.Add(CreateBodyText(_viewModel.FoundationsColorsDescription, new Thickness(0, 0, 0, 14)));

            panel.Children.Add(CreateSubsectionTitle(_viewModel.FoundationsTextColorsTitle));
            AddColorTokens(panel, _viewModel.FoundationsTextColorTokens);
            panel.Children.Add(CreateSubsectionTitle(_viewModel.FoundationsSurfaceColorsTitle, new Thickness(0, 8, 0, 0)));
            AddColorTokens(panel, _viewModel.FoundationsSurfaceTokens);
            panel.Children.Add(CreateSubsectionTitle(_viewModel.FoundationsAccentColorsTitle, new Thickness(0, 8, 0, 0)));
            AddColorTokens(panel, _viewModel.FoundationsAccentTokens);

            return CreateCard(panel);
        }

        private UIElement BuildRhythmCard()
        {
            var panel = new StackPanel();
            panel.Children.Add(CreateSectionTitle(_viewModel.FoundationsRhythmTitle));
            panel.Children.Add(CreateBodyText(_viewModel.FoundationsRhythmDescription, new Thickness(0, 0, 0, 14)));
            panel.Children.Add(CreateSubsectionTitle(_viewModel.FoundationsShapesTitle));
            AddMeasureTokens(panel, _viewModel.FoundationsShapeTokens);
            panel.Children.Add(CreateSubsectionTitle(_viewModel.FoundationsSpacingTitle, new Thickness(0, 8, 0, 0)));
            AddMeasureTokens(panel, _viewModel.FoundationsSpacingTokens);
            return CreateCard(panel);
        }

        private UIElement BuildTypographyToken(DemoFoundationTypographyTokenViewModel token)
        {
            var card = new Border
            {
                Background = ResourceBrush("DemoHintBackgroundBrush"),
                BorderBrush = ResourceBrush("DemoInputBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 12),
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var meta = new StackPanel();
            meta.Children.Add(CreateTokenText(token.TokenName));
            meta.Children.Add(CreateStrongText(token.Role));
            meta.Children.Add(CreateSmallText(token.Usage));
            grid.Children.Add(meta);

            var sampleCard = new Border
            {
                Background = ResourceBrush("DemoPanelBackgroundBrush"),
                BorderBrush = ResourceBrush("DemoPanelBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = token.SampleText,
                            FontFamily = new FontFamily(token.FontFamilyName),
                            FontSize = token.FontSize,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = ResourceBrush("DemoPrimaryTextBrush"),
                            TextWrapping = TextWrapping.WrapWholeWords,
                        },
                        CreateSmallText(token.StyleSummary, new Thickness(0, 8, 0, 0)),
                    },
                },
            };
            Grid.SetColumn(sampleCard, 1);
            grid.Children.Add(sampleCard);
            card.Child = grid;
            return card;
        }

        private void AddColorTokens(StackPanel panel, IReadOnlyList<DemoFoundationColorTokenViewModel> tokens)
        {
            foreach (var token in tokens)
            {
                panel.Children.Add(BuildColorToken(token));
            }
        }

        private UIElement BuildColorToken(DemoFoundationColorTokenViewModel token)
        {
            var card = new Border
            {
                Background = ResourceBrush("DemoHintBackgroundBrush"),
                BorderBrush = ResourceBrush("DemoInputBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });

            var meta = new StackPanel();
            meta.Children.Add(CreateTokenText(token.TokenName));
            meta.Children.Add(CreateSmallText(token.Usage));
            grid.Children.Add(meta);
            var day = BuildSwatch(_viewModel.FoundationsDayLabel, token.DayHex);
            Grid.SetColumn(day, 1);
            grid.Children.Add(day);
            var night = BuildSwatch(_viewModel.FoundationsNightLabel, token.NightHex);
            Grid.SetColumn(night, 2);
            grid.Children.Add(night);
            card.Child = grid;
            return card;
        }

        private FrameworkElement BuildSwatch(string label, string hex)
        {
            return new StackPanel
            {
                Margin = new Thickness(8, 0, 0, 0),
                Children =
                {
                    CreateSmallText(label),
                    new Border
                    {
                        Height = 28,
                        CornerRadius = new CornerRadius(5),
                        Background = CreateBrush(hex),
                        BorderBrush = ResourceBrush("DemoInputBorderBrush"),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(0, 4, 0, 4),
                    },
                    CreateSmallText(hex),
                },
            };
        }

        private void AddMeasureTokens(StackPanel panel, IReadOnlyList<DemoFoundationMeasureTokenViewModel> tokens)
        {
            foreach (var token in tokens)
            {
                panel.Children.Add(CreateMeasureToken(token));
            }
        }

        private UIElement CreateMeasureToken(DemoFoundationMeasureTokenViewModel token)
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 10),
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(new StackPanel
            {
                Children =
                {
                    CreateTokenText(token.TokenName),
                    CreateSmallText(token.Usage),
                },
            });
            var value = new Border
            {
                Background = ResourceBrush("DemoTokenBackgroundBrush"),
                BorderBrush = ResourceBrush("DemoTokenBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 3, 8, 3),
                Child = CreateSmallText(token.Value),
            };
            Grid.SetColumn(value, 1);
            grid.Children.Add(value);
            return grid;
        }

        private UIElement BuildWorkspacePreview()
        {
            return new Border
            {
                Background = ResourceBrush("DemoHintBackgroundBrush"),
                BorderBrush = ResourceBrush("DemoInputBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        CreateTokenChip("Category", "Count"),
                        CreateTokenChip("Area [m2]", "Sum"),
                        CreateTokenChip("Count", "Category530"),
                    },
                },
            };
        }

        private UIElement CreateTokenChip(string title, string value)
        {
            return new Border
            {
                Background = ResourceBrush("DemoPanelBackgroundBrush"),
                BorderBrush = ResourceBrush("DemoTokenBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 3, 8, 3),
                Child = new StackPanel
                {
                    Children =
                    {
                        CreateTokenText(title),
                        CreateSmallText(value),
                    },
                },
            };
        }

        private UIElement CreateHighlightChip(string text)
        {
            return new Border
            {
                Padding = new Thickness(8, 3, 8, 3),
                Background = ResourceBrush("DemoTokenBackgroundBrush"),
                BorderBrush = ResourceBrush("DemoTokenBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    Foreground = ResourceBrush("DemoTokenTextBrush"),
                    TextWrapping = TextWrapping.NoWrap,
                },
            };
        }

        private Border CreateCard(UIElement child)
        {
            return new Border
            {
                Background = ResourceBrush("DemoPanelBackgroundBrush"),
                BorderBrush = ResourceBrush("DemoPanelBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                Child = child,
            };
        }

        private TextBlock CreateSectionTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Bahnschrift SemiBold"),
                FontSize = 18,
                Foreground = ResourceBrush("DemoPrimaryTextBrush"),
                Margin = new Thickness(0, 0, 0, 14),
            };
        }

        private TextBlock CreateSubsectionTitle(string text, Thickness? margin = null)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Bahnschrift SemiBold"),
                FontSize = 13,
                Foreground = ResourceBrush("DemoPrimaryTextBrush"),
                Margin = margin ?? new Thickness(0, 0, 0, 8),
            };
        }

        private TextBlock CreateStrongText(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Bahnschrift SemiBold"),
                FontSize = 14,
                Foreground = ResourceBrush("DemoPrimaryTextBrush"),
                TextWrapping = TextWrapping.WrapWholeWords,
            };
        }

        private TextBlock CreateBodyText(string text, Thickness? margin = null)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 14,
                Foreground = ResourceBrush("DemoSecondaryTextBrush"),
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = margin ?? new Thickness(0),
            };
        }

        private TextBlock CreateTokenText(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Bahnschrift SemiBold"),
                FontSize = 11,
                Foreground = ResourceBrush("DemoHeaderTextBrush"),
                TextWrapping = TextWrapping.WrapWholeWords,
            };
        }

        private TextBlock CreateSmallText(string text, Thickness? margin = null)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 12,
                Foreground = ResourceBrush("DemoSecondaryTextBrush"),
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = margin ?? new Thickness(0),
            };
        }

        private static SolidColorBrush ResourceBrush(string key)
        {
            return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is SolidColorBrush brush
                ? brush
                : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        private static SolidColorBrush CreateBrush(string hex)
        {
            var color = new Windows.UI.Color
            {
                A = 255,
                R = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                G = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                B = byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber),
            };
            return new SolidColorBrush(color);
        }
    }
}

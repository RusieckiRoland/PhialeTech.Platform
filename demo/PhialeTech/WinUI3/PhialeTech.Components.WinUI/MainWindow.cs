using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Text;

namespace PhialeTech.Components.WinUI
{
    public sealed class MainWindow : Window
    {
        private readonly DemoApplicationServices _applicationServices;
        private readonly DemoShellViewModel _viewModel;
        private readonly TextBlock _appTitleText;
        private readonly TextBlock _appSubtitleText;
        private readonly TextBlock _searchLabelText;
        private readonly TextBox _searchBox;
        private readonly TextBlock _componentsLabelText;
        private readonly StackPanel _drawerGroupPanel;
        private readonly TextBlock _languageLabelText;
        private readonly ComboBox _languageComboBox;
        private readonly ScrollViewer _contentScroller;
        private readonly StackPanel _contentPanel;
        private WebHostShowcaseView _webHostShowcaseView;
        private PdfViewerShowcaseView _pdfViewerShowcaseView;
        private ReportDesignerShowcaseView _reportDesignerShowcaseView;
        private bool _isRefreshing;
        private bool _refreshQueued;

        public MainWindow(DemoApplicationServices applicationServices)
        {
            _applicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
            _viewModel = new DemoShellViewModel("WinUI", definitionManager: _applicationServices.DefinitionManager);
            _viewModel.PropertyChanged += HandleViewModelPropertyChanged;

            _appTitleText = CreateTextBlock(string.Empty, 28, FontWeights.SemiBold, "#17212B");
            _appSubtitleText = CreateTextBlock(string.Empty, 13, FontWeights.Normal, "#52606D");
            _searchLabelText = CreateTextBlock(string.Empty, 14, FontWeights.SemiBold, "#3A4754");
            _searchBox = new TextBox
            {
                Background = CreateBrush("#FBFBF9"),
                BorderBrush = CreateBrush("#D5D0C5"),
                Padding = new Thickness(12, 9, 12, 9),
            };
            _searchBox.TextChanged += HandleSearchTextChanged;

            _componentsLabelText = CreateTextBlock(string.Empty, 14, FontWeights.SemiBold, "#3A4754");
            _drawerGroupPanel = new StackPanel
            {
                Spacing = 12,
            };
            _languageLabelText = CreateTextBlock(string.Empty, 14, FontWeights.SemiBold, "#3A4754");
            _languageComboBox = new ComboBox();
            _languageComboBox.SelectionChanged += HandleLanguageSelectionChanged;

            _contentPanel = new StackPanel();
            _contentScroller = new ScrollViewer
            {
                Content = _contentPanel,
            };

            Content = BuildLayout();
            Closed += HandleClosed;
            RefreshUi();
        }

        private void HandleClosed(object sender, WindowEventArgs args)
        {
            _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            _webHostShowcaseView?.Dispose();
            _webHostShowcaseView = null;
            _pdfViewerShowcaseView?.Dispose();
            _pdfViewerShowcaseView = null;
            _reportDesignerShowcaseView?.Dispose();
            _reportDesignerShowcaseView = null;
        }

        private UIElement BuildLayout()
        {
            var root = new Grid
            {
                Background = CreateBrush("#F3F1EC"),
            };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(296) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var sidebar = new Border
            {
                Background = CreateBrush("#ECE8DE"),
                BorderBrush = CreateBrush("#D8D2C4"),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Child = BuildSidebar(),
            };

            var contentHost = new Grid
            {
                Margin = new Thickness(30, 26, 30, 26),
                Children = { _contentScroller },
            };
            Grid.SetColumn(contentHost, 1);

            root.Children.Add(sidebar);
            root.Children.Add(contentHost);
            return root;
        }

        private UIElement BuildSidebar()
        {
            var layout = new Grid
            {
                Margin = new Thickness(18),
            };
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var brand = new StackPanel();
            brand.Children.Add(_appTitleText);
            _appSubtitleText.Margin = new Thickness(0, 4, 0, 0);
            brand.Children.Add(_appSubtitleText);

            var searchSection = new StackPanel { Margin = new Thickness(0, 28, 0, 0) };
            searchSection.Children.Add(_searchLabelText);
            _searchBox.Margin = new Thickness(0, 8, 0, 0);
            searchSection.Children.Add(_searchBox);
            Grid.SetRow(searchSection, 1);

            var componentSection = new StackPanel { Margin = new Thickness(0, 26, 0, 0) };
            componentSection.Children.Add(_componentsLabelText);
            _drawerGroupPanel.Margin = new Thickness(0, 8, 0, 0);
            componentSection.Children.Add(_drawerGroupPanel);
            Grid.SetRow(componentSection, 2);

            var languageSection = new StackPanel();
            languageSection.Children.Add(_languageLabelText);
            _languageComboBox.Margin = new Thickness(0, 8, 0, 0);
            languageSection.Children.Add(_languageComboBox);
            Grid.SetRow(languageSection, 4);

            layout.Children.Add(brand);
            layout.Children.Add(searchSection);
            layout.Children.Add(componentSection);
            layout.Children.Add(languageSection);
            return layout;
        }

        private void RefreshUi()
        {
            _isRefreshing = true;

            _appTitleText.Text = _viewModel.AppTitle;
            _appSubtitleText.Text = _viewModel.AppSubtitle;
            _searchLabelText.Text = _viewModel.SearchPlaceholder;
            if (_searchBox.Text != _viewModel.SearchText)
            {
                _searchBox.Text = _viewModel.SearchText ?? string.Empty;
            }

            _componentsLabelText.Text = _viewModel.ComponentsTitle;
            RefreshDrawerGroupCards();
            _languageLabelText.Text = _viewModel.LanguageLabelText;

            _languageComboBox.ItemsSource = _viewModel.LanguageOptions;
            _languageComboBox.DisplayMemberPath = nameof(DemoLanguageOption.DisplayName);
            if (!ReferenceEquals(_languageComboBox.SelectedItem, _viewModel.SelectedLanguage))
            {
                _languageComboBox.SelectedItem = _viewModel.SelectedLanguage;
            }

            Title = _viewModel.AppTitle;
            RenderContent();

            _isRefreshing = false;
        }

        private void RenderContent()
        {
            _contentPanel.Children.Clear();

            if (_viewModel.IsOverviewVisible)
            {
                RenderOverview();
                return;
            }

            RenderDetail();
        }

        private void RenderOverview()
        {
            _contentPanel.Children.Add(CreateTextBlock(_viewModel.OverviewTitle, 30, FontWeights.SemiBold, "#17212B"));
            _contentPanel.Children.Add(CreateTextBlock(_viewModel.OverviewSubtitle, 15, FontWeights.Normal, "#52606D", new Thickness(0, 8, 0, 26)));

            if (_viewModel.HasNoOverviewResults)
            {
                _contentPanel.Children.Add(CreateTextBlock(_viewModel.EmptySearchText, 14, FontWeights.Normal, "#52606D", new Thickness(0, 0, 0, 22)));
            }

            foreach (var section in _viewModel.VisibleSections)
            {
                var sectionPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 28) };
                sectionPanel.Children.Add(CreateTextBlock(section.Title, 18, FontWeights.SemiBold, "#1F2933", new Thickness(0, 0, 0, 14)));
                sectionPanel.Children.Add(BuildCardRows(section.Rows));
                _contentPanel.Children.Add(sectionPanel);
            }
        }

        private void RenderDetail()
        {
            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var backButton = new Button
            {
                Content = _viewModel.BackToOverviewText,
                Padding = new Thickness(14, 8, 14, 8),
                Background = CreateBrush("#FBFBF9"),
                BorderBrush = CreateBrush("#D5D0C5"),
                BorderThickness = new Thickness(1),
            };
            backButton.Click += (_, _) => _viewModel.ShowOverview();

            var platformBadge = new Border
            {
                Background = CreateBrush("#E6F4F1"),
                BorderBrush = CreateBrush("#B8DDD7"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                Child = CreateTextBlock(_viewModel.PlatformBadgeText, 13, FontWeights.SemiBold, "#0F766E"),
            };
            Grid.SetColumn(platformBadge, 2);

            header.Children.Add(backButton);
            header.Children.Add(platformBadge);

            var titleBlock = CreateTextBlock(_viewModel.DetailHeadline, 34, FontWeights.SemiBold, "#17212B", new Thickness(0, 26, 0, 0));
            var descriptionBlock = CreateTextBlock(_viewModel.SelectedExampleDescription, 15, FontWeights.Normal, "#52606D", new Thickness(0, 8, 0, 20));
            descriptionBlock.TextWrapping = TextWrapping.WrapWholeWords;

            var pivot = new Pivot();
            pivot.SelectedIndex = _viewModel.SelectedTabIndex;
            pivot.SelectionChanged += (sender, args) =>
            {
                if (sender is Pivot control)
                {
                    _viewModel.SelectedTabIndex = control.SelectedIndex;
                }
            };

            pivot.Items.Add(new PivotItem
            {
                Header = _viewModel.DemoTabText,
                Content = BuildDemoTab(),
            });
            pivot.Items.Add(new PivotItem
            {
                Header = _viewModel.CodeTabText,
                Content = BuildCodeTab(),
            });

            _contentPanel.Children.Add(header);
            _contentPanel.Children.Add(titleBlock);
            _contentPanel.Children.Add(descriptionBlock);
            _contentPanel.Children.Add(pivot);
        }

        private UIElement BuildDemoTab()
        {
            if (_viewModel.ShowWebComponentsSurface)
            {
                if (_viewModel.SelectedTabIndex != 0)
                {
                    return new Grid();
                }

                if (_viewModel.ShowWebHostSurface)
                {
                    return BuildWebHostDemoTab();
                }

                if (_viewModel.ShowPdfViewerSurface)
                {
                    return BuildPdfViewerDemoTab();
                }

                return BuildReportDesignerDemoTab();
            }

            var panel = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };

            var groupingBar = new Border
            {
                Background = CreateBrush("#F6FAF9"),
                BorderBrush = CreateBrush("#D5E5E0"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Child = CreateTextBlock(_viewModel.GroupingBarText, 14, FontWeights.SemiBold, "#2F5D57"),
            };
            panel.Children.Add(groupingBar);

            var body = new Grid { Margin = new Thickness(0, 18, 0, 0) };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(176) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var metricPanel = new StackPanel();
            metricPanel.Children.Add(CreateTextBlock(_viewModel.MetricDeckTitle, 18, FontWeights.SemiBold, "#17212B", new Thickness(0, 0, 0, 14)));
            foreach (var metric in _viewModel.MetricCards)
            {
                var metricCard = new Border
                {
                    Background = CreateBrush(metric.AccentHex),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(18, 16, 18, 16),
                    Margin = new Thickness(0, 0, 0, 14),
                };

                var metricGrid = new Grid();
                metricGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                metricGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var metricTitle = CreateTextBlock(metric.Title, 18, FontWeights.SemiBold, "#FFFFFF");
                var metricDelta = CreateTextBlock(metric.DeltaText, 16, FontWeights.SemiBold, "#FFFFFF", new Thickness(0, 22, 0, 0));
                metricDelta.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetRow(metricDelta, 1);

                metricGrid.Children.Add(metricTitle);
                metricGrid.Children.Add(metricDelta);
                metricCard.Child = metricGrid;
                metricPanel.Children.Add(metricCard);
            }

            var previewPanel = new StackPanel();
            previewPanel.Children.Add(CreateTextBlock(_viewModel.PreviewUsageTitle, 18, FontWeights.SemiBold, "#17212B"));
            var previewHint = CreateTextBlock(_viewModel.PreviewHintText, 14, FontWeights.Normal, "#52606D", new Thickness(0, 8, 0, 16));
            previewHint.TextWrapping = TextWrapping.WrapWholeWords;
            previewPanel.Children.Add(previewHint);
            previewPanel.Children.Add(BuildPreviewSurface());

            body.Children.Add(metricPanel);
            Grid.SetColumn(previewPanel, 2);
            body.Children.Add(previewPanel);

            panel.Children.Add(body);
            return new ScrollViewer { Content = panel };
        }

        private UIElement BuildWebHostDemoTab()
        {
            _webHostShowcaseView ??= new WebHostShowcaseView();

            return new Border
            {
                Margin = new Thickness(0, 20, 0, 0),
                Background = CreateBrush("#FBFBF9"),
                BorderBrush = CreateBrush("#DDD8CF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                Child = _webHostShowcaseView,
            };
        }

        private UIElement BuildPdfViewerDemoTab()
        {
            _pdfViewerShowcaseView ??= new PdfViewerShowcaseView(this);

            return new Border
            {
                Margin = new Thickness(0, 20, 0, 0),
                Background = CreateBrush("#FBFBF9"),
                BorderBrush = CreateBrush("#DDD8CF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                Child = _pdfViewerShowcaseView,
            };
        }

        private UIElement BuildReportDesignerDemoTab()
        {
            _reportDesignerShowcaseView ??= new ReportDesignerShowcaseView(_viewModel.LanguageCode, ResolveReportDesignerTheme());
            _ = _reportDesignerShowcaseView.ApplyEnvironmentAsync(_viewModel.LanguageCode, ResolveReportDesignerTheme());

            return new Border
            {
                Margin = new Thickness(0, 20, 0, 0),
                Background = CreateBrush("#FBFBF9"),
                BorderBrush = CreateBrush("#DDD8CF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                Child = _reportDesignerShowcaseView,
            };
        }

        private UIElement BuildCodeTab()
        {
            var panel = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };
            panel.Children.Add(CreateTextBlock(_viewModel.FileLabelText, 14, FontWeights.SemiBold, "#3A4754"));

            var codeSelector = new ComboBox
            {
                Width = 260,
                Margin = new Thickness(0, 10, 0, 16),
                ItemsSource = _viewModel.AvailableCodeFiles,
                DisplayMemberPath = nameof(DemoCodeFileViewModel.FileName),
                SelectedItem = _viewModel.SelectedCodeFile,
            };
            codeSelector.SelectionChanged += (_, _) =>
            {
                if (!_isRefreshing)
                {
                    _viewModel.SelectedCodeFile = codeSelector.SelectedItem as DemoCodeFileViewModel;
                }
            };
            panel.Children.Add(codeSelector);

            var codeEditor = new TextBox
            {
                Text = _viewModel.SourceCodeText,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = CreateBrush("#F4F7FB"),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Padding = new Thickness(18),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
            };

            panel.Children.Add(new Border
            {
                Background = CreateBrush("#131A22"),
                BorderBrush = CreateBrush("#2E3642"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Child = codeEditor,
            });

            return panel;
        }

        private UIElement BuildPreviewSurface()
        {
            var container = new Border
            {
                Background = CreateBrush("#FBFBF9"),
                BorderBrush = CreateBrush("#DDD8CF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
            };

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            layout.Children.Add(CreatePreviewHeader());
            var rowsHost = new StackPanel();
            foreach (var row in _viewModel.PreviewRows)
            {
                rowsHost.Children.Add(CreatePreviewRow(row));
            }

            var scrollViewer = new ScrollViewer
            {
                Content = rowsHost,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            };
            Grid.SetRow(scrollViewer, 1);
            layout.Children.Add(scrollViewer);

            container.Child = layout;
            return container;
        }

        private UIElement CreatePreviewHeader()
        {
            var header = CreatePreviewGrid(new Thickness(18, 16, 18, 12));
            header.Children.Add(CreatePreviewCell(_viewModel.CategoryColumnText, 0, "#445565", FontWeights.SemiBold));
            header.Children.Add(CreatePreviewCell(_viewModel.ObjectNameColumnText, 1, "#445565", FontWeights.SemiBold));
            header.Children.Add(CreatePreviewCell(_viewModel.MunicipalityColumnText, 2, "#445565", FontWeights.SemiBold));
            header.Children.Add(CreatePreviewCell(_viewModel.StatusColumnText, 3, "#445565", FontWeights.SemiBold));
            header.Children.Add(CreatePreviewCell(_viewModel.AreaColumnText, 4, "#445565", FontWeights.SemiBold));
            header.Children.Add(CreatePreviewCell(_viewModel.LastInspectionColumnText, 5, "#445565", FontWeights.SemiBold));
            return header;
        }

        private UIElement CreatePreviewRow(DemoGisPreviewRowViewModel row)
        {
            var border = new Border
            {
                BorderBrush = CreateBrush("#EEE8DE"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 14, 18, 14),
            };

            var grid = CreatePreviewGrid(new Thickness(0));
            grid.Children.Add(CreatePreviewCell(row.Category, 0, "#17212B", FontWeights.Normal, true));
            grid.Children.Add(CreatePreviewCell(row.ObjectName, 1, "#17212B", FontWeights.Normal));
            grid.Children.Add(CreatePreviewCell(row.Municipality, 2, "#17212B", FontWeights.Normal));
            grid.Children.Add(CreatePreviewCell(row.Status, 3, "#17212B", FontWeights.Normal));
            grid.Children.Add(CreatePreviewCell(row.AreaDisplay, 4, row.StatusForegroundHex, FontWeights.Normal));
            grid.Children.Add(CreatePreviewCell(row.InspectionDisplay, 5, row.InspectionForegroundHex, FontWeights.Normal));
            border.Child = grid;
            return border;
        }

        private Grid CreatePreviewGrid(Thickness margin)
        {
            var grid = new Grid { Margin = margin };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.8, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            return grid;
        }

        private TextBlock CreatePreviewCell(string text, int columnIndex, string foregroundHex, FontWeight fontWeight, bool wrap = false)
        {
            var cell = CreateTextBlock(text, 14, fontWeight, foregroundHex);
            cell.TextWrapping = wrap ? TextWrapping.WrapWholeWords : TextWrapping.NoWrap;
            Grid.SetColumn(cell, columnIndex);
            return cell;
        }

        private UIElement BuildCardRows(IReadOnlyList<DemoSectionRowViewModel> rows)
        {
            var panel = new StackPanel();
            foreach (var row in rows)
            {
                var rowPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 16),
                };

                foreach (var example in row.Examples)
                {
                    rowPanel.Children.Add(CreateExampleCard(example));
                }

                panel.Children.Add(rowPanel);
            }

            return panel;
        }

        private FrameworkElement CreateExampleCard(DemoExampleCardViewModel example)
        {
            var button = new Button
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 16, 16),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Tag = example.Id,
            };
            button.Click += (_, _) => _viewModel.SelectExample((string)button.Tag);

            var card = new Border
            {
                Width = 330,
                Height = 168,
                Background = CreateBrush("#FBFBF9"),
                BorderBrush = CreateBrush("#DDD8CF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
            };

            var layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            layout.Children.Add(new Border
            {
                Background = CreateBrush(example.AccentHex),
                CornerRadius = new CornerRadius(14, 0, 0, 14),
            });

            var textPanel = new StackPanel
            {
                Margin = new Thickness(18, 16, 18, 16),
            };
            textPanel.Children.Add(CreateTextBlock(example.Title, 18, FontWeights.SemiBold, "#16202A", new Thickness(0, 0, 0, 10)));
            var description = CreateTextBlock(example.Description, 13, FontWeights.Normal, "#52606D");
            description.TextWrapping = TextWrapping.WrapWholeWords;
            textPanel.Children.Add(description);
            Grid.SetColumn(textPanel, 1);
            layout.Children.Add(textPanel);

            card.Child = layout;
            button.Content = card;
            return button;
        }

        private void HandleSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isRefreshing)
            {
                return;
            }

            _viewModel.SearchText = _searchBox.Text;
        }

        private void HandleLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRefreshing)
            {
                return;
            }

            _viewModel.SelectedLanguage = _languageComboBox.SelectedItem as DemoLanguageOption;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_refreshQueued)
            {
                return;
            }

            _refreshQueued = true;
            DispatcherQueue.TryEnqueue(() =>
            {
                _refreshQueued = false;
                RefreshUi();
            });
        }

        private string ResolveReportDesignerTheme()
        {
            if (string.Equals(_viewModel.SelectedThemeCode, "night", StringComparison.OrdinalIgnoreCase))
            {
                return "dark";
            }

            if (string.Equals(_viewModel.SelectedThemeCode, "day", StringComparison.OrdinalIgnoreCase))
            {
                return "light";
            }

            return Application.Current?.RequestedTheme == ApplicationTheme.Dark ? "dark" : "light";
        }

        private void RefreshDrawerGroupCards()
        {
            _drawerGroupPanel.Children.Clear();

            foreach (var group in _viewModel.DrawerGroups)
            {
                var button = new Button
                {
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Tag = group.Id,
                };
                button.Click += (_, _) => _viewModel.SelectDrawerGroup((string)button.Tag);

                var card = new Border
                {
                    Background = CreateBrush(group.IsSelected ? "#F1F9FD" : "#FBFBF9"),
                    BorderBrush = CreateBrush(group.IsSelected ? group.AccentHex : "#D5D0C5"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(12),
                };

                var layout = new Grid { ColumnSpacing = 12 };
                layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                layout.Children.Add(new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = CreateBrush(group.AccentHex),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 5, 0, 0),
                });

                var textPanel = new StackPanel();
                textPanel.Children.Add(CreateTextBlock(group.Title, 16, FontWeights.SemiBold, "#17212B"));
                var description = CreateTextBlock(group.Description, 12, FontWeights.Normal, "#52606D", new Thickness(0, 4, 0, 0));
                description.TextWrapping = TextWrapping.WrapWholeWords;
                textPanel.Children.Add(description);
                Grid.SetColumn(textPanel, 1);
                layout.Children.Add(textPanel);

                card.Child = layout;
                button.Content = card;
                _drawerGroupPanel.Children.Add(button);
            }
        }

        private static TextBlock CreateTextBlock(string text, double fontSize, FontWeight weight, string foregroundHex, Thickness? margin = null)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Bahnschrift"),
                FontSize = fontSize,
                FontWeight = weight,
                Foreground = CreateBrush(foregroundHex),
                Margin = margin ?? new Thickness(0),
            };
        }

        private static SolidColorBrush CreateBrush(string hexColor)
        {
            return new SolidColorBrush(ParseColor(hexColor));
        }

        private static Windows.UI.Color ParseColor(string hexColor)
        {
            var normalized = (hexColor ?? "#000000").Trim().TrimStart('#');
            if (normalized.Length == 6)
            {
                normalized = "FF" + normalized;
            }

            return Windows.UI.Color.FromArgb(
                Convert.ToByte(normalized.Substring(0, 2), 16),
                Convert.ToByte(normalized.Substring(2, 2), 16),
                Convert.ToByte(normalized.Substring(4, 2), 16),
                Convert.ToByte(normalized.Substring(6, 2), 16));
        }
    }
}


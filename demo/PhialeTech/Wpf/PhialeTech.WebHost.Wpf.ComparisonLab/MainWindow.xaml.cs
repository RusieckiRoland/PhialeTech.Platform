using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PhialeTech.Components.Wpf;
using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.MonacoEditor.Wpf.Controls;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Wpf.Controls;

namespace PhialeTech.WebHost.Wpf.ComparisonLab
{
    public partial class MainWindow : Window
    {
        private const string SampleCode =
@"namespace: comparison.lab
document:
  id: review
  kind: Form
  name: Host comparison
  fields:
    - id: notes
      caption: Notes
  layout:
    type: Column
    items:
      - fieldRef: notes";

        public MainWindow()
        {
            InitializeComponent();
            ComparisonLabTrace.Write("ui", "window", "MainWindow constructed");
            BuildUi();
        }

        private void BuildUi()
        {
            ScenarioStack.Children.Add(new TextBlock
            {
                Text = "WebHost comparison lab: standard WebView2 on the left, WebView2CompositionControl on the right.",
                FontSize = 28,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            });
            ScenarioStack.Children.Add(new TextBlock
            {
                Text = "Use this window to compare keyboard input, Ctrl+A, context menu, focus recovery, clipping and scroll behavior across identical Monaco scenarios.",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105))
            });
            ScenarioStack.Children.Add(new TextBlock
            {
                Text = "Log file: " + ComparisonLabTrace.CurrentLogFilePath,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 16),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235))
            });

            ScenarioStack.Children.Add(CreateScenarioRow(
                "Normal",
                "Direct host without tabs or scroll wrappers.",
                CreatePlainSurface(false, "plain.standard"),
                CreatePlainSurface(true, "plain.composition")));

            ScenarioStack.Children.Add(CreateScenarioRow(
                "Scrolled",
                "Editor inside a vertical ScrollViewer with large content above and below.",
                CreateScrolledSurface(false, "scrolled.standard", 340),
                CreateScrolledSurface(true, "scrolled.composition", 340)));

            ScenarioStack.Children.Add(CreateScenarioRow(
                "In Tabs",
                "Editor hosted in the first tab of a TabControl next to unrelated content.",
                CreateTabbedSurface(false, "tabs.standard"),
                CreateTabbedSurface(true, "tabs.composition")));

            ScenarioStack.Children.Add(CreateScenarioRow(
                "Tabs + Scroll",
                "TabControl itself sits inside a ScrollViewer to stress nested layout and focus.",
                CreateTabbedScrolledSurface(false, "tabscroll.standard", 360),
                CreateTabbedScrolledSurface(true, "tabscroll.composition", 360)));

            ScenarioStack.Children.Add(CreateScenarioRow(
                "Expander",
                "Editor in an expanded Expander to catch delayed measure and focus restore issues.",
                CreateExpanderSurface(false, "expander.standard"),
                CreateExpanderSurface(true, "expander.composition")));

            ScenarioStack.Children.Add(CreateScenarioRow(
                "Reparent",
                "Move the same side-specific editor between two content slots to observe host reattachment behavior.",
                CreateReparentSurface(false, "reparent.standard"),
                CreateReparentSurface(true, "reparent.composition")));
        }

        private UIElement CreatePlainSurface(bool useCompositionControl, string traceName)
        {
            if (useCompositionControl)
            {
                return CreateProductionMonacoSurface(traceName);
            }

            return CreateEditorBorder(CreateEditorHostGrid(CreateComparisonEditorSurface(CreateFactory(useCompositionControl), traceName)));
        }

        private UIElement CreateScrolledSurface(bool useCompositionControl, string traceName, double viewportHeight)
        {
            var editor = useCompositionControl
                ? CreateProductionMonacoSurface(traceName)
                : CreateEditorBorder(CreateEditorHostGrid(CreateComparisonEditorSurface(CreateFactory(false), traceName)));
            var stack = new StackPanel();
            stack.Children.Add(new Border { Height = 220, Background = Brushes.Transparent });
            stack.Children.Add(editor);
            stack.Children.Add(new Border { Height = 260, Background = Brushes.Transparent });

            if (useCompositionControl)
            {
                return new PhialeWebComponentScrollHost
                {
                    Height = viewportHeight,
                    HostedContent = stack
                };
            }

            return new ScrollViewer
            {
                Height = viewportHeight,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = stack
            };
        }

        private UIElement CreateTabbedSurface(bool useCompositionControl, string traceName)
        {
            var tabs = new TabControl();
            tabs.SelectionChanged += (_, __) =>
                ComparisonLabTrace.Write("ui", traceName, "tab selection changed; selectedIndex=" + tabs.SelectedIndex);
            tabs.Items.Add(new TabItem
            {
                Header = "Editor",
                Content = useCompositionControl
                    ? CreateProductionMonacoSurface(traceName)
                    : CreateEditorBorder(CreateEditorHostGrid(CreateComparisonEditorSurface(CreateFactory(false), traceName)))
            });
            tabs.Items.Add(new TabItem
            {
                Header = "Info",
                Content = new TextBlock
                {
                    Text = "Switch back to the editor tab and verify typing, Ctrl+A and Monaco context menu.",
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap
                }
            });

            return tabs;
        }

        private UIElement CreateTabbedScrolledSurface(bool useCompositionControl, string traceName, double viewportHeight)
        {
            var stack = new StackPanel();
            stack.Children.Add(new Border { Height = 160, Background = Brushes.Transparent });
            stack.Children.Add(CreateTabbedSurface(useCompositionControl, traceName));
            stack.Children.Add(new Border { Height = 200, Background = Brushes.Transparent });

            if (useCompositionControl)
            {
                return new PhialeWebComponentScrollHost
                {
                    Height = viewportHeight,
                    HostedContent = stack
                };
            }

            return new ScrollViewer
            {
                Height = viewportHeight,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = stack
            };
        }

        private UIElement CreateExpanderSurface(bool useCompositionControl, string traceName)
        {
            var expander = new Expander
            {
                IsExpanded = true,
                Header = "Toggle this expander and re-check input routing",
                Content = useCompositionControl
                    ? CreateProductionMonacoSurface(traceName)
                    : CreateEditorBorder(CreateEditorHostGrid(CreateComparisonEditorSurface(CreateFactory(false), traceName))),
                Margin = new Thickness(0, 6, 0, 0)
            };
            expander.Expanded += (_, __) => ComparisonLabTrace.Write("ui", traceName, "expander expanded");
            expander.Collapsed += (_, __) => ComparisonLabTrace.Write("ui", traceName, "expander collapsed");
            return expander;
        }

        private UIElement CreateReparentSurface(bool useCompositionControl, string traceName)
        {
            var editor = useCompositionControl
                ? (FrameworkElement)CreateProductionMonacoSurface(traceName)
                : CreateComparisonEditorSurface(CreateFactory(false), traceName);
            var leftSlot = new ContentControl();
            var rightSlot = new ContentControl();
            leftSlot.Content = editor;

            var moveLeftButton = new Button
            {
                Content = "Move to slot A",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 6, 12, 6)
            };
            moveLeftButton.Click += (_, __) =>
            {
                ComparisonLabTrace.Write("ui", traceName, "move editor to slot A");
                rightSlot.Content = null;
                leftSlot.Content = editor;
            };

            var moveRightButton = new Button
            {
                Content = "Move to slot B",
                Padding = new Thickness(12, 6, 12, 6)
            };
            moveRightButton.Click += (_, __) =>
            {
                ComparisonLabTrace.Write("ui", traceName, "move editor to slot B");
                leftSlot.Content = null;
                rightSlot.Content = editor;
            };

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10),
                Children =
                {
                    moveLeftButton,
                    moveRightButton
                }
            };

            var slots = new Grid();
            slots.ColumnDefinitions.Add(new ColumnDefinition());
            slots.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(leftSlot, 0);
            Grid.SetColumn(rightSlot, 1);
            leftSlot.Margin = new Thickness(0, 0, 8, 0);
            rightSlot.Margin = new Thickness(8, 0, 0, 0);
            leftSlot.ContentTemplate = null;
            rightSlot.ContentTemplate = null;
            slots.Children.Add(leftSlot);
            slots.Children.Add(rightSlot);

            var root = new StackPanel();
            root.Children.Add(buttonRow);
            root.Children.Add(slots);
            return root;
        }

        private Border CreateScenarioRow(string title, string description, UIElement leftContent, UIElement rightContent)
        {
            var root = new Border
            {
                Margin = new Thickness(0, 0, 0, 18),
                Padding = new Thickness(16),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            });
            stack.Children.Add(new TextBlock
            {
                Text = description,
                Margin = new Thickness(0, 6, 0, 14),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105))
            });

            var comparisonGrid = new Grid();
            comparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            comparisonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftPanel = CreateColumnPanel("WebView2", leftContent);
            var rightPanel = CreateColumnPanel("WebView2CompositionControl", rightContent);

            Grid.SetColumn(leftPanel, 0);
            Grid.SetColumn(rightPanel, 1);
            comparisonGrid.Children.Add(leftPanel);
            comparisonGrid.Children.Add(rightPanel);

            stack.Children.Add(comparisonGrid);
            root.Child = stack;
            return root;
        }

        private Border CreateColumnPanel(string title, UIElement content)
        {
            return new Border
            {
                Margin = title == "WebView2" ? new Thickness(0, 0, 10, 0) : new Thickness(10, 0, 0, 0),
                Padding = new Thickness(12),
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 16,
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        content
                    }
                }
            };
        }

        private Grid CreateEditorHostGrid(FrameworkElement editor)
        {
            return new Grid
            {
                Height = 260,
                Children =
                {
                    editor
                }
            };
        }

        private Border CreateEditorBorder(UIElement content)
        {
            return new Border
            {
                MinHeight = 260,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Child = content
            };
        }

        private FrameworkElement CreateComparisonEditorSurface(IWebComponentHostFactory factory, string traceName)
        {
            var editor = new PhialeMonacoEditor(
                factory,
                new MonacoEditorOptions
                {
                    InitialTheme = "light",
                    InitialLanguage = "yaml",
                    InitialValue = SampleCode,
                })
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Text = "Initializing Monaco..."
            };

            bool opened = false;

            ComparisonLabTrace.Write("ui", traceName, "editor surface created; factory=" + factory.GetType().Name);

            editor.ReadyStateChanged += (_, args) =>
            {
                ComparisonLabTrace.Write(
                    "monaco",
                    traceName,
                    "ready state changed; initialized=" + args.IsInitialized + "; ready=" + args.IsReady);
                statusText.Text = args.IsReady
                    ? "Ready: Monaco loaded and host bridge connected."
                    : args.IsInitialized
                        ? "Initialized: waiting for Monaco ready signal..."
                        : "Waiting for host initialization...";
            };
            editor.ErrorOccurred += (_, args) =>
            {
                ComparisonLabTrace.Write(
                    "monaco",
                    traceName,
                    "error; message=" + ComparisonLabTrace.SafeSnippet(args.Message) + "; detail=" + ComparisonLabTrace.SafeSnippet(args.Detail));
                statusText.Text = "Monaco error: " + args.Message + " " + args.Detail;
                statusText.Foreground = Brushes.IndianRed;
            };
            editor.ContentChanged += (_, args) =>
            {
                ComparisonLabTrace.Write(
                    "monaco",
                    traceName,
                    "content changed; length=" + (args.Value ?? string.Empty).Length + "; snippet=" + ComparisonLabTrace.SafeSnippet(args.Value));
            };

            var root = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(editor, 0);
            Grid.SetRow(statusText, 1);
            root.Children.Add(editor);
            root.Children.Add(statusText);

            root.Loaded += async (_, __) =>
            {
                ComparisonLabTrace.Write("ui", traceName, "root loaded; opened=" + opened);
                if (opened)
                {
                    return;
                }

                opened = true;

                try
                {
                    await editor.InitializeAsync().ConfigureAwait(true);
                    await editor.SetThemeAsync("light").ConfigureAwait(true);
                    await editor.SetLanguageAsync("yaml").ConfigureAwait(true);
                    await editor.SetValueAsync(SampleCode).ConfigureAwait(true);
                    editor.FocusEditor();
                }
                catch (Exception ex)
                {
                    ComparisonLabTrace.Write("monaco", traceName, "initialization failed; " + ex);
                    statusText.Text = "Monaco initialization failed: " + ex.Message;
                    statusText.Foreground = Brushes.IndianRed;
                }
            };

            return root;
        }

        private static IWebComponentHostFactory CreateFactory(bool useCompositionControl)
        {
            return useCompositionControl
                ? (IWebComponentHostFactory)new CompositionWebComponentHostFactory()
                : new StandardWebComponentHostFactory();
        }

        private UIElement CreateProductionMonacoSurface(string traceName)
        {
            ComparisonLabTrace.Write("ui", traceName, "using production MonacoEditorShowcaseView");

            var view = new MonacoEditorShowcaseView("en", "light")
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            return CreateEditorBorder(CreateEditorHostGrid(view));
        }

    }
}


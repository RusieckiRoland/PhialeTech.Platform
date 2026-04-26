using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Wpf;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PhialeTech.DocumentEditor.Wpf.Controls
{
    public sealed class PhialeDocumentEditor : UserControl, IDocumentEditor
    {
        private static readonly Thickness HostInset = new Thickness(6);
        private readonly DocumentEditorOptions _options;
        private readonly DocumentEditorWorkspace _workspace;
        private readonly IWebComponentHost _host;
        private readonly DocumentEditorRuntime _runtime;
        private readonly Grid _hostRoot;
        private readonly Grid _hostPresenter;
        private readonly Border _hostViewport;
        private readonly Grid _overlayRoot;
        private readonly Border _overlayBackdrop;
        private readonly Border _overlayViewport;
        private readonly Grid _overlayPresenter;
        private Panel _overlayHostPanel;
        private Window _overlayWindow;
        private Window _overlayHostWindow;
        private bool _isOverlayOpen;
        private bool _disposed;

        public PhialeDocumentEditor()
            : this(new WpfWebComponentHostFactory(), new DocumentEditorOptions())
        {
        }

        public PhialeDocumentEditor(IWebComponentHostFactory hostFactory, DocumentEditorOptions options = null)
        {
            if (hostFactory == null)
            {
                throw new ArgumentNullException(nameof(hostFactory));
            }

            _options = (options ?? new DocumentEditorOptions()).Clone();
            _workspace = new DocumentEditorWorkspace(_options);
            _host = hostFactory.CreateHost(new WebComponentHostOptions
            {
                LocalContentRootPath = _workspace.WorkspaceRootPath,
                JavaScriptReadyMessageType = _options.ReadyMessageType,
                VirtualHostName = _options.VirtualHostName,
                QueueMessagesUntilReady = true
            });

            var hostElement = _host as UIElement;
            if (hostElement == null)
            {
                throw new InvalidOperationException("The supplied WPF web host factory did not return a WPF UI element.");
            }

            var hostFrameworkElement = hostElement as FrameworkElement;
            if (hostFrameworkElement != null)
            {
                hostFrameworkElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                hostFrameworkElement.VerticalAlignment = VerticalAlignment.Stretch;
                hostFrameworkElement.MinWidth = 0d;
                hostFrameworkElement.MinHeight = 0d;
                hostFrameworkElement.Margin = new Thickness(0d);
            }

            _runtime = new DocumentEditorRuntime(_host, _workspace, _options);
            _runtime.NativeFileActionRequested += HandleNativeFileActionRequested;
            _host.MessageReceived += HandleHostMessageReceived;

            _hostRoot = new Grid
            {
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var hostPresenter = new Grid
            {
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _hostPresenter = hostPresenter;
            hostPresenter.Children.Add(hostElement);

            _hostViewport = new Border
            {
                Padding = HostInset,
                Background = System.Windows.Media.Brushes.Transparent,
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = hostPresenter,
            };
            _hostRoot.Children.Add(_hostViewport);

            _overlayBackdrop = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(132, 15, 23, 42)),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            _overlayBackdrop.MouseLeftButtonDown += HandleOverlayBackdropMouseLeftButtonDown;

            _overlayPresenter = new Grid
            {
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            _overlayViewport = new Border
            {
                Margin = new Thickness(12),
                Background = System.Windows.Media.Brushes.Transparent,
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = _overlayPresenter,
            };

            _overlayRoot = new Grid
            {
                Background = System.Windows.Media.Brushes.Transparent,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            _overlayRoot.Children.Add(_overlayBackdrop);
            _overlayRoot.Children.Add(_overlayViewport);

            Content = _hostRoot;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;
        }

        public DocumentEditorOptions Options => _options;

        public new bool IsInitialized => _runtime.IsInitialized;

        public bool IsReady => _runtime.IsReady;

        public DocumentEditorState State => _runtime.State;

        public string Theme => _runtime.Theme;

        public event EventHandler<DocumentEditorReadyStateChangedEventArgs> ReadyStateChanged
        {
            add { _runtime.ReadyStateChanged += value; }
            remove { _runtime.ReadyStateChanged -= value; }
        }

        public event EventHandler<DocumentEditorContentChangedEventArgs> ContentChanged
        {
            add { _runtime.ContentChanged += value; }
            remove { _runtime.ContentChanged -= value; }
        }

        public event EventHandler<DocumentEditorSelectionChangedEventArgs> SelectionChanged
        {
            add { _runtime.SelectionChanged += value; }
            remove { _runtime.SelectionChanged -= value; }
        }

        public event EventHandler<DocumentEditorErrorEventArgs> ErrorOccurred
        {
            add { _runtime.ErrorOccurred += value; }
            remove { _runtime.ErrorOccurred -= value; }
        }

        public event EventHandler<string> ThemeChanged
        {
            add { _runtime.ThemeChanged += value; }
            remove { _runtime.ThemeChanged -= value; }
        }

        public Task InitializeAsync() => _runtime.InitializeAsync();

        public Task SetHtmlAsync(string html) => _runtime.SetHtmlAsync(html);

        public Task<string> GetHtmlAsync() => _runtime.GetHtmlAsync();

        public Task SetMarkdownAsync(string markdown) => _runtime.SetMarkdownAsync(markdown);

        public Task<string> GetMarkdownAsync() => _runtime.GetMarkdownAsync();

        public Task SetDocumentJsonAsync(string documentJson) => _runtime.SetDocumentJsonAsync(documentJson);

        public Task<string> GetDocumentJsonAsync() => _runtime.GetDocumentJsonAsync();

        public Task SetThemeAsync(string theme) => _runtime.SetThemeAsync(theme);

        public Task SetLanguageAsync(string languageCode) => _runtime.SetLanguageAsync(languageCode);

        public Task SetReadOnlyAsync(bool isReadOnly) => _runtime.SetReadOnlyAsync(isReadOnly);

        public Task SetToolbarAsync(DocumentEditorToolbarConfig toolbar) => _runtime.SetToolbarAsync(toolbar);

        public Task ClearAsync() => _runtime.ClearAsync();

        public void FocusEditor() => _runtime.FocusEditor();

        public Task ExecuteCommandAsync(DocumentEditorCommand command, string value = null) => _runtime.ExecuteCommandAsync(command, value);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Loaded -= HandleLoaded;
            Unloaded -= HandleUnloaded;
            _runtime.NativeFileActionRequested -= HandleNativeFileActionRequested;
            _host.MessageReceived -= HandleHostMessageReceived;
            SetOverlayOpen(false, false);
            DetachOverlayWindow();
            _overlayBackdrop.MouseLeftButtonDown -= HandleOverlayBackdropMouseLeftButtonDown;
            _runtime.Dispose();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            _ = _runtime.InitializeAsync();
            UpdateOverlayPlacement();
            if (_isOverlayOpen)
            {
                ApplyOverlayState(true);
            }
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isOverlayOpen)
            {
                ApplyOverlayState(false);
            }
        }

        private async void HandleNativeFileActionRequested(object sender, DocumentEditorNativeFileActionRequestedEventArgs e)
        {
            try
            {
                switch (e.Kind)
                {
                    case DocumentEditorNativeFileActionKind.ExportHtml:
                        SaveTextWithDesktopDialog("Export HTML", "HTML document (*.html)|*.html|All files (*.*)|*.*", "document-editor.html", e.Html);
                        return;
                    case DocumentEditorNativeFileActionKind.ExportMarkdown:
                        SaveTextWithDesktopDialog("Export Markdown", "Markdown document (*.md)|*.md|All files (*.*)|*.*", "document-editor.md", e.Markdown);
                        return;
                    case DocumentEditorNativeFileActionKind.SaveJson:
                        SaveTextWithDesktopDialog("Save Document JSON", "JSON document (*.json)|*.json|All files (*.*)|*.*", "document-editor.json", e.DocumentJson);
                        return;
                    case DocumentEditorNativeFileActionKind.LoadJson:
                        await LoadJsonWithDesktopDialogAsync().ConfigureAwait(true);
                        return;
                    default:
                        throw new InvalidOperationException("Unsupported DocumentEditor native file action: " + e.Kind);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DocumentEditor file operation failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void SaveTextWithDesktopDialog(string title, string filter, string fileName, string content)
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = fileName,
                AddExtension = true,
                OverwritePrompt = true,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            File.WriteAllText(dialog.FileName, content ?? string.Empty, Encoding.UTF8);
        }

        private async Task LoadJsonWithDesktopDialogAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Load Document JSON",
                Filter = "JSON document (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string documentJson = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            await SetDocumentJsonAsync(documentJson).ConfigureAwait(true);
        }

        private void HandleHostMessageReceived(object sender, WebComponentMessageEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.MessageType) || !string.Equals(e.MessageType, "documentEditor.toggleOverlay", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool isOpen = ReadOverlayState(e.RawMessage);
            Dispatcher.BeginInvoke(new Action(() => SetOverlayOpen(isOpen, true)));
        }

        private void HandleOverlayBackdropMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            SetOverlayOpen(false, true);
        }

        private void SetOverlayOpen(bool isOpen, bool notifyWeb)
        {
            _isOverlayOpen = isOpen;
            if (IsLoaded)
            {
                ApplyOverlayState(isOpen);
            }

            if (notifyWeb && _host.IsInitialized)
            {
                _ = _host.PostMessageAsync(new { type = "documentEditor.setOverlay", isOpen });
            }
        }

        private void ApplyOverlayState(bool isOpen)
        {
            UpdateOverlayPlacement();
            if (isOpen)
            {
                if (TryApplyOverlayWindow())
                {
                    MoveViewport(_overlayPresenter);
                    _host.FocusHost();
                    return;
                }

                if (!TryApplyPanelOverlay())
                {
                    return;
                }

                MoveViewport(_overlayPresenter);
                _host.FocusHost();
                return;
            }

            RemoveOverlayWindow();
            RemovePanelOverlay();
            MoveViewport(_hostRoot);
        }

        private void MoveViewport(Panel target)
        {
            if (target == null)
            {
                return;
            }

            if (ReferenceEquals(_hostViewport.Parent, target))
            {
                return;
            }

            if (_hostViewport.Parent is Panel currentPanel)
            {
                currentPanel.Children.Remove(_hostViewport);
            }
            else if (_hostViewport.Parent is Decorator currentDecorator)
            {
                currentDecorator.Child = null;
            }

            target.Children.Add(_hostViewport);
        }

        private void UpdateOverlayPlacement()
        {
            var window = Window.GetWindow(this);
            if (window == null)
            {
                return;
            }

            if (!ReferenceEquals(_overlayWindow, window))
            {
                DetachOverlayWindow();
                _overlayWindow = window;
                _overlayWindow.SizeChanged += HandleOverlayWindowLayoutChanged;
                _overlayWindow.LocationChanged += HandleOverlayWindowLayoutChanged;
                _overlayWindow.StateChanged += HandleOverlayWindowLayoutChanged;
            }

            _overlayHostPanel = FindOverlayHostPanel(this);
            if (_overlayHostPanel != null)
            {
                PrepareOverlayRootForHostPanel(_overlayHostPanel);
            }

            if (_isOverlayOpen && _overlayHostWindow != null)
            {
                UpdateOverlayWindowBounds();
            }
        }

        private bool TryApplyOverlayWindow()
        {
            var owner = Window.GetWindow(this);
            if (owner == null)
            {
                return false;
            }

            RemovePanelOverlay();

            if (_overlayHostWindow == null)
            {
                _overlayHostWindow = new Window
                {
                    Owner = owner,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    AllowsTransparency = false,
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    SizeToContent = SizeToContent.Manual,
                    Topmost = false,
                    Content = _overlayRoot
                };
                _overlayHostWindow.Closed += HandleOverlayHostWindowClosed;
            }
            else if (!ReferenceEquals(_overlayHostWindow.Owner, owner))
            {
                _overlayHostWindow.Owner = owner;
            }

            UpdateOverlayWindowBounds();
            if (!_overlayHostWindow.IsVisible)
            {
                _overlayHostWindow.Show();
            }

            _overlayHostWindow.Activate();
            return true;
        }

        private bool TryApplyPanelOverlay()
        {
            if (_overlayHostPanel == null)
            {
                return false;
            }

            if (!ReferenceEquals(_overlayRoot.Parent, _overlayHostPanel))
            {
                if (_overlayRoot.Parent is Panel currentOverlayParent)
                {
                    currentOverlayParent.Children.Remove(_overlayRoot);
                }

                PrepareOverlayRootForHostPanel(_overlayHostPanel);
                Panel.SetZIndex(_overlayRoot, short.MaxValue);
                _overlayHostPanel.Children.Add(_overlayRoot);
            }
            else
            {
                PrepareOverlayRootForHostPanel(_overlayHostPanel);
            }

            return true;
        }

        private void RemovePanelOverlay()
        {
            if (_overlayRoot.Parent is Panel overlayParent)
            {
                overlayParent.Children.Remove(_overlayRoot);
            }
        }

        private void RemoveOverlayWindow()
        {
            if (_overlayHostWindow == null)
            {
                return;
            }

            var overlayHostWindow = _overlayHostWindow;
            _overlayHostWindow = null;
            overlayHostWindow.Closed -= HandleOverlayHostWindowClosed;
            overlayHostWindow.Content = null;
            overlayHostWindow.Close();
        }

        private void PrepareOverlayRootForHostPanel(Panel hostPanel)
        {
            if (hostPanel == null)
            {
                return;
            }

            _overlayRoot.Width = Math.Max(hostPanel.RenderSize.Width, 0d);
            _overlayRoot.Height = Math.Max(hostPanel.RenderSize.Height, 0d);
            _overlayRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            _overlayRoot.VerticalAlignment = VerticalAlignment.Stretch;

            if (hostPanel is Grid grid)
            {
                Grid.SetRow(_overlayRoot, 0);
                Grid.SetColumn(_overlayRoot, 0);
                Grid.SetRowSpan(_overlayRoot, Math.Max(grid.RowDefinitions.Count, 1));
                Grid.SetColumnSpan(_overlayRoot, Math.Max(grid.ColumnDefinitions.Count, 1));
            }
        }

        private void HandleOverlayWindowLayoutChanged(object sender, EventArgs e)
        {
            UpdateOverlayPlacement();
        }

        private void DetachOverlayWindow()
        {
            if (_overlayWindow == null)
            {
                return;
            }

            _overlayWindow.SizeChanged -= HandleOverlayWindowLayoutChanged;
            _overlayWindow.LocationChanged -= HandleOverlayWindowLayoutChanged;
            _overlayWindow.StateChanged -= HandleOverlayWindowLayoutChanged;
            _overlayWindow = null;
            _overlayHostPanel = null;
        }

        private void UpdateOverlayWindowBounds()
        {
            if (_overlayHostWindow == null)
            {
                return;
            }

            var owner = _overlayHostWindow.Owner ?? Window.GetWindow(this);
            if (owner == null)
            {
                return;
            }

            var contentElement = owner.Content as FrameworkElement;
            if (contentElement == null || contentElement.ActualWidth <= 0d || contentElement.ActualHeight <= 0d)
            {
                _overlayHostWindow.Left = owner.Left;
                _overlayHostWindow.Top = owner.Top;
                _overlayHostWindow.Width = Math.Max(owner.ActualWidth, owner.Width);
                _overlayHostWindow.Height = Math.Max(owner.ActualHeight, owner.Height);
                PrepareOverlayRootForHostPanel(_overlayHostPanel);
                return;
            }

            var topLeft = contentElement.PointToScreen(new Point(0d, 0d));
            var presentationSource = PresentationSource.FromVisual(contentElement);
            if (presentationSource?.CompositionTarget != null)
            {
                topLeft = presentationSource.CompositionTarget.TransformFromDevice.Transform(topLeft);
            }

            _overlayHostWindow.Left = topLeft.X;
            _overlayHostWindow.Top = topLeft.Y;
            _overlayHostWindow.Width = contentElement.ActualWidth;
            _overlayHostWindow.Height = contentElement.ActualHeight;
            _overlayRoot.Width = Math.Max(contentElement.ActualWidth, 0d);
            _overlayRoot.Height = Math.Max(contentElement.ActualHeight, 0d);
            _overlayRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            _overlayRoot.VerticalAlignment = VerticalAlignment.Stretch;
        }

        private void HandleOverlayHostWindowClosed(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, _overlayHostWindow))
            {
                _overlayHostWindow = null;
            }

            if (_isOverlayOpen)
            {
                SetOverlayOpen(false, true);
            }
        }

        private static Panel FindOverlayHostPanel(DependencyObject origin)
        {
            if (origin == null)
            {
                return null;
            }

            Panel lastPanel = null;
            DependencyObject current = origin;
            while (current != null)
            {
                if (current is Panel panel)
                {
                    lastPanel = panel;
                }

                if (current is Window)
                {
                    break;
                }

                current = GetParent(current);
            }

            return lastPanel;
        }

        private static DependencyObject GetParent(DependencyObject current)
        {
            if (current == null)
            {
                return null;
            }

            if (current is Visual)
            {
                var visualParent = VisualTreeHelper.GetParent(current);
                if (visualParent != null)
                {
                    return visualParent;
                }
            }

            return LogicalTreeHelper.GetParent(current);
        }

        private static bool ReadOverlayState(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                return false;
            }

            try
            {
                using (var document = JsonDocument.Parse(rawMessage))
                {
                    if (document.RootElement.ValueKind == JsonValueKind.Object &&
                        document.RootElement.TryGetProperty("isOpen", out var isOpen) &&
                        (isOpen.ValueKind == JsonValueKind.True || isOpen.ValueKind == JsonValueKind.False))
                    {
                        return isOpen.GetBoolean();
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}

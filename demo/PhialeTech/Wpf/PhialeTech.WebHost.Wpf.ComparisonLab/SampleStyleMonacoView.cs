using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using PhialeTech.MonacoEditor;
using PhialeTech.MonacoEditor.Abstractions;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PhialeTech.WebHost.Wpf.ComparisonLab
{
    internal sealed class SampleStyleMonacoView : Grid, IDisposable
    {
        private readonly MonacoEditorWorkspace _workspace;
        private readonly string _traceName;
        private readonly bool _useCompositionControl;
        private readonly TextBlock _status;
        private readonly WebView2 _standardWebView;
        private readonly WebView2CompositionControl _compositionWebView;
        private bool _initialized;
        private bool _disposed;

        public SampleStyleMonacoView(bool useCompositionControl, string traceName, string initialValue)
        {
            _useCompositionControl = useCompositionControl;
            _traceName = traceName ?? throw new ArgumentNullException(nameof(traceName));
            _workspace = new MonacoEditorWorkspace(new MonacoEditorOptions
            {
                InitialTheme = "light",
                InitialLanguage = "yaml",
                InitialValue = initialValue ?? string.Empty
            });
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            if (useCompositionControl)
            {
                _compositionWebView = new WebView2CompositionControl
                {
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                HookCompositionEvents(_compositionWebView);
                SetRow(_compositionWebView, 0);
                Children.Add(_compositionWebView);
            }
            else
            {
                _standardWebView = new WebView2
                {
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                HookStandardEvents(_standardWebView);
                SetRow(_standardWebView, 0);
                Children.Add(_standardWebView);
            }

            _status = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Text = "Initializing raw Microsoft-style WebView..."
            };
            SetRow(_status, 1);
            Children.Add(_status);

            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Loaded -= HandleLoaded;
            Unloaded -= HandleUnloaded;

            if (_standardWebView != null)
            {
                UnhookStandardEvents(_standardWebView);
                _standardWebView.Dispose();
            }

            if (_compositionWebView != null)
            {
                UnhookCompositionEvents(_compositionWebView);
                _compositionWebView.Dispose();
            }

            _workspace.Dispose();
        }

        private async void HandleLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized || _disposed)
                return;

            _initialized = true;
            await InitializeAsync().ConfigureAwait(true);
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            ComparisonLabTrace.Write("sample", _traceName, "view unloaded");
        }

        private async Task InitializeAsync()
        {
            try
            {
                ComparisonLabTrace.Write("sample", _traceName, "workspace prepare start");
                await _workspace.PrepareAsync().ConfigureAwait(true);
                ComparisonLabTrace.Write("sample", _traceName, "workspace prepare done: " + _workspace.WorkspaceRootPath);

                var environment = await CoreWebView2Environment.CreateAsync().ConfigureAwait(true);

                if (_useCompositionControl)
                {
                    await _compositionWebView.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
                    _compositionWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                    _compositionWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "phialetech.monacoeditor",
                        _workspace.WorkspaceRootPath,
                        CoreWebView2HostResourceAccessKind.Allow);
                    _compositionWebView.Source = new Uri("https://phialetech.monacoeditor/MonacoEditor/index.html");
                }
                else
                {
                    await _standardWebView.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
                    _standardWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                    _standardWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "phialetech.monacoeditor",
                        _workspace.WorkspaceRootPath,
                        CoreWebView2HostResourceAccessKind.Allow);
                    _standardWebView.Source = new Uri("https://phialetech.monacoeditor/MonacoEditor/index.html");
                }

                _status.Text = "Ready to click into the editor.";
                ComparisonLabTrace.Write("sample", _traceName, "source assigned");
            }
            catch (Exception ex)
            {
                _status.Text = "Initialization failed: " + ex.Message;
                ComparisonLabTrace.Write("sample", _traceName, "initialization failed; " + ex);
            }
        }

        private void HookStandardEvents(WebView2 webView)
        {
            webView.CoreWebView2InitializationCompleted += Standard_CoreWebView2InitializationCompleted;
            webView.NavigationStarting += Standard_NavigationStarting;
            webView.NavigationCompleted += Standard_NavigationCompleted;
            webView.WebMessageReceived += Standard_WebMessageReceived;
        }

        private void UnhookStandardEvents(WebView2 webView)
        {
            webView.CoreWebView2InitializationCompleted -= Standard_CoreWebView2InitializationCompleted;
            webView.NavigationStarting -= Standard_NavigationStarting;
            webView.NavigationCompleted -= Standard_NavigationCompleted;
            webView.WebMessageReceived -= Standard_WebMessageReceived;
        }

        private void HookCompositionEvents(WebView2CompositionControl webView)
        {
            webView.CoreWebView2InitializationCompleted += Composition_CoreWebView2InitializationCompleted;
            webView.NavigationStarting += Composition_NavigationStarting;
            webView.NavigationCompleted += Composition_NavigationCompleted;
            webView.WebMessageReceived += Composition_WebMessageReceived;
        }

        private void UnhookCompositionEvents(WebView2CompositionControl webView)
        {
            webView.CoreWebView2InitializationCompleted -= Composition_CoreWebView2InitializationCompleted;
            webView.NavigationStarting -= Composition_NavigationStarting;
            webView.NavigationCompleted -= Composition_NavigationCompleted;
            webView.WebMessageReceived -= Composition_WebMessageReceived;
        }

        private void Standard_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            _status.Text = "Initialized: " + (e.InitializationException?.Message ?? "Success");
            ComparisonLabTrace.Write("sample", _traceName, "standard init completed; success=" + (e.InitializationException == null));
        }

        private void Standard_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _status.Text = "NavigationStarting";
            ComparisonLabTrace.Write("sample", _traceName, "standard navigation starting; uri=" + e.Uri);
        }

        private void Standard_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _status.Text = "NavigationCompleted: " + (e.IsSuccess ? "Success" : e.WebErrorStatus.ToString());
            ComparisonLabTrace.Write("sample", _traceName, "standard navigation completed; success=" + e.IsSuccess);
        }

        private void Standard_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = SafeReadMessage(e);
            _status.Text = message;
            ComparisonLabTrace.Write("sample", _traceName, "standard message: " + ComparisonLabTrace.SafeSnippet(message));
        }

        private void Composition_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            _status.Text = "Initialized: " + (e.InitializationException?.Message ?? "Success");
            ComparisonLabTrace.Write("sample", _traceName, "composition init completed; success=" + (e.InitializationException == null));
        }

        private void Composition_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _status.Text = "NavigationStarting";
            ComparisonLabTrace.Write("sample", _traceName, "composition navigation starting; uri=" + e.Uri);
        }

        private void Composition_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _status.Text = "NavigationCompleted: " + (e.IsSuccess ? "Success" : e.WebErrorStatus.ToString());
            ComparisonLabTrace.Write("sample", _traceName, "composition navigation completed; success=" + e.IsSuccess);
        }

        private void Composition_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = SafeReadMessage(e);
            _status.Text = message;
            ComparisonLabTrace.Write("sample", _traceName, "composition message: " + ComparisonLabTrace.SafeSnippet(message));
        }

        private static string SafeReadMessage(CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                return e.TryGetWebMessageAsString();
            }
            catch
            {
                return "<non-string message>";
            }
        }
    }
}


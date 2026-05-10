using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeTech.DocumentEditor;
using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.DocumentEditor.Wpf.Controls;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Wpf.Controls;

namespace PhialeTech.WebHost.Wpf.Tests
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public sealed class DocumentEditorLayoutBehaviorTests
    {
        [Test]
        public void PhialeDocumentEditor_FillsAvailableViewport_WithStretchHostPresenter()
        {
            var hostFactory = new FakeWebComponentHostFactory();
            var editor = new PhialeDocumentEditor(hostFactory, new DocumentEditorOptions());
            var container = new Grid
            {
                Width = 640d,
                Height = 320d
            };

            container.Children.Add(editor);
            container.Measure(new Size(container.Width, container.Height));
            container.Arrange(new Rect(0d, 0d, container.Width, container.Height));
            container.UpdateLayout();

            Assert.That(editor.ActualWidth, Is.EqualTo(640d).Within(0.5d));
            Assert.That(editor.ActualHeight, Is.EqualTo(320d).Within(0.5d));
            Assert.That(hostFactory.Host.ActualWidth, Is.GreaterThan(620d));
            Assert.That(hostFactory.Host.ActualHeight, Is.GreaterThan(300d));
        }

        [Test]
        public void PhialeDocumentEditor_OverlayUsesNearestExplicitScope_WhenHostedInsideLayoutCell()
        {
            var hostFactory = new FakeWebComponentHostFactory();
            var editor = new PhialeDocumentEditor(hostFactory, new DocumentEditorOptions
            {
                OverlayMode = DocumentEditorOverlayMode.Container
            });
            var root = new Grid
            {
                Width = 900d,
                Height = 600d
            };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(96d) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220d) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });

            var cell = new Grid();
            OverlayHost.SetIsScope(cell, true);
            Grid.SetRow(cell, 1);
            Grid.SetColumn(cell, 1);
            cell.Children.Add(editor);
            root.Children.Add(cell);

            var window = new Window
            {
                Width = root.Width,
                Height = root.Height,
                Content = root,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None
            };

            try
            {
                window.Show();
                root.Measure(new Size(root.Width, root.Height));
                root.Arrange(new Rect(0d, 0d, root.Width, root.Height));
                root.UpdateLayout();

                hostFactory.Host.RaiseMessage("{\"type\":\"documentEditor.toggleOverlay\",\"isOpen\":true}", "documentEditor.toggleOverlay");
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                root.UpdateLayout();

                Assert.That(cell.Children.Count, Is.EqualTo(2));
                Assert.That(root.Children.Count, Is.EqualTo(1));
                Assert.That(cell.Children[1], Is.InstanceOf<Grid>());
                Assert.That(Panel.GetZIndex((UIElement)cell.Children[1]), Is.EqualTo(short.MaxValue));
                Assert.That(hostFactory.Host.PostedMessages, Does.Contain("{\"type\":\"documentEditor.setOverlay\",\"isOpen\":true}"));
            }
            finally
            {
                window.Close();
                editor.Dispose();
            }
        }

        [Test]
        public void PhialeDocumentEditor_DoesNotOpenOverlay_WhenOverlayModeIsDisabled()
        {
            var hostFactory = new FakeWebComponentHostFactory();
            var editor = new PhialeDocumentEditor(hostFactory, new DocumentEditorOptions
            {
                OverlayMode = DocumentEditorOverlayMode.Disabled
            });
            var root = new Grid
            {
                Width = 640d,
                Height = 400d
            };

            root.Children.Add(editor);
            var window = new Window
            {
                Width = root.Width,
                Height = root.Height,
                Content = root,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None
            };

            try
            {
                window.Show();
                root.Measure(new Size(root.Width, root.Height));
                root.Arrange(new Rect(0d, 0d, root.Width, root.Height));
                root.UpdateLayout();

                hostFactory.Host.RaiseMessage("{\"type\":\"documentEditor.toggleOverlay\",\"isOpen\":true}", "documentEditor.toggleOverlay");
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                root.UpdateLayout();

                Assert.That(root.Children.Count, Is.EqualTo(1));
            }
            finally
            {
                window.Close();
                editor.Dispose();
            }
        }

        [Test]
        public async Task DocumentEditorWorkspace_NormalizesInitialThemeForBootstrap()
        {
            using (var workspace = new DocumentEditorWorkspace(new DocumentEditorOptions
            {
                InitialTheme = "night"
            }))
            {
                await workspace.PrepareAsync().ConfigureAwait(false);

                var bootstrapPath = Path.Combine(
                    workspace.WorkspaceRootPath,
                    "DocumentEditor",
                    "document-editor.bootstrap.json");
                using (var document = JsonDocument.Parse(File.ReadAllText(bootstrapPath)))
                {
                    Assert.That(document.RootElement.GetProperty("theme").GetString(), Is.EqualTo("dark"));
                }
            }
        }

        [Test]
        [Apartment(System.Threading.ApartmentState.STA)]
        public async Task PhialeDocumentEditor_DoesNotResetTheme_WhenContentMessageDoesNotCarryTheme()
        {
            var hostFactory = new FakeWebComponentHostFactory();
            var editor = new PhialeDocumentEditor(hostFactory, new DocumentEditorOptions());

            try
            {
                await editor.InitializeAsync().ConfigureAwait(true);
                await editor.SetThemeAsync("dark").ConfigureAwait(true);

                hostFactory.Host.RaiseMessage(
                    "{\"type\":\"documentEditor.contentChanged\",\"html\":\"<p>x</p>\",\"markdown\":\"x\",\"documentJson\":\"{}\",\"isReadOnly\":false}",
                    "documentEditor.contentChanged");

                Assert.That(editor.Theme, Is.EqualTo("dark"));
            }
            finally
            {
                editor.Dispose();
            }
        }

        private sealed class FakeWebComponentHostFactory : IWebComponentHostFactory
        {
            public FakeWebComponentHost Host { get; private set; }

            public IWebComponentHost CreateHost(WebComponentHostOptions options)
            {
                Host = new FakeWebComponentHost(options);
                return Host;
            }
        }

        private sealed class FakeWebComponentHost : Border, IWebComponentHost
        {
            private readonly System.Collections.Generic.List<string> _postedMessages = new System.Collections.Generic.List<string>();

            public FakeWebComponentHost(WebComponentHostOptions options)
            {
                Options = options ?? new WebComponentHostOptions();
                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Stretch;
                MinWidth = 0d;
                MinHeight = 0d;
            }

            public WebComponentHostOptions Options { get; }

            public new bool IsInitialized => true;

            public bool IsReady => true;

            public event EventHandler<WebComponentMessageEventArgs> MessageReceived;

            public event EventHandler<WebComponentReadyStateChangedEventArgs> ReadyStateChanged;

            public System.Collections.Generic.IReadOnlyList<string> PostedMessages => _postedMessages;

            public Task InitializeAsync()
            {
                ReadyStateChanged?.Invoke(this, new WebComponentReadyStateChangedEventArgs(true, true));
                return Task.CompletedTask;
            }

            public Task LoadEntryPageAsync(string entryPageRelativePath) => Task.CompletedTask;

            public Task NavigateAsync(Uri uri) => Task.CompletedTask;

            public Task LoadHtmlAsync(string html, string baseUrl = null) => Task.CompletedTask;

            public Task PostMessageAsync(object message)
            {
                _postedMessages.Add(System.Text.Json.JsonSerializer.Serialize(message));
                return Task.CompletedTask;
            }

            public Task PostRawMessageAsync(string rawMessage)
            {
                _postedMessages.Add(rawMessage ?? string.Empty);
                return Task.CompletedTask;
            }

            public Task<string> ExecuteScriptAsync(string script) => Task.FromResult(string.Empty);

            public void FocusHost()
            {
                MessageReceived?.Invoke(this, new WebComponentMessageEventArgs(string.Empty, string.Empty));
            }

            public void RaiseMessage(string rawMessage, string messageType)
            {
                MessageReceived?.Invoke(this, new WebComponentMessageEventArgs(rawMessage, messageType));
            }

            public void Dispose()
            {
            }
        }
    }
}


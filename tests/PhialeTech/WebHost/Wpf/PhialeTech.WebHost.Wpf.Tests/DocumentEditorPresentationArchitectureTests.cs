using System;
using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class DocumentEditorPresentationArchitectureTests
    {
        [Test]
        public void StoryBook_ListsDocumentEditor_InWebComponents_AndYamlAdvancedControls()
        {
            var featureCatalog = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Services", "DemoFeatureCatalog.cs"));
            var viewModel = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "ViewModels", "DemoShellViewModel.cs"));
            var textKeys = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Localization", "DemoTextKeys.cs"));
            var english = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Languages", "en.lang"));
            var polish = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Languages", "pl.lang"));

            Assert.That(featureCatalog, Does.Contain("new DemoExampleDefinition(\"yaml-advanced-controls\", \"yaml-ui\""));
            Assert.That(featureCatalog, Does.Contain("new DemoExampleDefinition(\"document-editor\", \"web-components\""));
            Assert.That(viewModel, Does.Contain("public bool IsYamlAdvancedControlsExample => SelectedExample != null && string.Equals(SelectedExample.Id, \"yaml-advanced-controls\", StringComparison.OrdinalIgnoreCase);"));
            Assert.That(viewModel, Does.Contain("public bool IsDocumentEditorExample => SelectedExample != null && string.Equals(SelectedExample.Id, \"document-editor\", StringComparison.OrdinalIgnoreCase);"));
            Assert.That(textKeys, Does.Contain("ExampleYamlAdvancedControlsTitle"));
            Assert.That(textKeys, Does.Contain("ExampleYamlAdvancedControlsDescription"));
            Assert.That(textKeys, Does.Contain("ExampleDocumentEditorTitle"));
            Assert.That(textKeys, Does.Contain("ExampleDocumentEditorDescription"));
            Assert.That(english, Does.Contain("Example.YamlAdvancedControls.Title="));
            Assert.That(english, Does.Contain("Example.DocumentEditor.Title="));
            Assert.That(polish, Does.Contain("Example.YamlAdvancedControls.Title="));
            Assert.That(polish, Does.Contain("Example.DocumentEditor.Title="));
        }

        [Test]
        public void MainWindow_RendersYamlAdvancedControls_AndDocumentEditorShowcase()
        {
            var codeBehind = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));
            var xaml = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes.Linked", "Demo", "PhialeTech.Components.Wpf.MainWindow.xaml"));

            Assert.That(codeBehind, Does.Contain("DocumentEditorShowcaseView"));
            Assert.That(codeBehind, Does.Contain("_documentEditorShowcaseView"));
            Assert.That(codeBehind, Does.Contain("ApplyYamlAdvancedDocumentEditorThemeAsync()"));
            Assert.That(xaml, Does.Contain("IsYamlAdvancedControlsExample"));
            Assert.That(xaml, Does.Contain("IsDocumentEditorExample"));
            Assert.That(xaml, Does.Contain("YamlDocumentEditor"));
            Assert.That(xaml, Does.Contain("UseLayoutRounding=\"True\""));
            Assert.That(xaml, Does.Contain("SnapsToDevicePixels=\"True\""));
        }

        [Test]
        public void DocumentEditor_UsesInsetHost_ThemeMessages_AndIconToolbar()
        {
            var wpfControl = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Platforms", "Wpf", "PhialeTech.DocumentEditor.Wpf", "Controls", "PhialeDocumentEditor.cs"));
            var overlayBase = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "Platforms", "Wpf", "PhialeTech.WebHost.Wpf", "Controls", "OverlayableWebComponentControlBase.cs"));
            var overlayHost = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "Platforms", "Wpf", "PhialeTech.WebHost.Wpf", "Controls", "OverlayHost.cs"));
            var webHost = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "Platforms", "Wpf", "PhialeTech.WebHost.Wpf", "Controls", "PhialeWebComponentHost.cs"));
            var abstractions = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Abstractions", "PhialeTech.DocumentEditor.Abstractions", "IDocumentEditor.cs"));
            var overlayOptionsInterface = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Abstractions", "PhialeTech.DocumentEditor.Abstractions", "IDocumentEditorOverlayOptions.cs"));
            var overlayMode = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Abstractions", "PhialeTech.DocumentEditor.Abstractions", "DocumentEditorOverlayMode.cs"));
            var options = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Abstractions", "PhialeTech.DocumentEditor.Abstractions", "DocumentEditorOptions.cs"));
            var runtime = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Core", "PhialeTech.DocumentEditor", "DocumentEditorRuntime.cs"));
            var workspace = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Core", "PhialeTech.DocumentEditor", "DocumentEditorWorkspace.cs"));
            var yamlControl = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "YamlApp", "Platforms", "Wpf", "PhialeTech.YamlApp.Wpf", "Controls", "DocumentEditor", "YamlDocumentEditor.cs"));
            var yamlDocumentHost = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "YamlApp", "Platforms", "Wpf", "PhialeTech.YamlApp.Wpf", "Document", "YamlDocumentHost.cs"));
            var yamlLayoutRenderer = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "YamlApp", "Platforms", "Wpf", "PhialeTech.YamlApp.Wpf", "Document", "YamlDocumentLayoutRenderer.cs"));
            var yamlFieldFactory = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "YamlApp", "Platforms", "Wpf", "PhialeTech.YamlApp.Wpf", "Document", "YamlFieldControlFactory.cs"));
            var yamlRuntimeBinding = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "YamlApp", "Runtime", "PhialeTech.YamlApp.Runtime", "Controls", "DocumentEditor", "YamlDocumentEditorFieldBinding.cs"));
            var yamlRuntimeBindingState = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "YamlApp", "Runtime", "PhialeTech.YamlApp.Runtime", "Controls", "DocumentEditor", "YamlDocumentEditorFieldBindingState.cs"));
            var showcase = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "DocumentEditorShowcaseView.cs"));
            var codeBehind = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));
            var hostedYamlFactory = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "Hosting", "DemoYamlHostedSurfaceFactory.cs"));
            var xaml = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.Styles.Wpf", "Themes.Linked", "Demo", "PhialeTech.Components.Wpf.MainWindow.xaml"));
            var html = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.WebAssets", "Assets", "DocumentEditor", "index.html"));
            var script = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.WebAssets", "Assets", "DocumentEditor", "phiale-document-editor-host.js"));
            var yamlAdvancedOverlayScopeCount = xaml.Split(new[] { "webHost:OverlayHost.IsScope=\"True\"" }, StringSplitOptions.None).Length - 1;

            Assert.That(wpfControl, Does.Contain("private static readonly Thickness HostInset = new Thickness(6);"));
            Assert.That(wpfControl, Does.Contain("OverlayableWebComponentControlBase"));
            Assert.That(wpfControl, Does.Contain("InitializeOverlaySurface(hostElement, HostInset"));
            Assert.That(wpfControl, Does.Contain("OpenOverlayInNearestScope();"));
            Assert.That(overlayBase, Does.Contain("Padding = hostInset"));
            Assert.That(overlayBase, Does.Contain("ClipToBounds = true"));
            Assert.That(overlayBase, Does.Contain("UseLayoutRounding = true"));
            Assert.That(overlayBase, Does.Contain("SnapsToDevicePixels = true;"));
            Assert.That(overlayBase, Does.Contain("HostPresenter = new Grid"));
            Assert.That(overlayBase, Does.Contain("hostFrameworkElement.HorizontalAlignment = HorizontalAlignment.Stretch;"));
            Assert.That(overlayBase, Does.Contain("hostFrameworkElement.VerticalAlignment = VerticalAlignment.Stretch;"));
            Assert.That(overlayBase, Does.Contain("hostFrameworkElement.MinWidth = 0d;"));
            Assert.That(overlayBase, Does.Contain("hostFrameworkElement.MinHeight = 0d;"));
            Assert.That(overlayHost, Does.Contain("IsScopeProperty"));
            Assert.That(overlayHost, Does.Contain("FindNearestScopePanel"));
            Assert.That(webHost, Does.Contain("HorizontalAlignment = HorizontalAlignment.Stretch;"));
            Assert.That(webHost, Does.Contain("VerticalAlignment = VerticalAlignment.Stretch;"));
            Assert.That(abstractions, Does.Contain("Task SetThemeAsync(string theme);"));
            Assert.That(abstractions, Does.Contain("Task SetOverlayModeAsync(DocumentEditorOverlayMode overlayMode);"));
            Assert.That(overlayOptionsInterface, Does.Contain("DocumentEditorOverlayMode? OverlayMode { get; }"));
            Assert.That(overlayMode, Does.Contain("Disabled"));
            Assert.That(overlayMode, Does.Contain("Container"));
            Assert.That(overlayMode, Does.Contain("Window"));
            Assert.That(options, Does.Contain("public string InitialTheme { get; set; } = \"light\";"));
            Assert.That(options, Does.Contain("public DocumentEditorOverlayMode OverlayMode { get; set; } = DocumentEditorOverlayMode.Window;"));
            Assert.That(runtime, Does.Contain("documentEditor.setTheme"));
            Assert.That(runtime, Does.Contain("documentEditor.setOverlayMode"));
            Assert.That(runtime, Does.Contain("public string Theme => _theme;"));
            Assert.That(runtime, Does.Contain("public event EventHandler<string> ThemeChanged;"));
            Assert.That(runtime, Does.Contain("CreateToolbarPayload"));
            Assert.That(workspace, Does.Contain("theme = NormalizeTheme(_options.InitialTheme)"));
            Assert.That(workspace, Does.Contain("overlayMode = ToMessageOverlayMode(_options.OverlayMode)"));
            Assert.That(workspace, Does.Contain("command = ToMessageCommand(item.Command)"));
            Assert.That(yamlControl, Does.Contain("ThemeProperty"));
            Assert.That(yamlDocumentHost, Does.Contain("ThemeProperty"));
            Assert.That(yamlDocumentHost, Does.Contain("_layoutRenderer.Render(RuntimeDocumentState, Theme, LanguageCode)"));
            Assert.That(yamlDocumentHost, Does.Contain("ApplyDocumentEditorThemeAsync(NormalizeTheme((string)e.NewValue))"));
            Assert.That(yamlLayoutRenderer, Does.Contain("_fieldControlFactory.Create(runtimeField, theme, languageCode)"));
            Assert.That(yamlFieldFactory, Does.Contain("Theme = NormalizeTheme(theme)"));
            Assert.That(yamlFieldFactory, Does.Contain("LanguageCode = NormalizeLanguageCode(languageCode)"));
            Assert.That(yamlControl, Does.Contain("private readonly Grid _root;"));
            Assert.That(yamlControl, Does.Contain("new DocumentEditorOptions"));
            Assert.That(yamlControl, Does.Contain("OverlayMode = DocumentEditorOverlayMode.Container"));
            Assert.That(yamlControl, Does.Contain("HorizontalAlignment = HorizontalAlignment.Stretch;"));
            Assert.That(yamlControl, Does.Contain("UseLayoutRounding = true;"));
            Assert.That(yamlControl, Does.Contain("SnapsToDevicePixels = true;"));
            Assert.That(yamlControl, Does.Contain("private readonly Border _fieldChromeBorder;"));
            Assert.That(yamlControl, Does.Contain("private readonly Grid _contentPanel;"));
            Assert.That(yamlControl, Does.Contain("_fieldChromeBorder.Child = _contentPanel;"));
            Assert.That(yamlControl, Does.Contain("_root.Children.Add(_fieldChromeBorder);"));
            Assert.That(yamlControl, Does.Contain("_root.Children.Add(_contentPanel);"));
            Assert.That(yamlControl, Does.Contain("_fieldChromeBorder.Visibility = chromeMode == FieldChromeMode.Framed ? Visibility.Visible : Visibility.Collapsed;"));
            Assert.That(yamlControl, Does.Not.Contain("YamlTextBox.ClearButtonStyle"));
            Assert.That(yamlControl, Does.Not.Contain("YamlTextBox.RestoreButtonStyle"));
            Assert.That(yamlControl, Does.Not.Contain("BorderThickness = new Thickness(0, 0, 0, 1)"));
            Assert.That(yamlControl, Does.Not.Contain("CornerRadius = new CornerRadius(0)"));
            Assert.That(yamlControl, Does.Contain("MinWidth = 0d,"));
            Assert.That(yamlControl, Does.Contain("public Task ApplyExternalThemeAsync(string theme)"));
            Assert.That(yamlControl, Does.Contain("_editor.ThemeChanged += HandleEditorThemeChanged;"));
            Assert.That(yamlControl, Does.Contain("await _editor.SetOverlayModeAsync(state.OverlayMode).ConfigureAwait(true);"));
            Assert.That(yamlControl, Does.Contain("await _editor.SetDocumentJsonAsync(string.Empty).ConfigureAwait(true);"));
            Assert.That(yamlRuntimeBinding, Does.Not.Contain("RestoreOldValue"));
            Assert.That(yamlRuntimeBindingState, Does.Not.Contain("ShowOldValueRestoreButton"));
            Assert.That(yamlRuntimeBindingState, Does.Not.Contain("OldValue"));
            Assert.That(yamlControl, Does.Contain("SetCurrentValue(ThemeProperty, normalizedTheme);"));
            Assert.That(yamlControl, Does.Contain("await ApplyThemeCoreAsync(Theme, true).ConfigureAwait(true);"));
            Assert.That(wpfControl, Does.Not.Contain("RuntimeFieldStateProperty"));
            Assert.That(wpfControl, Does.Not.Contain("_captionText"));
            Assert.That(wpfControl, Does.Not.Contain("_supportText"));
            Assert.That(showcase, Does.Contain("await _editor.SetThemeAsync(_theme).ConfigureAwait(true);"));
            Assert.That(hostedYamlFactory, Does.Contain("public DemoYamlHostedSurfaceFactory(Func<string> themeProvider)"));
            Assert.That(hostedYamlFactory, Does.Contain("Theme = ResolveTheme()"));
            Assert.That(codeBehind, Does.Contain("new DemoYamlHostedSurfaceFactory(ResolveReportDesignerTheme)"));
            Assert.That(codeBehind, Does.Contain("await ApplyYamlAdvancedDocumentEditorThemeAsync().ConfigureAwait(true);"));
            Assert.That(codeBehind, Does.Contain("await YamlAdvancedInlineDocumentEditorControl.ApplyExternalThemeAsync(theme).ConfigureAwait(true);"));
            Assert.That(codeBehind, Does.Contain("await YamlAdvancedFramedDocumentEditorControl.ApplyExternalThemeAsync(theme).ConfigureAwait(true);"));
            Assert.That(xaml, Does.Contain("x:Name=\"YamlAdvancedInlineDocumentEditorControl\""));
            Assert.That(xaml, Does.Contain("x:Name=\"YamlAdvancedFramedDocumentEditorControl\""));
            Assert.That(xaml, Does.Contain("webHost:OverlayHost.IsScope=\"True\""));
            Assert.That(yamlAdvancedOverlayScopeCount, Is.EqualTo(1));
            Assert.That(xaml, Does.Contain("HorizontalAlignment=\"Stretch\""));
            Assert.That(xaml, Does.Contain("UseLayoutRounding=\"True\""));
            Assert.That(xaml, Does.Contain("SnapsToDevicePixels=\"True\""));
            Assert.That(html, Does.Contain("[data-theme='dark']"));
            Assert.That(html, Does.Contain("html[data-theme='dark']"));
            Assert.That(html, Does.Contain("body[data-theme='dark']"));
            Assert.That(html, Does.Contain("#editor-shell {"));
            Assert.That(html, Does.Contain("border: 1px solid var(--de-border);"));
            Assert.That(html, Does.Contain("border-radius: 12px;"));
            Assert.That(html, Does.Contain("overflow: hidden;"));
            Assert.That(html, Does.Contain("padding: 16px 10px 8px 10px;"));
            Assert.That(html, Does.Contain("max-width: calc(100% - 20px);"));
            Assert.That(html, Does.Not.Contain("padding: 16px 76px 8px 10px;"));
            Assert.That(html, Does.Not.Contain("max-width: calc(100% - 96px);"));
            Assert.That(html, Does.Not.Contain("toolbar-spacer"));
            Assert.That(script, Does.Contain("toolbar-button__icon"));
            Assert.That(script, Does.Contain("ICONS"));
            Assert.That(script, Does.Contain("applyTheme"));
            Assert.That(script, Does.Contain("function notifyReady()"));
            Assert.That(script, Does.Contain("window.PhialeWebHost.notifyReady()"));
            Assert.That(script, Does.Contain("window.PhialeWebHost.onHostMessage = handleMessage;"));
            Assert.That(script, Does.Contain("bootstrap.overlayMode"));
            Assert.That(script, Does.Contain("overlayMode !== \"disabled\""));
            Assert.That(script, Does.Not.Contain("const overlayClose = document.createElement(\"button\");"));
            Assert.That(script, Does.Not.Contain("const clearButton = document.createElement(\"button\");"));
            Assert.That(script, Does.Not.Contain("const restoreButton = document.createElement(\"button\");"));
            Assert.That(script, Does.Not.Contain("clearButton.innerHTML = ICONS.close;"));
            Assert.That(script, Does.Not.Contain("restoreButton.innerHTML = ICONS.undo;"));
            Assert.That(script, Does.Not.Contain("clearDocumentContent()"));
            Assert.That(script, Does.Not.Contain("restoreBaselineDocument()"));
            Assert.That(script, Does.Not.Contain("baselineDocumentJson"));
            Assert.That(script, Does.Not.Contain("documentEditor.clear"));
            Assert.That(script, Does.Contain("undo: \"undo\""));
            Assert.That(script, Does.Contain("redo: \"redo\""));
            Assert.That(script, Does.Not.Contain("toggleTheme"));
            Assert.That(script, Does.Not.Contain("themeToggle"));
            Assert.That(script, Does.Not.Contain("getThemeToggleIcon"));
            Assert.That(script, Does.Contain("theme: bootstrap.theme"));
            Assert.That(script, Does.Contain("window.PhialeWebHost.onHostMessage = handleMessage;"));
            Assert.That(script, Does.Contain("window.addEventListener(\"phiale-webhost-bridge-ready\", bindHostBridge);"));
            Assert.That(script, Does.Contain("window.addEventListener(\"phiale-webhost-message\", (event) => handleMessage(event.detail));"));
            Assert.That(script, Does.Contain("window.PhialeWebHost.postMessage(message);"));
            Assert.That(script, Does.Contain("window.chrome.webview.postMessage(JSON.stringify(message));"));
            Assert.That(script, Does.Contain("document.documentElement.dataset.theme"));
            Assert.That(script, Does.Contain("document.body.dataset.theme"));
            Assert.That(script, Does.Contain("button.addEventListener(\"mousedown\""));
            Assert.That(script, Does.Contain("event.preventDefault();"));
            Assert.That(script, Does.Contain("button.addEventListener(\"keydown\""));
        }

        [Test]
        public void DocumentEditor_ToolbarSupportsDefaultAdvancedActions_AndPersistedUserVisibility()
        {
            var commands = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Abstractions", "PhialeTech.DocumentEditor.Abstractions", "DocumentEditorCommand.cs"));
            var toolbarConfig = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Abstractions", "PhialeTech.DocumentEditor.Abstractions", "DocumentEditorToolbarConfig.cs"));
            var runtime = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Core", "PhialeTech.DocumentEditor", "DocumentEditorRuntime.cs"));
            var workspace = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Core", "PhialeTech.DocumentEditor", "DocumentEditorWorkspace.cs"));
            var wpfControl = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "DocumentEditor", "Platforms", "Wpf", "PhialeTech.DocumentEditor.Wpf", "Controls", "PhialeDocumentEditor.cs"));
            var html = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.WebAssets", "Assets", "DocumentEditor", "index.html"));
            var script = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.WebAssets", "Assets", "DocumentEditor", "phiale-document-editor-host.js"));

            Assert.That(commands, Does.Contain("ExportHtml"));
            Assert.That(commands, Does.Contain("ExportMarkdown"));
            Assert.That(commands, Does.Contain("SaveJson"));
            Assert.That(commands, Does.Contain("LoadJson"));
            Assert.That(commands, Does.Not.Contain("Clear ="));
            Assert.That(runtime, Does.Not.Contain("ClearAsync"));
            Assert.That(runtime, Does.Not.Contain("documentEditor.clear"));
            Assert.That(wpfControl, Does.Not.Contain("ClearAsync"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.TextColor"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.HighlightColor"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.AlignLeft"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.AlignCenter"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.AlignRight"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.AlignJustify"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.ExportHtml"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.ExportMarkdown"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.SaveJson"));
            Assert.That(toolbarConfig, Does.Contain("DocumentEditorCommand.LoadJson"));
            Assert.That(runtime, Does.Contain("case DocumentEditorCommand.ExportHtml: return \"exportHtml\";"));
            Assert.That(runtime, Does.Contain("case DocumentEditorCommand.ExportMarkdown: return \"exportMarkdown\";"));
            Assert.That(runtime, Does.Contain("case DocumentEditorCommand.SaveJson: return \"saveJson\";"));
            Assert.That(runtime, Does.Contain("case DocumentEditorCommand.LoadJson: return \"loadJson\";"));
            Assert.That(workspace, Does.Contain("case DocumentEditorCommand.ExportHtml: return \"exportHtml\";"));
            Assert.That(workspace, Does.Contain("case DocumentEditorCommand.ExportMarkdown: return \"exportMarkdown\";"));
            Assert.That(workspace, Does.Contain("case DocumentEditorCommand.SaveJson: return \"saveJson\";"));
            Assert.That(workspace, Does.Contain("case DocumentEditorCommand.LoadJson: return \"loadJson\";"));
            Assert.That(html, Does.Contain("toolbar-settings"));
            Assert.That(script, Does.Contain("TOOLBAR_VISIBILITY_STORAGE_KEY"));
            Assert.That(script, Does.Contain("phialetech.documentEditor.toolbar.hiddenCommands.v1"));
            Assert.That(script, Does.Contain("renderToolbarSettings"));
            Assert.That(script, Does.Contain("saveHiddenToolbarCommands"));
            Assert.That(script, Does.Contain("documentEditor.exportHtmlRequested"));
            Assert.That(script, Does.Contain("documentEditor.exportMarkdownRequested"));
            Assert.That(script, Does.Contain("documentEditor.saveJsonRequested"));
            Assert.That(script, Does.Contain("documentEditor.loadJsonRequested"));
            Assert.That(script, Does.Contain("document.addEventListener(\"mousedown\", closeToolbarSettingsWhenOutside);"));
            Assert.That(script, Does.Contain("document.addEventListener(\"keydown\", closeToolbarSettingsOnEscape);"));
            Assert.That(script, Does.Contain("event.stopPropagation();"));
            Assert.That(script, Does.Contain("renderColorPicker"));
            Assert.That(script, Does.Contain("COLOR_SWATCHES"));
            Assert.That(script, Does.Contain("toolbar-color-picker"));
            Assert.That(script, Does.Contain("input.type = \"color\";"));
            Assert.That(script, Does.Contain("LABELS_BY_LANGUAGE"));
            Assert.That(script, Does.Contain("documentEditor.setLanguage"));
            Assert.That(runtime, Does.Contain("NativeFileActionRequested"));
            Assert.That(wpfControl, Does.Contain("SaveFileDialog"));
            Assert.That(wpfControl, Does.Contain("OpenFileDialog"));
            Assert.That(wpfControl, Does.Contain("SetLanguageAsync"));
        }

        [Test]
        public void About_ListsDocumentEditorThirdPartyLicenses()
        {
            var licenseCatalog = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Services", "DemoLicenseCatalog.cs"));
            var english = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Languages", "en.lang"));
            var polish = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "Languages", "pl.lang"));

            Assert.That(licenseCatalog, Does.Contain("\"Tiptap OSS DocumentEditor\""));
            Assert.That(licenseCatalog, Does.Contain("Assets/DocumentEditor/ThirdPartyNotices.md"));
            Assert.That(licenseCatalog, Does.Contain("@tiptap/markdown"));
            Assert.That(english, Does.Contain("Example.ThirdPartyLicenses.Description=Lists the open-source notices and local license files required by the bundled PdfViewer, ReportDesigner, MonacoEditor, and DocumentEditor web assets."));
            Assert.That(polish, Does.Contain("Example.ThirdPartyLicenses.Description=Pokazuje noty open-source i lokalne pliki licencji wymagane przez zbundlowane web assety PdfViewera, ReportDesignera, MonacoEditora oraz DocumentEditora."));
        }

        private static string GetRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Repository root could not be located from the test output directory.");
            }

            return directory.FullName;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Rendering;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Wpf.Controls.Actions;
using PhialeTech.YamlApp.Wpf.Controls.Badges;
using PhialeTech.YamlApp.Wpf.Controls.Buttons;
using PhialeTech.YamlApp.Wpf.Controls.DocumentEditor;
using PhialeTech.WebHost.Wpf.Controls;

namespace PhialeTech.YamlApp.Wpf.Document
{
    public sealed class YamlDocumentHost : ContentControl
    {
        private readonly YamlDocumentLayoutRenderer _layoutRenderer;
        private readonly DocumentActionRenderPlanBuilder _actionRenderPlanBuilder;

        public static readonly DependencyProperty RuntimeDocumentStateProperty =
            DependencyProperty.Register(
                nameof(RuntimeDocumentState),
                typeof(RuntimeDocumentState),
                typeof(YamlDocumentHost),
                new FrameworkPropertyMetadata(null, OnRuntimeDocumentStateChanged));

        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register(
                nameof(ShowTitle),
                typeof(bool),
                typeof(YamlDocumentHost),
                new FrameworkPropertyMetadata(true, OnRuntimeDocumentStateChanged));

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register(
                nameof(Theme),
                typeof(string),
                typeof(YamlDocumentHost),
                new FrameworkPropertyMetadata("light", OnThemeChanged));

        public static readonly DependencyProperty LanguageCodeProperty =
            DependencyProperty.Register(
                nameof(LanguageCode),
                typeof(string),
                typeof(YamlDocumentHost),
                new FrameworkPropertyMetadata("en", OnLanguageCodeChanged));

        public static readonly RoutedEvent ActionInvokedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ActionInvoked),
                RoutingStrategy.Bubble,
                typeof(EventHandler<YamlDocumentActionInvokedEventArgs>),
                typeof(YamlDocumentHost));

        static YamlDocumentHost()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(YamlDocumentHost), new FrameworkPropertyMetadata(typeof(YamlDocumentHost)));
        }

        public YamlDocumentHost()
            : this(new YamlDocumentLayoutRenderer(), new DocumentActionRenderPlanBuilder())
        {
        }

        public YamlDocumentHost(YamlDocumentLayoutRenderer layoutRenderer, DocumentActionRenderPlanBuilder actionRenderPlanBuilder)
        {
            _layoutRenderer = layoutRenderer ?? throw new ArgumentNullException(nameof(layoutRenderer));
            _actionRenderPlanBuilder = actionRenderPlanBuilder ?? throw new ArgumentNullException(nameof(actionRenderPlanBuilder));
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
        }

        public RuntimeDocumentState RuntimeDocumentState
        {
            get => (RuntimeDocumentState)GetValue(RuntimeDocumentStateProperty);
            set => SetValue(RuntimeDocumentStateProperty, value);
        }

        public bool ShowTitle
        {
            get => (bool)GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        public string Theme
        {
            get => (string)GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        public string LanguageCode
        {
            get => (string)GetValue(LanguageCodeProperty);
            set => SetValue(LanguageCodeProperty, value);
        }

        public event EventHandler<YamlDocumentActionInvokedEventArgs> ActionInvoked
        {
            add => AddHandler(ActionInvokedEvent, value);
            remove => RemoveHandler(ActionInvokedEvent, value);
        }

        private static void OnRuntimeDocumentStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlDocumentHost)d).RebuildVisualTree();
        }

        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (YamlDocumentHost)d;
            _ = host.ApplyDocumentEditorThemeAsync(NormalizeTheme((string)e.NewValue));
        }

        private static void OnLanguageCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (YamlDocumentHost)d;
            _ = host.ApplyDocumentEditorLanguageAsync(NormalizeLanguageCode((string)e.NewValue));
        }

        private void RebuildVisualTree()
        {
            try
            {
                var previousContent = Content;
                if (previousContent != null)
                {
                    Content = null;
                }

                if (RuntimeDocumentState == null)
                {
                    return;
                }

                YamlWpfPresentationHelper.ApplyPresentation(
                    this,
                    RuntimeDocumentState.Document == null ? null : RuntimeDocumentState.Document.Width,
                    RuntimeDocumentState.Document == null ? null : RuntimeDocumentState.Document.WidthHint,
                    RuntimeDocumentState.Visible,
                    RuntimeDocumentState.Enabled);
                VerticalAlignment = IsLayoutHeightAuto()
                    ? VerticalAlignment.Top
                    : VerticalAlignment.Stretch;

                var root = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                KeyboardNavigation.SetTabNavigation(root, KeyboardNavigationMode.Cycle);
                OverlayHost.SetIsScope(root, true);

                var chrome = new Border
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                ApplyBorderStyle(chrome, "YamlDocument.RootBorderStyle");

                var content = BuildContent();
                chrome.Child = content;
                root.Children.Add(chrome);
                Content = root;
            }
            catch (Exception ex)
            {
                Content = BuildErrorContent(ex);
            }
        }

        private UIElement BuildContent()
        {
            var plan = RuntimeDocumentState?.Document is Core.Resolved.ResolvedFormDocumentDefinition formDocument
                ? _actionRenderPlanBuilder.Build(formDocument)
                : null;
            var headerContent = BuildHeaderContent();
            var stickyTopAreas = EnumerateAreas(plan, ActionPlacement.Top, stickyOnly: true).ToList();
            var footerContent = BuildFooterContent();
            var stickyBottomAreas = EnumerateAreas(plan, ActionPlacement.Bottom, stickyOnly: true).ToList();
            var topActionContent = BuildShellActionPanelContent(stickyTopAreas, ActionPlacement.Top);
            var bottomActionContent = BuildShellActionPanelContent(stickyBottomAreas, ActionPlacement.Bottom);
            var layoutContent = BuildLayoutRegionContent(plan);

            var hasHeader = headerContent != null;
            var hasTopActionPanel = topActionContent != null;
            var hasBottomActionPanel = bottomActionContent != null;
            var hasFooter = footerContent != null;

            var isTopMerged = IsShellMergeEnabled(ActionPlacement.Top) && hasHeader && hasTopActionPanel;
            var isBottomMerged = IsShellMergeEnabled(ActionPlacement.Bottom) && hasFooter && hasBottomActionPanel;

            var shell = new Grid
            {
                Name = "ShellHost",
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            KeyboardNavigation.SetTabNavigation(shell, KeyboardNavigationMode.Continue);
            shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            shell.RowDefinitions.Add(new RowDefinition { Height = ResolveLayoutRegionHeight() });
            shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddShellRegion(
                shell,
                0,
                "Header",
                headerContent,
                hasHeader,
                "YamlDocument.HeaderShellRegionStyle",
                dividerOnTop: false);

            AddShellRegion(
                shell,
                1,
                "TopActionPanel",
                topActionContent,
                hasTopActionPanel,
                isTopMerged ? "YamlDocument.TopActionPanelMergedShellRegionStyle" : "YamlDocument.TopActionPanelShellRegionStyle",
                dividerOnTop: hasHeader && !isTopMerged);

            AddShellRegion(
                shell,
                2,
                "Layout",
                layoutContent,
                visible: true,
                "YamlDocument.LayoutShellRegionStyle",
                dividerOnTop: hasHeader || hasTopActionPanel);

            AddShellRegion(
                shell,
                3,
                "BottomActionPanel",
                bottomActionContent,
                hasBottomActionPanel,
                isBottomMerged ? "YamlDocument.BottomActionPanelMergedShellRegionStyle" : "YamlDocument.BottomActionPanelShellRegionStyle",
                dividerOnTop: hasBottomActionPanel);

            AddShellRegion(
                shell,
                4,
                "Footer",
                footerContent,
                hasFooter,
                "YamlDocument.FooterShellRegionStyle",
                dividerOnTop: hasFooter && (!hasBottomActionPanel || !isBottomMerged));

            return shell;
        }

        private GridLength ResolveLayoutRegionHeight()
        {
            return IsLayoutHeightAuto()
                ? GridLength.Auto
                : new GridLength(1d, GridUnitType.Star);
        }

        private bool IsLayoutHeightAuto()
        {
            return RuntimeDocumentState?.Document?.Layout?.HeightMode == LayoutHeightMode.Auto;
        }

        private FrameworkElement BuildShellActionPanelContent(IEnumerable<DocumentActionAreaRenderPlan> areaPlans, ActionPlacement placement)
        {
            var areas = (areaPlans ?? Enumerable.Empty<DocumentActionAreaRenderPlan>()).ToList();
            if (areas.Count == 0)
            {
                return null;
            }

            if (areas.Count == 1)
            {
                return BuildAreaActionContent(areas[0], ResolveAreaOrientation(areas[0]));
            }

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = placement == ActionPlacement.Bottom ? HorizontalAlignment.Right : HorizontalAlignment.Stretch,
            };
            KeyboardNavigation.SetTabNavigation(stack, KeyboardNavigationMode.Continue);

            for (var i = 0; i < areas.Count; i++)
            {
                var areaContent = BuildAreaActionContent(areas[i], ResolveAreaOrientation(areas[i]));
                if (areaContent != null)
                {
                    stack.Children.Add(i == 0 ? areaContent : WrapInBorder(areaContent, "YamlDocument.ActionAreaSpacerStyle"));
                }
            }

            return stack;
        }

        private bool IsShellMergeEnabled(ActionPlacement placement)
        {
            if (RuntimeDocumentState?.Document == null)
            {
                return false;
            }

            var mode = placement == ActionPlacement.Top
                ? RuntimeDocumentState.Document.TopRegionChrome
                : RuntimeDocumentState.Document.BottomRegionChrome;

            return mode == DocumentRegionChromeMode.Merged;
        }

        private UIElement BuildLayoutRegionContent(DocumentActionRenderPlan plan)
        {
            var scrollHost = new PhialeWebComponentScrollHost
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                HostedContent = BuildScrollableContent(plan),
            };
            KeyboardNavigation.SetTabNavigation(scrollHost, KeyboardNavigationMode.Continue);
            scrollHost.SetResourceReference(FrameworkElement.StyleProperty, "YamlDocument.WebComponentScrollHostStyle");

            var center = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            center.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            center.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
            center.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            KeyboardNavigation.SetTabNavigation(center, KeyboardNavigationMode.Continue);

            var leftAreas = BuildSideAreas(plan, ActionPlacement.Left);
            if (leftAreas != null)
            {
                Grid.SetColumn(leftAreas, 0);
                center.Children.Add(leftAreas);
            }

            Grid.SetColumn(scrollHost, 1);
            center.Children.Add(scrollHost);

            var rightAreas = BuildSideAreas(plan, ActionPlacement.Right);
            if (rightAreas != null)
            {
                Grid.SetColumn(rightAreas, 2);
                center.Children.Add(rightAreas);
            }

            return center;
        }

        private void AddShellRegion(Grid host, int row, string elementName, UIElement content, bool visible, string styleKey, bool dividerOnTop)
        {
            var region = BuildShellRegion(elementName, content, visible, styleKey, dividerOnTop);
            Grid.SetRow(region, row);
            host.Children.Add(region);
        }

        private Border BuildShellRegion(string elementName, UIElement content, bool visible, string styleKey, bool dividerOnTop)
        {
            var region = new Border
            {
                Name = elementName,
                Visibility = visible ? Visibility.Visible : Visibility.Collapsed,
                Child = content,
            };
            ApplyBorderStyle(region, dividerOnTop ? styleKey + ".WithDivider" : styleKey);
            return region;
        }

        private void ApplyBorderStyle(Border border, string styleKey)
        {
            if (border == null)
            {
                return;
            }

            var resolvedStyle = TryFindResource(styleKey) as Style;
            if (resolvedStyle != null)
            {
                border.Style = resolvedStyle;
                return;
            }

            border.SetResourceReference(FrameworkElement.StyleProperty, styleKey);
        }

        private static bool HasStickyArea(DocumentActionRenderPlan plan, ActionPlacement placement)
        {
            return plan?.Areas?.Any(areaPlan =>
                areaPlan.Area != null &&
                areaPlan.Area.Sticky &&
                areaPlan.Area.Placement == placement) == true;
        }

        private UIElement BuildScrollableContent(DocumentActionRenderPlan plan)
        {
            var stack = new StackPanel();
            ApplyElementStyle(stack, "YamlDocument.LayoutFlowPanelStyle");
            KeyboardNavigation.SetTabNavigation(stack, KeyboardNavigationMode.Continue);

            AddInFlowAreas(stack, plan, ActionPlacement.Top);
            stack.Children.Add(_layoutRenderer.Render(RuntimeDocumentState, Theme, LanguageCode));
            AddInFlowAreas(stack, plan, ActionPlacement.Bottom);

            return stack;
        }

        private async Task ApplyDocumentEditorThemeAsync(string theme)
        {
            foreach (var editor in FindDocumentEditors())
            {
                await editor.ApplyExternalThemeAsync(theme).ConfigureAwait(true);
            }
        }

        private async Task ApplyDocumentEditorLanguageAsync(string languageCode)
        {
            foreach (var editor in FindDocumentEditors())
            {
                await editor.ApplyExternalLanguageAsync(languageCode).ConfigureAwait(true);
            }
        }

        private IList<YamlDocumentEditor> FindDocumentEditors()
        {
            var editors = new List<YamlDocumentEditor>();
            CollectDocumentEditors(this, editors);
            return editors;
        }

        private static void CollectDocumentEditors(DependencyObject current, IList<YamlDocumentEditor> editors)
        {
            if (current == null)
            {
                return;
            }

            var editor = current as YamlDocumentEditor;
            if (editor != null && !editors.Contains(editor))
            {
                editors.Add(editor);
            }

            var visualChildren = 0;
            try
            {
                visualChildren = VisualTreeHelper.GetChildrenCount(current);
            }
            catch (InvalidOperationException)
            {
            }

            for (var index = 0; index < visualChildren; index++)
            {
                CollectDocumentEditors(VisualTreeHelper.GetChild(current, index), editors);
            }

            foreach (var logicalChild in LogicalTreeHelper.GetChildren(current))
            {
                var dependencyChild = logicalChild as DependencyObject;
                if (dependencyChild != null)
                {
                    CollectDocumentEditors(dependencyChild, editors);
                }
            }
        }

        private UIElement BuildActionArea(DocumentActionAreaRenderPlan areaPlan)
        {
            var orientation = ResolveAreaOrientation(areaPlan);
            var actionContent = BuildAreaActionContent(areaPlan, orientation);

            return new YamlDocumentActionAreaHost
            {
                AlignmentMode = areaPlan.Area == null ? ActionAlignment.Right : areaPlan.Area.HorizontalAlignment,
                ChromeMode = areaPlan.Area == null ? ActionAreaChromeMode.Explicit : areaPlan.Area.ChromeMode,
                IsSharedArea = areaPlan.Area != null && areaPlan.Area.Shared,
                IsSticky = areaPlan.Area != null && areaPlan.Area.Sticky,
                Placement = areaPlan.Area == null ? ActionPlacement.Top : areaPlan.Area.Placement,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = actionContent,
                Name = ResolveActionAreaHostName(areaPlan),
            };
        }

        private static string ResolveActionAreaHostName(DocumentActionAreaRenderPlan areaPlan)
        {
            if (areaPlan?.Area == null)
            {
                return "ActionAreaHost";
            }

            var suffix = string.IsNullOrWhiteSpace(areaPlan.Area.Id)
                ? string.Empty
                : "_" + SanitizeElementName(areaPlan.Area.Id);

            switch (areaPlan.Area.Placement)
            {
                case ActionPlacement.Top:
                    return "TopActionArea" + suffix;
                case ActionPlacement.Bottom:
                    return "BottomActionArea" + suffix;
                case ActionPlacement.Left:
                    return "LeftActionArea" + suffix;
                case ActionPlacement.Right:
                    return "RightActionArea" + suffix;
                default:
                    return "ActionAreaHost" + suffix;
            }
        }

        private static string SanitizeElementName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Region";
            }

            var buffer = new char[value.Length];
            var length = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (char.IsLetterOrDigit(current) || current == '_')
                {
                    buffer[length++] = current;
                }
            }

            if (length == 0)
            {
                return "Region";
            }

            if (!char.IsLetter(buffer[0]) && buffer[0] != '_')
            {
                return "Region_" + new string(buffer, 0, length);
            }

            return new string(buffer, 0, length);
        }

        private FrameworkElement BuildAreaActionContent(DocumentActionAreaRenderPlan areaPlan, Orientation orientation)
        {
            var actions = ResolveRuntimeActions(areaPlan.Actions).ToList();
            var hasExplicitSlots = actions.Any(action => action.Action != null && action.Action.Slot.HasValue);
            var buttonVariant = ResolveActionButtonVariant(areaPlan, orientation);

            if (!hasExplicitSlots)
            {
                return BuildActionStack(
                    actions,
                    orientation,
                    areaPlan.Area == null ? ActionAlignment.Right : areaPlan.Area.HorizontalAlignment,
                    buttonVariant);
            }

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            KeyboardNavigation.SetTabNavigation(grid, KeyboardNavigationMode.Continue);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Auto) });

            var startPanel = BuildActionStack(actions.Where(action => ResolveSlot(action) == ActionSlot.Start), orientation, ActionAlignment.Left, buttonVariant);
            var centerPanel = BuildActionStack(actions.Where(action => ResolveSlot(action) == ActionSlot.Center), orientation, ActionAlignment.Center, buttonVariant);
            var endPanel = BuildActionStack(actions.Where(action => ResolveSlot(action) == ActionSlot.End), orientation, ActionAlignment.Right, buttonVariant);

            if (startPanel.Children.Count > 0)
            {
                Grid.SetColumn(startPanel, 0);
                grid.Children.Add(startPanel);
            }

            if (centerPanel.Children.Count > 0)
            {
                Grid.SetColumn(centerPanel, 1);
                grid.Children.Add(centerPanel);
            }

            if (endPanel.Children.Count > 0)
            {
                Grid.SetColumn(endPanel, 2);
                grid.Children.Add(endPanel);
            }

            return grid;
        }

        private StackPanel BuildActionStack(IEnumerable<RuntimeActionState> actions, Orientation orientation, ActionAlignment alignment, ButtonVariant buttonVariant)
        {
            var panel = new StackPanel
            {
                Orientation = orientation,
                HorizontalAlignment = orientation == Orientation.Horizontal
                    ? MapHorizontalAlignment(alignment)
                    : HorizontalAlignment.Stretch,
                VerticalAlignment = orientation == Orientation.Vertical
                    ? MapVerticalAlignment(alignment)
                    : VerticalAlignment.Center,
            };
            KeyboardNavigation.SetTabNavigation(panel, KeyboardNavigationMode.Continue);

            var actionList = (actions ?? Enumerable.Empty<RuntimeActionState>()).ToList();
            for (var index = 0; index < actionList.Count; index++)
            {
                var button = BuildActionButton(actionList[index], orientation, buttonVariant);
                if (button != null)
                {
                    ApplyElementStyle(button, ResolveActionButtonStyleKey(orientation, index < actionList.Count - 1));
                    panel.Children.Add(button);
                }
            }

            return panel;
        }

        private IEnumerable<RuntimeActionState> ResolveRuntimeActions(IReadOnlyList<Core.Resolved.ResolvedDocumentActionDefinition> actions)
        {
            if (actions == null || RuntimeDocumentState == null)
            {
                return Enumerable.Empty<RuntimeActionState>();
            }

            var runtimeActions = RuntimeDocumentState.Actions ?? Array.Empty<RuntimeActionState>();
            var runtimeActionMap = runtimeActions
                .Where(action => action != null && !string.IsNullOrWhiteSpace(action.Id))
                .ToDictionary(action => action.Id, StringComparer.OrdinalIgnoreCase);

            return actions
                .Select(action =>
                {
                    if (action == null || string.IsNullOrWhiteSpace(action.Id))
                    {
                        return null;
                    }

                    runtimeActionMap.TryGetValue(action.Id, out var runtimeAction);
                    return runtimeAction;
                })
                .Where(action => action != null && action.Visible);
        }

        private YamlDocumentActionButton BuildActionButton(RuntimeActionState actionState, Orientation areaOrientation, ButtonVariant buttonVariant)
        {
            if (actionState == null)
            {
                return null;
            }

            var action = actionState.Action;
            var isPrimaryAction = action != null && (action.IsPrimary || action.ActionKind == DocumentActionKind.Ok || action.ActionKind == DocumentActionKind.Finish || action.ActionKind == DocumentActionKind.Apply || action.ActionKind == DocumentActionKind.Next);
            var button = new YamlDocumentActionButton
            {
                Content = ResolveActionCaption(actionState),
                CommandId = actionState.Id ?? string.Empty,
                IconKey = ResolveActionIconKey(actionState, buttonVariant),
                Semantic = action == null ? null : action.Semantic,
                IsPrimaryAction = isPrimaryAction,
                Tone = ResolveActionTone(actionState, isPrimaryAction, buttonVariant),
                Variant = buttonVariant,
                Size = ResolveActionButtonSize(buttonVariant),
                IsEnabled = actionState.Enabled,
                Tag = actionState,
            };

            button.Invoked += OnActionButtonInvoked;
            return button;
        }

        private void AddDockedAreas(DockPanel shell, DocumentActionRenderPlan plan, ActionPlacement placement, bool stickyOnly)
        {
            foreach (var areaPlan in EnumerateAreas(plan, placement, stickyOnly))
            {
                var area = BuildActionArea(areaPlan);
                DockPanel.SetDock(area, placement == ActionPlacement.Top ? Dock.Top : Dock.Bottom);
                shell.Children.Add(area);
            }
        }

        private void AddInFlowAreas(Panel stack, DocumentActionRenderPlan plan, ActionPlacement placement)
        {
            foreach (var areaPlan in EnumerateAreas(plan, placement, stickyOnly: false))
            {
                stack.Children.Add(BuildActionArea(areaPlan));
            }
        }

        private UIElement BuildSideAreas(DocumentActionRenderPlan plan, ActionPlacement placement)
        {
            var areas = EnumerateAreas(plan, placement, stickyOnly: null).ToList();
            if (areas.Count == 0)
            {
                return null;
            }

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            ApplyElementStyle(stack, placement == ActionPlacement.Left ? "YamlDocument.SideAreaStackStyle.Left" : "YamlDocument.SideAreaStackStyle.Right");
            KeyboardNavigation.SetTabNavigation(stack, KeyboardNavigationMode.Continue);

            foreach (var areaPlan in areas)
            {
                stack.Children.Add(BuildActionArea(areaPlan));
            }

            return stack;
        }

        private IEnumerable<DocumentActionAreaRenderPlan> EnumerateAreas(DocumentActionRenderPlan plan, ActionPlacement placement, bool? stickyOnly)
        {
            if (plan?.Areas == null)
            {
                return Enumerable.Empty<DocumentActionAreaRenderPlan>();
            }

            return plan.Areas.Where(areaPlan =>
            {
                if (areaPlan?.Area == null || areaPlan.Actions == null || areaPlan.Actions.Count == 0)
                {
                    return false;
                }

                if (areaPlan.Area.Placement != placement)
                {
                    return false;
                }

                if (stickyOnly.HasValue && areaPlan.Area.Sticky != stickyOnly.Value)
                {
                    return false;
                }

                return true;
            });
        }

        private void OnActionButtonInvoked(object sender, YamlButtonInvokedEventArgs e)
        {
            if (!(sender is YamlDocumentActionButton button) || !(button.Tag is RuntimeActionState actionState) || RuntimeDocumentState == null)
            {
                return;
            }

            RaiseEvent(new YamlDocumentActionInvokedEventArgs(ActionInvokedEvent, this, RuntimeDocumentState, actionState));
            e.Handled = true;
        }

        private static ButtonVariant ResolveActionButtonVariant(DocumentActionAreaRenderPlan areaPlan, Orientation orientation)
        {
            var placement = areaPlan?.Area == null ? ActionPlacement.Bottom : areaPlan.Area.Placement;
            switch (placement)
            {
                case ActionPlacement.Top:
                    return areaPlan?.Area != null && areaPlan.Area.ChromeMode == ActionAreaChromeMode.Explicit
                        ? ButtonVariant.ActionStrip
                        : ButtonVariant.Toolbar;
                case ActionPlacement.Bottom:
                    return ButtonVariant.ActionStrip;
                default:
                    return orientation == Orientation.Vertical ? ButtonVariant.Standard : ButtonVariant.Toolbar;
            }
        }

        private static ButtonSize ResolveActionButtonSize(ButtonVariant buttonVariant)
        {
            switch (buttonVariant)
            {
                case ButtonVariant.Toolbar:
                    return ButtonSize.Compact;
                case ButtonVariant.ActionStrip:
                    return ButtonSize.Regular;
                default:
                    return ButtonSize.Regular;
            }
        }

        private static ButtonTone ResolveActionTone(RuntimeActionState actionState, bool isPrimaryAction, ButtonVariant buttonVariant)
        {
            if (isPrimaryAction)
            {
                return ButtonTone.Primary;
            }

            var semantic = actionState?.Action == null ? (ActionSemantic?)null : actionState.Action.Semantic;
            if (semantic == ActionSemantic.Help && buttonVariant == ButtonVariant.Toolbar)
            {
                return ButtonTone.Tertiary;
            }

            return ButtonTone.Secondary;
        }

        private static string ResolveActionIconKey(RuntimeActionState actionState, ButtonVariant buttonVariant)
        {
            if (!string.IsNullOrWhiteSpace(actionState?.Action?.IconKey))
            {
                return actionState.Action.IconKey;
            }

            var action = actionState?.Action;
            if (action?.ActionKind == DocumentActionKind.Ok)
            {
                return "ok";
            }

            if (action?.ActionKind == DocumentActionKind.Cancel)
            {
                return "cancel";
            }

            if (action?.ActionKind == DocumentActionKind.Apply)
            {
                return "apply";
            }

            if (action?.ActionKind == DocumentActionKind.Back)
            {
                return "back";
            }

            if (action?.ActionKind == DocumentActionKind.Next)
            {
                return "next";
            }

            if (action?.ActionKind == DocumentActionKind.Finish)
            {
                return "finish";
            }

            if (action?.Semantic == ActionSemantic.Help)
            {
                return "help";
            }

            var probe = string.Join(
                " ",
                new[]
                {
                    actionState?.Id,
                    actionState?.Name,
                    action?.CaptionKey,
                }.Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();

            if (probe.Contains("validate"))
            {
                return "validate";
            }

            if (probe.Contains("draft") || probe.Contains("save"))
            {
                return "save-draft";
            }

            if (probe.Contains("preview"))
            {
                return "preview";
            }

            if (probe.Contains("history"))
            {
                return "history";
            }

            if (probe.Contains("help"))
            {
                return "help";
            }

            return string.Empty;
        }

        private static string ResolveActionCaption(RuntimeActionState actionState)
        {
            var action = actionState?.Action;
            if (!string.IsNullOrWhiteSpace(action?.CaptionKey))
            {
                return action.CaptionKey;
            }

            if (!string.IsNullOrWhiteSpace(actionState?.Name))
            {
                return actionState.Name;
            }

            return actionState?.Id ?? "Action";
        }

        private static HorizontalAlignment MapHorizontalAlignment(ActionAlignment alignment)
        {
            switch (alignment)
            {
                case ActionAlignment.Left:
                    return HorizontalAlignment.Left;
                case ActionAlignment.Center:
                    return HorizontalAlignment.Center;
                case ActionAlignment.Stretch:
                    return HorizontalAlignment.Stretch;
                default:
                    return HorizontalAlignment.Right;
            }
        }

        private static VerticalAlignment MapVerticalAlignment(ActionAlignment alignment)
        {
            switch (alignment)
            {
                case ActionAlignment.Center:
                    return VerticalAlignment.Center;
                case ActionAlignment.Stretch:
                    return VerticalAlignment.Stretch;
                case ActionAlignment.Right:
                    return VerticalAlignment.Bottom;
                default:
                    return VerticalAlignment.Top;
            }
        }

        private static Orientation ResolveAreaOrientation(DocumentActionAreaRenderPlan areaPlan)
        {
            if (areaPlan?.Area == null)
            {
                return Orientation.Horizontal;
            }

            return areaPlan.Area.Placement == ActionPlacement.Left || areaPlan.Area.Placement == ActionPlacement.Right
                ? Orientation.Vertical
                : Orientation.Horizontal;
        }

        private static ActionSlot ResolveSlot(RuntimeActionState action)
        {
            if (action?.Action != null && action.Action.Slot.HasValue)
            {
                return action.Action.Slot.Value;
            }

            return ActionSlot.End;
        }

        private UIElement BuildHeaderContent()
        {
            if (RuntimeDocumentState == null)
            {
                return null;
            }

            var header = RuntimeDocumentState.Document == null ? null : RuntimeDocumentState.Document.Header;
            var isHeaderVisible = header == null || header.Visible;
            var titleText = header == null ? null : header.TitleKey;
            if (string.IsNullOrWhiteSpace(titleText) && ShowTitle)
            {
                titleText = RuntimeDocumentState.Name;
            }

            var subtitleText = header == null ? null : header.SubtitleKey;
            var descriptionText = header == null ? null : header.DescriptionKey;
            var statusText = header == null ? null : header.StatusKey;
            var contextText = header == null ? null : header.ContextKey;
            var iconText = header == null ? null : header.IconKey;

            if (!isHeaderVisible ||
                (string.IsNullOrWhiteSpace(titleText) &&
                 string.IsNullOrWhiteSpace(subtitleText) &&
                 string.IsNullOrWhiteSpace(descriptionText) &&
                 string.IsNullOrWhiteSpace(statusText) &&
                 string.IsNullOrWhiteSpace(contextText) &&
                 string.IsNullOrWhiteSpace(iconText)))
            {
                return null;
            }

            var stack = new StackPanel();
            ApplyElementStyle(stack, "YamlDocument.HeaderContentPanelStyle");

            var leadRow = BuildHeaderLeadRow(contextText, statusText, iconText);
            if (leadRow != null)
            {
                stack.Children.Add(leadRow);
            }

            if (!string.IsNullOrWhiteSpace(titleText))
            {
                var title = new TextBlock
                {
                    Text = titleText,
                };
                ApplyElementStyle(title, "YamlDocument.HeaderTitleTextStyle");
                stack.Children.Add(title);
            }

            if (!string.IsNullOrWhiteSpace(subtitleText))
            {
                var subtitle = new TextBlock
                {
                    Text = subtitleText,
                };
                ApplyElementStyle(subtitle, "YamlDocument.HeaderSubtitleTextStyle");
                stack.Children.Add(subtitle);
            }

            if (!string.IsNullOrWhiteSpace(descriptionText))
            {
                var description = new TextBlock
                {
                    Text = descriptionText,
                };
                ApplyElementStyle(description, "YamlDocument.HeaderDescriptionTextStyle");
                stack.Children.Add(description);
            }

            return stack;
        }

        private UIElement BuildFooterContent()
        {
            if (RuntimeDocumentState == null)
            {
                return null;
            }

            var footer = RuntimeDocumentState.Document == null ? null : RuntimeDocumentState.Document.Footer;
            if (footer == null || !footer.Visible)
            {
                return null;
            }

            var noteText = footer.NoteKey;
            var statusText = footer.StatusKey;
            var sourceText = footer.SourceKey;
            if (string.IsNullOrWhiteSpace(noteText) &&
                string.IsNullOrWhiteSpace(statusText) &&
                string.IsNullOrWhiteSpace(sourceText))
            {
                return null;
            }

            var grid = new Grid();
            ApplyElementStyle(grid, "YamlDocument.FooterContentGridStyle");
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftStack = new StackPanel();
            ApplyElementStyle(leftStack, "YamlDocument.FooterPrimaryStackStyle");

            if (!string.IsNullOrWhiteSpace(noteText))
            {
                var note = new TextBlock
                {
                    Text = noteText,
                };
                ApplyElementStyle(note, "YamlDocument.FooterNoteTextStyle");
                leftStack.Children.Add(note);
            }

            if (!string.IsNullOrWhiteSpace(sourceText))
            {
                var source = new TextBlock
                {
                    Text = sourceText,
                };
                ApplyElementStyle(source, "YamlDocument.FooterSupportTextStyle");
                leftStack.Children.Add(source);
            }

            if (leftStack.Children.Count > 0)
            {
                Grid.SetColumn(leftStack, 0);
                grid.Children.Add(leftStack);
            }

            if (!string.IsNullOrWhiteSpace(statusText))
            {
                var status = new TextBlock
                {
                    Text = statusText,
                };
                ApplyElementStyle(status, "YamlDocument.FooterStatusTextStyle");
                Grid.SetColumn(status, 1);
                grid.Children.Add(status);
            }

            return grid.Children.Count == 0 ? null : grid;
        }

        private UIElement BuildHeaderLeadRow(string contextText, string statusText, string iconText)
        {
            var hasLeftContent = !string.IsNullOrWhiteSpace(contextText) || !string.IsNullOrWhiteSpace(statusText);
            var hasRightContent = !string.IsNullOrWhiteSpace(iconText);
            if (!hasLeftContent && !hasRightContent)
            {
                return null;
            }

            var grid = new Grid();
            ApplyElementStyle(grid, "YamlDocument.HeaderLeadRowStyle");
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            if (hasLeftContent)
            {
                var metaPanel = new WrapPanel();
                ApplyElementStyle(metaPanel, "YamlDocument.HeaderMetaPanelStyle");

                if (!string.IsNullOrWhiteSpace(contextText))
                {
                    var context = new TextBlock
                    {
                        Text = contextText,
                    };
                    ApplyElementStyle(context, "YamlDocument.HeaderContextTextStyle");
                    metaPanel.Children.Add(context);
                }

                var badge = BuildStatusBadge(statusText);
                if (badge != null)
                {
                    metaPanel.Children.Add(badge);
                }

                Grid.SetColumn(metaPanel, 0);
                grid.Children.Add(metaPanel);
            }

            if (hasRightContent)
            {
                var icon = new TextBlock
                {
                    Text = iconText,
                };
                ApplyElementStyle(icon, "YamlDocument.HeaderIconTextStyle");
                Grid.SetColumn(icon, 1);
                grid.Children.Add(icon);
            }

            return grid;
        }

        private UIElement BuildStatusBadge(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var badge = new YamlBadge
            {
                Text = text,
            };
            ApplyElementStyle(badge, "YamlDocument.StatusBadgeStyle");
            return badge;
        }

        private UIElement BuildFooterSupportLine(string statusText, string sourceText)
        {
            var values = new[] { statusText, sourceText }
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();
            if (values.Count == 0)
            {
                return null;
            }

            var support = new TextBlock
            {
                Text = string.Join(" · ", values),
            };
            ApplyElementStyle(support, "YamlDocument.FooterSupportTextStyle");
            return support;
        }

        private UIElement BuildErrorContent(Exception exception)
        {
            var text = new TextBlock
            {
                Text = exception == null
                    ? "YamlDocumentHost failed to render."
                    : "YamlDocumentHost failed to render." + Environment.NewLine + exception.Message,
            };
            ApplyElementStyle(text, "YamlDocument.ErrorTextStyle");

            var border = new Border
            {
                Child = text,
            };
            ApplyBorderStyle(border, "YamlDocument.ErrorBorderStyle");
            return border;
        }

        private void ApplyElementStyle(FrameworkElement element, string styleKey)
        {
            if (element == null || string.IsNullOrWhiteSpace(styleKey))
            {
                return;
            }

            var resolvedStyle = TryFindResource(styleKey) as Style;
            if (resolvedStyle != null)
            {
                element.Style = resolvedStyle;
                return;
            }

            element.SetResourceReference(FrameworkElement.StyleProperty, styleKey);
        }

        private static string ResolveActionButtonStyleKey(Orientation orientation, bool hasFollowingSibling)
        {
            if (orientation == Orientation.Vertical)
            {
                return hasFollowingSibling
                    ? "YamlDocument.ActionButtonStyle.Vertical.Spaced"
                    : "YamlDocument.ActionButtonStyle.Vertical.Last";
            }

            return hasFollowingSibling
                ? "YamlDocument.ActionButtonStyle.Horizontal.Spaced"
                : "YamlDocument.ActionButtonStyle.Horizontal.Last";
        }

        private Border WrapInBorder(UIElement child, string styleKey)
        {
            var border = new Border
            {
                Child = child,
            };
            ApplyBorderStyle(border, styleKey);
            return border;
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(theme, "night", StringComparison.OrdinalIgnoreCase)
                ? "dark"
                : "light";
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return string.Equals(languageCode, "pl", StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";
        }

    }
}

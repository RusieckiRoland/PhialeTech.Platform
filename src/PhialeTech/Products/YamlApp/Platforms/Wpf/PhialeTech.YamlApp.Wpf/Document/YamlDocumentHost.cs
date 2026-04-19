using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Rendering;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Wpf.Controls.Actions;

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

        public event EventHandler<YamlDocumentActionInvokedEventArgs> ActionInvoked
        {
            add => AddHandler(ActionInvokedEvent, value);
            remove => RemoveHandler(ActionInvokedEvent, value);
        }

        private static void OnRuntimeDocumentStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlDocumentHost)d).RebuildVisualTree();
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

                var root = new DockPanel
                {
                    LastChildFill = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                KeyboardNavigation.SetTabNavigation(root, KeyboardNavigationMode.Cycle);

                var content = BuildContent();
                root.Children.Add(content);
                Content = root;
                FocusFirstFocusableElement(content as FrameworkElement ?? root);
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

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = BuildScrollableContent(plan),
            };
            KeyboardNavigation.SetTabNavigation(scrollViewer, KeyboardNavigationMode.Continue);
            scrollViewer.SetResourceReference(FrameworkElement.StyleProperty, "YamlDocument.ScrollViewerStyle");
            scrollViewer.PreviewMouseWheel += OnContentScrollViewerPreviewMouseWheel;

            var shell = new DockPanel
            {
                LastChildFill = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            KeyboardNavigation.SetTabNavigation(shell, KeyboardNavigationMode.Continue);

            AddDockedAreas(shell, plan, ActionPlacement.Top, stickyOnly: true);
            AddDockedAreas(shell, plan, ActionPlacement.Bottom, stickyOnly: true);

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

            Grid.SetColumn(scrollViewer, 1);
            center.Children.Add(scrollViewer);

            var rightAreas = BuildSideAreas(plan, ActionPlacement.Right);
            if (rightAreas != null)
            {
                Grid.SetColumn(rightAreas, 2);
                center.Children.Add(rightAreas);
            }

            shell.Children.Add(center);
            return shell;
        }

        private UIElement BuildScrollableContent(DocumentActionRenderPlan plan)
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            KeyboardNavigation.SetTabNavigation(stack, KeyboardNavigationMode.Continue);

            var title = BuildTitle();
            if (title != null)
            {
                stack.Children.Add(title);
            }

            AddInFlowAreas(stack, plan, ActionPlacement.Top);
            stack.Children.Add(_layoutRenderer.Render(RuntimeDocumentState));
            AddInFlowAreas(stack, plan, ActionPlacement.Bottom);

            return stack;
        }

        private UIElement BuildActionArea(DocumentActionAreaRenderPlan areaPlan)
        {
            var orientation = ResolveAreaOrientation(areaPlan);
            var actionContent = BuildAreaActionContent(areaPlan, orientation);

            return new YamlDocumentActionAreaHost
            {
                AlignmentMode = areaPlan.Area == null ? ActionAlignment.Right : areaPlan.Area.HorizontalAlignment,
                IsSharedArea = areaPlan.Area != null && areaPlan.Area.Shared,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = actionContent,
            };
        }

        private FrameworkElement BuildAreaActionContent(DocumentActionAreaRenderPlan areaPlan, Orientation orientation)
        {
            var actions = ResolveRuntimeActions(areaPlan.Actions).ToList();
            var hasExplicitSlots = actions.Any(action => action.Action != null && action.Action.Slot.HasValue);

            if (!hasExplicitSlots)
            {
                return BuildActionStack(
                    actions,
                    orientation,
                    areaPlan.Area == null ? ActionAlignment.Right : areaPlan.Area.HorizontalAlignment);
            }

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            KeyboardNavigation.SetTabNavigation(grid, KeyboardNavigationMode.Continue);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Auto) });

            var startPanel = BuildActionStack(actions.Where(action => ResolveSlot(action) == ActionSlot.Start), orientation, ActionAlignment.Left);
            var centerPanel = BuildActionStack(actions.Where(action => ResolveSlot(action) == ActionSlot.Center), orientation, ActionAlignment.Center);
            var endPanel = BuildActionStack(actions.Where(action => ResolveSlot(action) == ActionSlot.End), orientation, ActionAlignment.Right);

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

        private StackPanel BuildActionStack(IEnumerable<RuntimeActionState> actions, Orientation orientation, ActionAlignment alignment)
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

            foreach (var action in actions ?? Enumerable.Empty<RuntimeActionState>())
            {
                var button = BuildActionButton(action, orientation);
                if (button != null)
                {
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

        private YamlDocumentActionButton BuildActionButton(RuntimeActionState actionState, Orientation areaOrientation)
        {
            if (actionState == null)
            {
                return null;
            }

            var action = actionState.Action;
            var button = new YamlDocumentActionButton
            {
                Content = ResolveActionCaption(actionState),
                Semantic = action == null ? null : action.Semantic,
                IsPrimaryAction = action != null && (action.IsPrimary || action.ActionKind == DocumentActionKind.Ok || action.ActionKind == DocumentActionKind.Finish || action.ActionKind == DocumentActionKind.Apply),
                IsEnabled = actionState.Enabled,
                Margin = areaOrientation == Orientation.Horizontal
                    ? new Thickness(0, 0, 8, 0)
                    : new Thickness(0, 0, 0, 8),
                MinWidth = 108,
                Tag = actionState,
            };

            if (areaOrientation == Orientation.Vertical)
            {
                button.HorizontalAlignment = HorizontalAlignment.Stretch;
                button.MinWidth = 132;
            }

            button.Click += OnActionButtonClick;
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
                Margin = placement == ActionPlacement.Left
                    ? new Thickness(0, 0, 16, 0)
                    : new Thickness(16, 0, 0, 0),
            };
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

        private void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is YamlDocumentActionButton button) || !(button.Tag is RuntimeActionState actionState) || RuntimeDocumentState == null)
            {
                return;
            }

            RaiseEvent(new YamlDocumentActionInvokedEventArgs(ActionInvokedEvent, this, RuntimeDocumentState, actionState));
            e.Handled = true;
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

        private UIElement BuildTitle()
        {
            if (!ShowTitle || RuntimeDocumentState == null || string.IsNullOrWhiteSpace(RuntimeDocumentState.Name))
            {
                return null;
            }

            var title = new TextBlock
            {
                Text = RuntimeDocumentState.Name,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap,
            };
            title.SetResourceReference(FrameworkElement.StyleProperty, "YamlDocument.TitleTextStyle");
            return title;
        }

        private static void OnContentScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null || e == null || e.Handled)
            {
                return;
            }

            if (CanScrollInDirection(scrollViewer, e.Delta))
            {
                return;
            }

            var parentScrollViewer = FindParentScrollViewer(scrollViewer);
            if (parentScrollViewer == null)
            {
                return;
            }

            e.Handled = true;
            var forwardedEvent = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = scrollViewer,
            };

            parentScrollViewer.RaiseEvent(forwardedEvent);
        }

        private static bool CanScrollInDirection(ScrollViewer scrollViewer, int delta)
        {
            if (scrollViewer.ScrollableHeight <= 0)
            {
                return false;
            }

            if (delta > 0)
            {
                return scrollViewer.VerticalOffset > 0;
            }

            if (delta < 0)
            {
                return scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;
            }

            return false;
        }

        private static ScrollViewer FindParentScrollViewer(DependencyObject start)
        {
            var current = VisualTreeHelper.GetParent(start);
            while (current != null)
            {
                if (current is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private UIElement BuildErrorContent(Exception exception)
        {
            var text = new TextBlock
            {
                Text = exception == null
                    ? "YamlDocumentHost failed to render."
                    : "YamlDocumentHost failed to render." + Environment.NewLine + exception.Message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0),
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Danger.Text");

            return new Border
            {
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Child = text,
            };
        }

        private void FocusFirstFocusableElement(FrameworkElement root)
        {
            if (root == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsVisible || !IsEnabled)
                {
                    return;
                }

                root.UpdateLayout();

                var firstFocusable = FindFirstFocusableDescendant(root);
                if (firstFocusable != null)
                {
                    Keyboard.Focus(firstFocusable);
                    return;
                }

                root.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private static Control FindFirstFocusableDescendant(DependencyObject root)
        {
            if (root == null)
            {
                return null;
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childrenCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is Control control &&
                    control.Focusable &&
                    control.IsTabStop &&
                    control.IsVisible &&
                    control.IsEnabled)
                {
                    return control;
                }

                var nested = FindFirstFocusableDescendant(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}

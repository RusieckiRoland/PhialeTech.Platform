using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Wpf.Controls.Badges;
using PhialeTech.YamlApp.Wpf.Controls.Buttons;
using PhialeTech.WebHost.Wpf.Controls;

namespace PhialeTech.YamlApp.Wpf.Document
{
    public sealed class YamlDocumentLayoutRenderer
    {
        private readonly YamlFieldControlFactory _fieldControlFactory;

        public YamlDocumentLayoutRenderer()
            : this(new YamlFieldControlFactory())
        {
        }

        public YamlDocumentLayoutRenderer(YamlFieldControlFactory fieldControlFactory)
        {
            _fieldControlFactory = fieldControlFactory ?? throw new ArgumentNullException(nameof(fieldControlFactory));
        }

        public FrameworkElement Render(RuntimeDocumentState documentState)
        {
            if (documentState == null)
            {
                throw new InvalidOperationException("Runtime document state is required.");
            }

            if (documentState.Document == null)
            {
                throw new InvalidOperationException("Runtime document definition is required.");
            }

            if (documentState.Document.Layout == null)
            {
                return BuildVerticalItemsPanel(documentState, null, isCompactSpacing: false);
            }

            var root = BuildVerticalItemsPanel(documentState, documentState.Document.Layout.Items, isCompactSpacing: false);
            YamlWpfPresentationHelper.ApplyPresentation(
                root,
                documentState.Document.Layout.Width,
                documentState.Document.Layout.WidthHint,
                documentState.Document.Layout.Visible,
                documentState.Document.Layout.Enabled);
            OverlayHost.SetIsScope(root, documentState.Document.Layout.IsOverlayScope);

            return root;
        }

        private FrameworkElement BuildVerticalItemsPanel(RuntimeDocumentState documentState, System.Collections.Generic.IReadOnlyList<ResolvedLayoutItemDefinition> items, bool isCompactSpacing)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            if (items == null)
            {
                return panel;
            }

            for (var index = 0; index < items.Count; index++)
            {
                var element = BuildItem(documentState, items[index]);
                if (element == null)
                {
                    continue;
                }

                panel.Children.Add(index < items.Count - 1 ? WrapVerticalItem(element, isCompactSpacing) : element);
            }

            return panel;
        }

        private FrameworkElement BuildItem(RuntimeDocumentState documentState, ResolvedLayoutItemDefinition item)
        {
            if (item == null)
            {
                return null;
            }

            if (item is ResolvedFieldReferenceDefinition fieldReference)
            {
                return BuildFieldReference(documentState, fieldReference);
            }

            if (item is ResolvedContainerDefinition container)
            {
                return BuildContainer(documentState, container);
            }

            if (item is ResolvedBadgeDefinition badge)
            {
                return BuildBadge(badge);
            }

            if (item is ResolvedButtonDefinition button)
            {
                return BuildButton(button);
            }

            if (item is ResolvedColumnDefinition column)
            {
                return BuildColumn(documentState, column);
            }

            if (item is ResolvedRowDefinition row)
            {
                return BuildRow(documentState, row);
            }

            throw new NotSupportedException(string.Format("Unsupported layout item: {0}", item.GetType().Name));
        }

        private FrameworkElement BuildFieldReference(RuntimeDocumentState documentState, ResolvedFieldReferenceDefinition fieldReference)
        {
            var runtimeField = documentState.GetField(fieldReference.FieldRef);
            var control = _fieldControlFactory.Create(runtimeField);
            ApplyLayoutItemPresentation(control, fieldReference);

            if (HasLocalSizing(fieldReference))
            {
                control.UpdateLayout();
            }

            return control;
        }

        private FrameworkElement BuildContainer(RuntimeDocumentState documentState, ResolvedContainerDefinition container)
        {
            var contentStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
            };

            if (!string.IsNullOrWhiteSpace(container.CaptionKey))
            {
                var caption = new TextBlock
                {
                    Text = container.CaptionKey,
                };
                caption.SetResourceReference(
                    FrameworkElement.StyleProperty,
                    container.Variant == ContainerVariant.Compact
                        ? "YamlDocument.ContainerCaptionTextStyle.Compact"
                        : "YamlDocument.ContainerCaptionTextStyle");
                contentStack.Children.Add(caption);
            }

            var itemsPanel = BuildVerticalItemsPanel(documentState, container.Items, container.Variant == ContainerVariant.Compact);
            contentStack.Children.Add(itemsPanel);

            FrameworkElement element;
            if (container.ShowBorder)
            {
                var border = new Border
                {
                    Child = contentStack,
                };
                border.SetResourceReference(
                    FrameworkElement.StyleProperty,
                    container.Variant == ContainerVariant.Compact
                        ? "YamlDocument.ContainerBorderStyle.Compact"
                        : "YamlDocument.ContainerBorderStyle");
                element = border;
            }
            else
            {
                element = contentStack;
            }

            ApplyLayoutItemPresentation(element, container);
            OverlayHost.SetIsScope(element, container.IsOverlayScope);
            return element;
        }

        private FrameworkElement BuildBadge(ResolvedBadgeDefinition badge)
        {
            var element = new YamlBadge
            {
                Text = badge.TextKey ?? string.Empty,
                IconKey = badge.IconKey,
                ToolTip = badge.ToolTipKey,
                Tone = badge.Tone,
                Variant = badge.Variant,
                Size = badge.Size,
                IconPlacement = badge.IconPlacement,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            ApplyLayoutItemPresentation(element, badge);
            return element;
        }

        private FrameworkElement BuildButton(ResolvedButtonDefinition button)
        {
            var element = new YamlButton
            {
                Content = button.TextKey ?? string.Empty,
                IconKey = button.IconKey,
                ToolTip = button.ToolTipKey,
                CommandId = button.CommandId,
                Tone = button.Tone,
                Variant = button.Variant,
                Size = button.Size,
                IconPlacement = button.IconPlacement,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            ApplyLayoutItemPresentation(element, button);
            return element;
        }

        private FrameworkElement BuildColumn(RuntimeDocumentState documentState, ResolvedColumnDefinition column)
        {
            var panel = BuildVerticalItemsPanel(documentState, column.Items, isCompactSpacing: false);
            ApplyLayoutItemPresentation(panel, column);
            OverlayHost.SetIsScope(panel, column.IsOverlayScope);
            return panel;
        }

        private FrameworkElement BuildRow(RuntimeDocumentState documentState, ResolvedRowDefinition row)
        {
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            if (row.Items == null || row.Items.Count == 0)
            {
                ApplyLayoutItemPresentation(grid, row);
                OverlayHost.SetIsScope(grid, row.IsOverlayScope);
                return grid;
            }

            for (var index = 0; index < row.Items.Count; index++)
            {
                var childDefinition = row.Items[index];
                var columnWidth = ResolveColumnWidth(childDefinition);
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = columnWidth });

                var child = BuildItem(documentState, childDefinition) ?? new Border();
                if (index < row.Items.Count - 1)
                {
                    var host = WrapRowItem(child);
                    Grid.SetColumn(host, index);
                    grid.Children.Add(host);
                }
                else
                {
                    Grid.SetColumn(child, index);
                    grid.Children.Add(child);
                }
            }

            ApplyLayoutItemPresentation(grid, row);
            OverlayHost.SetIsScope(grid, row.IsOverlayScope);
            return grid;
        }

        private static GridLength ResolveColumnWidth(ResolvedLayoutItemDefinition item)
        {
            if (item == null)
            {
                return GridLength.Auto;
            }

            var effectiveWidthHint = item.WidthHint;
            var effectiveWidth = item.Width;

            if (!effectiveWidthHint.HasValue && !effectiveWidth.HasValue && item is ResolvedFieldReferenceDefinition fieldReference && fieldReference.Field != null)
            {
                effectiveWidthHint = fieldReference.Field.WidthHint;
                effectiveWidth = fieldReference.Field.Width;
            }

            if (effectiveWidth.HasValue)
            {
                return GridLength.Auto;
            }

            var explicitWeight = ResolveWeight(item);
            if (explicitWeight.GetValueOrDefault() > 0d)
            {
                return new GridLength(explicitWeight.Value, GridUnitType.Star);
            }

            if (effectiveWidthHint == FieldWidthHint.Fill)
            {
                return new GridLength(1d, GridUnitType.Star);
            }

            return GridLength.Auto;
        }

        private static double? ResolveWeight(ResolvedLayoutItemDefinition item)
        {
            if (item is ResolvedFieldReferenceDefinition fieldReference)
            {
                return fieldReference.Field?.Weight;
            }

            return null;
        }

        private static void ApplyLayoutItemPresentation(FrameworkElement element, ResolvedLayoutItemDefinition item)
        {
            if (element == null || item == null)
            {
                return;
            }

            if (item.Width.HasValue || item.WidthHint.HasValue)
            {
                YamlWpfPresentationHelper.ApplyPresentation(element, item.Width, item.WidthHint, item.Visible, item.Enabled);
                return;
            }

            element.Visibility = item.Visible ? Visibility.Visible : Visibility.Collapsed;
            element.IsEnabled = item.Enabled;
        }

        private static bool HasLocalSizing(ResolvedLayoutItemDefinition item)
        {
            return item != null && (item.Width.HasValue || item.WidthHint.HasValue);
        }

        private static Border WrapVerticalItem(UIElement child, bool isCompactSpacing)
        {
            var border = new Border
            {
                Child = child,
            };
            border.SetResourceReference(
                FrameworkElement.StyleProperty,
                isCompactSpacing
                    ? "YamlDocument.LayoutItemHostStyle.Compact"
                    : "YamlDocument.LayoutItemHostStyle");
            return border;
        }

        private static Border WrapRowItem(UIElement child)
        {
            var border = new Border
            {
                Child = child,
            };
            border.SetResourceReference(FrameworkElement.StyleProperty, "YamlDocument.RowItemHostStyle");
            return border;
        }
    }
}

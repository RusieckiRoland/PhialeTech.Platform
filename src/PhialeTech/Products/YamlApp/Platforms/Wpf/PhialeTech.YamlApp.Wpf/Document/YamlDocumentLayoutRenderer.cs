using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Runtime.Model;

namespace PhialeTech.YamlApp.Wpf.Document
{
    public sealed class YamlDocumentLayoutRenderer
    {
        private const double VerticalItemSpacing = 16d;
        private const double HorizontalItemSpacing = 16d;
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

            if (documentState.Document == null || documentState.Document.Layout == null)
            {
                throw new InvalidOperationException("Document layout is required.");
            }

            var root = BuildVerticalItemsPanel(documentState, documentState.Document.Layout.Items);
            YamlWpfPresentationHelper.ApplyPresentation(
                root,
                documentState.Document.Layout.Width,
                documentState.Document.Layout.WidthHint,
                documentState.Document.Layout.Visible,
                documentState.Document.Layout.Enabled);

            return root;
        }

        private FrameworkElement BuildVerticalItemsPanel(RuntimeDocumentState documentState, System.Collections.Generic.IReadOnlyList<ResolvedLayoutItemDefinition> items)
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

                if (index < items.Count - 1)
                {
                    element.Margin = MergeMargins(element.Margin, new Thickness(0, 0, 0, VerticalItemSpacing));
                }

                panel.Children.Add(element);
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
                    Margin = new Thickness(0, 0, 0, 12),
                    TextWrapping = TextWrapping.Wrap,
                };
                caption.SetResourceReference(FrameworkElement.StyleProperty, "YamlDocument.ContainerCaptionTextStyle");
                contentStack.Children.Add(caption);
            }

            var itemsPanel = BuildVerticalItemsPanel(documentState, container.Items);
            contentStack.Children.Add(itemsPanel);

            FrameworkElement element;
            if (container.ShowBorder)
            {
                var border = new Border
                {
                    Child = contentStack,
                };
                border.SetResourceReference(FrameworkElement.StyleProperty, "YamlDocument.ContainerBorderStyle");
                element = border;
            }
            else
            {
                element = contentStack;
            }

            ApplyLayoutItemPresentation(element, container);
            return element;
        }

        private FrameworkElement BuildColumn(RuntimeDocumentState documentState, ResolvedColumnDefinition column)
        {
            var panel = BuildVerticalItemsPanel(documentState, column.Items);
            ApplyLayoutItemPresentation(panel, column);
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
                return grid;
            }

            for (var index = 0; index < row.Items.Count; index++)
            {
                var childDefinition = row.Items[index];
                var columnWidth = ResolveColumnWidth(childDefinition);
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = columnWidth });

                var child = BuildItem(documentState, childDefinition) ?? new Border();
                Grid.SetColumn(child, index);
                child.Margin = MergeMargins(child.Margin, new Thickness(0, 0, index < row.Items.Count - 1 ? HorizontalItemSpacing : 0, 0));
                grid.Children.Add(child);
            }

            ApplyLayoutItemPresentation(grid, row);
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

        private static Thickness MergeMargins(Thickness current, Thickness addition)
        {
            return new Thickness(
                current.Left + addition.Left,
                current.Top + addition.Top,
                current.Right + addition.Right,
                current.Bottom + addition.Bottom);
        }
    }
}

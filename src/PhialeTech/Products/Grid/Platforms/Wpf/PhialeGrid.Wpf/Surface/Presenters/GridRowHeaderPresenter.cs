using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PhialeGrid.Core.Surface;

namespace PhialeTech.PhialeGrid.Wpf.Surface.Presenters
{
    /// <summary>
    /// Presenter dla nagłówka wiersza.
    /// </summary>
    public sealed class GridRowHeaderPresenter : ContentControl
    {
        public GridRowHeaderPresenter()
        {
            SetValue(ClipToBoundsProperty, true);
            SetValue(UseLayoutRoundingProperty, true);
            SetValue(SnapsToDevicePixelsProperty, true);
            SetResourceReference(StyleProperty, "PgGridSurfaceHeaderPresenterStyle");
            SetResourceReference(BackgroundProperty, "PgHeaderBackgroundBrush");
            SetResourceReference(BorderBrushProperty, "PgHeaderBorderBrush");
            SetResourceReference(BorderThicknessProperty, "PgGridRowHeaderBorderThickness");
            SetValue(VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
            SetValue(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
            SetValue(PaddingProperty, new Thickness(0));
            AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(DebugObserveRowHeaderPreviewMouseDown), true);
        }

        public GridHeaderSurfaceItem HeaderData
        {
            get { return (GridHeaderSurfaceItem)GetValue(HeaderDataProperty); }
            set { SetValue(HeaderDataProperty, value); }
        }

        public static readonly DependencyProperty HeaderDataProperty =
            DependencyProperty.Register(
                nameof(HeaderData),
                typeof(GridHeaderSurfaceItem),
                typeof(GridRowHeaderPresenter),
                new PropertyMetadata(null, OnHeaderDataChanged));

        public GridBounds Bounds
        {
            get { return (GridBounds)GetValue(BoundsProperty); }
            set { SetValue(BoundsProperty, value); }
        }

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds),
                typeof(GridBounds),
                typeof(GridRowHeaderPresenter),
                new PropertyMetadata(GridBounds.Empty, OnBoundsChanged));

        private static void OnHeaderDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridRowHeaderPresenter)d;
            var headerData = (GridHeaderSurfaceItem)e.NewValue;

            if (headerData == null)
            {
                presenter.Content = null;
                AutomationProperties.SetName(presenter, string.Empty);
                AutomationProperties.SetAutomationId(presenter, string.Empty);
                return;
            }

            AutomationProperties.SetAutomationId(
                presenter,
                headerData.Kind == GridHeaderKind.RowNumberHeader
                    ? "surface.row-number-header." + headerData.HeaderKey
                    : "surface.row-header." + headerData.HeaderKey);
            AutomationProperties.SetName(presenter, BuildAutomationName(headerData));
            presenter.Content = BuildHeaderContent(headerData);
            ApplyPresenterChrome(presenter, headerData);

            if (headerData.IsSelected)
            {
                presenter.SetResourceReference(BackgroundProperty, "PgHeaderSelectedBackgroundBrush");
            }
            else if (headerData.IsCurrentRow)
            {
                presenter.SetResourceReference(BackgroundProperty, "PgCurrentRowBackgroundBrush");
            }
            else
            {
                presenter.SetResourceReference(BackgroundProperty, "PgHeaderBackgroundBrush");
            }
        }

        private static FrameworkElement BuildHeaderContent(GridHeaderSurfaceItem headerData)
        {
            if (headerData.Kind == GridHeaderKind.RowNumberHeader)
            {
                return BuildRowNumberHeaderContent(headerData);
            }

            var host = new Grid
            {
                SnapsToDevicePixels = true,
                ClipToBounds = true,
            };

            var columnIndex = 0;

            if (headerData.RowIndicatorWidth > 0d)
            {
                host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(System.Math.Max(0d, headerData.RowIndicatorWidth)) });
                var indicatorSlot = CreateSlotBorder(headerData.IsCurrentRow);
                AutomationProperties.SetAutomationId(indicatorSlot, "surface.row-indicator." + headerData.HeaderKey);
                AutomationProperties.SetName(indicatorSlot, "Row indicator " + headerData.RowIndicatorState);
                indicatorSlot.ToolTip = string.IsNullOrWhiteSpace(headerData.RowIndicatorToolTip) ? null : headerData.RowIndicatorToolTip;
                AttachIndicatorClickPopupBehavior(indicatorSlot, headerData.RowIndicatorToolTip);
                ToolTipService.SetInitialShowDelay(indicatorSlot, 0);
                ToolTipService.SetShowDuration(indicatorSlot, 30000);
                ToolTipService.SetPlacement(indicatorSlot, PlacementMode.Right);
                ToolTipService.SetHorizontalOffset(indicatorSlot, 10d);
                ToolTipService.SetVerticalOffset(indicatorSlot, 0d);
                indicatorSlot.Child = BuildIndicatorGlyph(headerData.ShowRowIndicator ? headerData.RowIndicatorState : GridRowIndicatorState.Empty);
                Grid.SetColumn(indicatorSlot, columnIndex++);
                host.Children.Add(indicatorSlot);
            }

            if (headerData.SelectionCheckboxWidth > 0d)
            {
                host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(System.Math.Max(0d, headerData.SelectionCheckboxWidth)) });
                var checkboxSlot = CreateSlotBorder(headerData.IsCurrentRow);
                AutomationProperties.SetAutomationId(checkboxSlot, "surface.row-checkbox-slot." + headerData.HeaderKey);
                AutomationProperties.SetName(checkboxSlot, "Row checkbox");
                checkboxSlot.Child = headerData.ShowSelectionCheckbox
                    ? BuildSelectionCheckboxContent(headerData)
                    : (FrameworkElement)new Border { Background = Brushes.Transparent };
                Grid.SetColumn(checkboxSlot, columnIndex++);
                host.Children.Add(checkboxSlot);
            }

            if (columnIndex == 0)
            {
                return host;
            }

            return host;
        }

        private static void AttachIndicatorClickPopupBehavior(Border indicatorSlot, string toolTipText)
        {
            if (indicatorSlot == null || string.IsNullOrWhiteSpace(toolTipText))
            {
                return;
            }

            var popup = CreateIndicatorClickPopup(indicatorSlot, toolTipText);
            indicatorSlot.Unloaded += (sender, args) => popup.IsOpen = false;
            indicatorSlot.AddHandler(
                UIElement.PreviewMouseLeftButtonUpEvent,
                new MouseButtonEventHandler((sender, args) =>
                {
                    popup.IsOpen = !popup.IsOpen;
                    args.Handled = true;
                }),
                true);
        }

        private static Popup CreateIndicatorClickPopup(Border indicatorSlot, string toolTipText)
        {
            var textBlock = new TextBlock
            {
                Text = toolTipText ?? string.Empty,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 360d,
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PgHeaderTextBrush");

            var contentBorder = new Border
            {
                Child = textBlock,
                Padding = new Thickness(10d, 8d, 10d, 8d),
                CornerRadius = new CornerRadius(4d),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            contentBorder.SetResourceReference(Border.BackgroundProperty, "PgHeaderBackgroundBrush");
            contentBorder.SetResourceReference(Border.BorderBrushProperty, "PgDetailsBorderLightBrush");
            contentBorder.BorderThickness = new Thickness(1d);

            return new Popup
            {
                PlacementTarget = indicatorSlot,
                Placement = PlacementMode.Right,
                HorizontalOffset = 10d,
                VerticalOffset = 0d,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = contentBorder,
            };
        }

        private static void ApplyPresenterChrome(GridRowHeaderPresenter presenter, GridHeaderSurfaceItem headerData)
        {
            if (presenter == null || headerData == null)
            {
                return;
            }

            presenter.SetResourceReference(BorderBrushProperty, "PgDetailsBorderLightBrush");
            presenter.BorderThickness = headerData.Kind == GridHeaderKind.RowNumberHeader
                ? new Thickness(0, 0, 1, 1)
                : new Thickness(0, 0, 0, 1);
        }

        private static Border CreateSlotBorder(bool isCurrentRow)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            border.SetResourceReference(Border.BorderBrushProperty, "PgDetailsBorderLightBrush");
            border.SetResourceReference(Border.BackgroundProperty, isCurrentRow ? "PgRowIndicatorColumnCurrentBackgroundBrush" : "PgRowIndicatorColumnBackgroundBrush");
            return border;
        }

        private static FrameworkElement BuildRowNumberHeaderContent(GridHeaderSurfaceItem headerData)
        {
            var slot = CreateRowNumberSlotBorder(headerData.IsCurrentRow);
            AutomationProperties.SetAutomationId(slot, "surface.row-number." + headerData.HeaderKey);
            AutomationProperties.SetName(slot, "Row number");
            slot.Child = headerData.ShowRowNumber
                ? BuildRowNumberGlyph(headerData.RowNumberText)
                : (FrameworkElement)new Border { Background = Brushes.Transparent };
            return slot;
        }

        private static Border CreateRowNumberSlotBorder(bool isCurrentRow)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            border.SetResourceReference(Border.BorderBrushProperty, "PgDetailsBorderLightBrush");
            border.SetResourceReference(
                Border.BackgroundProperty,
                isCurrentRow ? "PgRowIndicatorColumnCurrentBackgroundBrush" : "PgRowIndicatorColumnBackgroundBrush");
            return border;
        }

        private static FrameworkElement BuildSelectionCheckboxContent(GridHeaderSurfaceItem headerData)
        {
            var checkbox = BuildCheckboxGlyph(headerData.IsSelectionCheckboxChecked);
            AutomationProperties.SetAutomationId(checkbox, "surface.row-selector." + headerData.HeaderKey);
            return checkbox;
        }

        private static FrameworkElement BuildIndicatorGlyph(GridRowIndicatorState state)
        {
            switch (state)
            {
                case GridRowIndicatorState.Invalid:
                    return BuildInvalidGlyph();
                case GridRowIndicatorState.Edited:
                    return BuildEditedGlyph();
                case GridRowIndicatorState.Editing:
                    return BuildEditingGlyph();
                case GridRowIndicatorState.EditingAndEdited:
                    return BuildEditingAndEditedGlyph();
                case GridRowIndicatorState.EditingAndInvalid:
                    return BuildEditingAndInvalidGlyph();
                case GridRowIndicatorState.EditingAndEditedAndInvalid:
                    return BuildEditingAndEditedAndInvalidGlyph();
                case GridRowIndicatorState.CurrentAndInvalid:
                    return BuildCurrentAndInvalidGlyph();
                case GridRowIndicatorState.CurrentAndEdited:
                    return BuildCurrentAndEditedGlyph();
                case GridRowIndicatorState.Current:
                    return BuildCurrentGlyph();
                default:
                    return new Border { Background = Brushes.Transparent };
            }
        }

        private static FrameworkElement BuildCheckboxGlyph(bool isChecked)
        {
            var host = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 14,
                Height = 14,
                Background = Brushes.Transparent,
                Focusable = false,
                IsHitTestVisible = false,
            };

            var box = new Border
            {
                CornerRadius = new CornerRadius(2),
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
            };
            box.SetResourceReference(Border.BorderBrushProperty, "PgHeaderBorderBrush");
            host.Children.Add(box);

            if (isChecked)
            {
                box.SetResourceReference(Border.BackgroundProperty, "PgSelectionBackgroundBrush");
                box.SetResourceReference(Border.BorderBrushProperty, "PgAccentBrush");

                var check = new Path
                {
                    Data = Geometry.Parse("M 2 7 L 5.5 10.5 L 12 3.5"),
                    StrokeThickness = 2.0,
                    Stretch = Stretch.Fill,
                    Margin = new Thickness(1),
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round,
                    IsHitTestVisible = false,
                };
                check.SetResourceReference(Path.StrokeProperty, "PgSelectionTextBrush");
                host.Children.Add(check);
            }

            return host;
        }

        private static FrameworkElement BuildRowNumberGlyph(string rowNumberText)
        {
            var text = new TextBlock
            {
                Text = rowNumberText ?? string.Empty,
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                IsHitTestVisible = false,
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, "PgRowNumberTextBrush");
            return text;
        }

        private static FrameworkElement BuildCurrentGlyph()
        {
            var host = new Grid
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
            };

            var glyph = new Polygon
            {
                Width = 8,
                Height = 10,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Points = new PointCollection
                {
                    new Point(0, 0),
                    new Point(8, 5),
                    new Point(0, 10),
                },
            };
            glyph.SetResourceReference(Shape.FillProperty, "PgAccentBrush");
            host.Children.Add(glyph);
            return host;
        }

        private static FrameworkElement BuildEditedGlyph(bool useSemiTransparentInnerFill = false)
        {
            var host = new Grid
            {
                Width = 11,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };

            if (useSemiTransparentInnerFill)
            {
                var innerFill = new Path
                {
                    Data = Geometry.Parse("M 3.4 10.0 L 8.5 4.9 L 7.3 3.7 L 2.2 8.8 Z"),
                    Stretch = Stretch.Fill,
                    Fill = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)),
                };
                host.Children.Add(innerFill);
            }

            var glyph = new Path
            {
                Data = Geometry.Parse("M 1.5 10.8 L 1.2 13.1 L 3.5 12.8 L 11.6 4.7 L 9.2 2.3 Z M 8.6 2.9 L 11 5.3 M 2 12.5 L 3.9 12.2"),
                Stretch = Stretch.Fill,
                StrokeThickness = 1.35,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
            };
            glyph.SetResourceReference(Path.StrokeProperty, "Brush.Warning.Border");
            glyph.Fill = Brushes.Transparent;
            host.Children.Add(glyph);
            return host;
        }

        private static FrameworkElement BuildEditingGlyph()
        {
            var host = new Grid
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
            };

            var glyph = new Border
            {
                Width = 2.4d,
                Height = 11.2d,
                CornerRadius = new CornerRadius(1.2d),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            glyph.SetResourceReference(Border.BackgroundProperty, "PgAccentBrush");
            host.Children.Add(glyph);
            return host;
        }

        private static FrameworkElement BuildInvalidGlyph()
        {
            var host = new Grid
            {
                Width = 8,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
            };

            var stem = new Rectangle
            {
                Width = 3.2,
                Height = 8.4,
                RadiusX = 1.4,
                RadiusY = 1.4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0.4, 0, 0),
            };
            stem.SetResourceReference(Shape.FillProperty, "Brush.Danger.Border");
            host.Children.Add(stem);

            var dot = new Ellipse
            {
                Width = 3.1,
                Height = 3.1,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 0.2),
            };
            dot.SetResourceReference(Shape.FillProperty, "Brush.Danger.Border");
            host.Children.Add(dot);

            return host;
        }

        private static FrameworkElement BuildCurrentAndEditedGlyph()
        {
            return BuildCompositeIndicatorGlyph(
                CreateCompositeGlyphTriangle(),
                BuildEditedGlyph(useSemiTransparentInnerFill: true),
                triangleLeft: 1.8,
                triangleTop: 2.3,
                pencilLeft: 9.8,
                pencilTop: 1.3,
                pencilWidth: 8.1,
                pencilHeight: 11.1);
        }

        private static FrameworkElement BuildCurrentAndInvalidGlyph()
        {
            return BuildCompositeIndicatorGlyph(
                CreateCompositeGlyphTriangle(),
                BuildInvalidGlyph(),
                triangleLeft: 1.8,
                triangleTop: 2.3,
                pencilLeft: 10.2,
                pencilTop: 1.1,
                pencilWidth: 4.8,
                pencilHeight: 11.4);
        }

        private static FrameworkElement BuildEditingAndEditedGlyph()
        {
            return BuildCompositeIndicatorGlyph(
                BuildEditingGlyph(),
                BuildEditedGlyph(useSemiTransparentInnerFill: true),
                triangleLeft: 2.6,
                triangleTop: 1.1,
                pencilLeft: 8.8,
                pencilTop: 1.3,
                pencilWidth: 8.1,
                pencilHeight: 11.1);
        }

        private static FrameworkElement BuildEditingAndInvalidGlyph()
        {
            return BuildCompositeIndicatorGlyph(
                BuildEditingGlyph(),
                BuildInvalidGlyph(),
                triangleLeft: 2.6,
                triangleTop: 1.1,
                pencilLeft: 10.0,
                pencilTop: 1.1,
                pencilWidth: 4.8,
                pencilHeight: 11.4);
        }

        private static FrameworkElement BuildEditingAndEditedAndInvalidGlyph()
        {
            var host = new Canvas
            {
                Width = 22,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                ClipToBounds = false,
            };

            var editGlyph = BuildEditingGlyph();
            Canvas.SetLeft(editGlyph, 2.4d);
            Canvas.SetTop(editGlyph, 1.1d);
            host.Children.Add(editGlyph);

            var editedGlyph = BuildEditedGlyph(useSemiTransparentInnerFill: true);
            editedGlyph.Width = 7.8d;
            editedGlyph.Height = 10.8d;
            Canvas.SetLeft(editedGlyph, 8.2d);
            Canvas.SetTop(editedGlyph, 1.4d);
            host.Children.Add(editedGlyph);

            var invalidGlyph = BuildInvalidGlyph();
            invalidGlyph.Width = 4.2d;
            invalidGlyph.Height = 10.8d;
            Canvas.SetLeft(invalidGlyph, 16.6d);
            Canvas.SetTop(invalidGlyph, 1.4d);
            host.Children.Add(invalidGlyph);

            return host;
        }

        private static FrameworkElement CreateCompositeGlyphTriangle()
        {
            var triangle = new Polygon
            {
                Width = 6.9,
                Height = 9.4,
                Stretch = Stretch.Fill,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                Points = new PointCollection
                {
                    new Point(0, 0),
                    new Point(6.9, 4.7),
                    new Point(0, 9.4),
                },
            };
            triangle.SetResourceReference(Shape.FillProperty, "PgAccentBrush");
            return triangle;
        }

        private static FrameworkElement BuildCompositeIndicatorGlyph(
            FrameworkElement triangleGlyph,
            FrameworkElement secondaryGlyph,
            double triangleLeft,
            double triangleTop,
            double pencilLeft,
            double pencilTop,
            double pencilWidth,
            double pencilHeight)
        {
            var host = new Canvas
            {
                Width = 20,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                ClipToBounds = false,
            };

            if (triangleGlyph != null)
            {
                Canvas.SetLeft(triangleGlyph, triangleLeft);
                Canvas.SetTop(triangleGlyph, triangleTop);
                host.Children.Add(triangleGlyph);
            }

            if (secondaryGlyph != null)
            {
                secondaryGlyph.Width = pencilWidth;
                secondaryGlyph.Height = pencilHeight;
                Canvas.SetLeft(secondaryGlyph, pencilLeft);
                Canvas.SetTop(secondaryGlyph, pencilTop);
                host.Children.Add(secondaryGlyph);
            }

            return host;
        }

        private static string BuildAutomationName(GridHeaderSurfaceItem headerData)
        {
            if (headerData == null)
            {
                return string.Empty;
            }

            return headerData.Kind == GridHeaderKind.RowNumberHeader
                ? "Row number header " + headerData.HeaderKey
                : "Row state header " + headerData.HeaderKey + " " + headerData.RowIndicatorState;
        }

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridRowHeaderPresenter)d;
            var bounds = (GridBounds)e.NewValue;

            Canvas.SetLeft(presenter, bounds.X);
            Canvas.SetTop(presenter, bounds.Y);
            presenter.Width = bounds.Width;
            presenter.Height = bounds.Height;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GridRowHeaderPresenterAutomationPeer(this);
        }

        private void DebugObserveRowHeaderPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            var originalSource = e.OriginalSource as DependencyObject;
            var source = e.Source as DependencyObject;
            var directlyOver = Mouse.DirectlyOver as DependencyObject;
            var headerKey = HeaderData?.HeaderKey;

            _ = originalSource;
            _ = source;
            _ = directlyOver;
            _ = headerKey;
            _ = e.Handled;
            _ = e.RoutedEvent;
        }

        private sealed class GridRowHeaderPresenterAutomationPeer : FrameworkElementAutomationPeer
        {
            public GridRowHeaderPresenterAutomationPeer(GridRowHeaderPresenter owner)
                : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.HeaderItem;
            }

            protected override string GetClassNameCore()
            {
                return nameof(GridRowHeaderPresenter);
            }

            protected override string GetNameCore()
            {
                var owner = (GridRowHeaderPresenter)Owner;
                return AutomationProperties.GetName(owner) ?? base.GetNameCore();
            }
        }
    }
}

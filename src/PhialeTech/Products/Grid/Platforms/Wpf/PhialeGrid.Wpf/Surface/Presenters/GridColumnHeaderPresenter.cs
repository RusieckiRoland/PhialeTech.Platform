using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeGrid.Core.Surface;

namespace PhialeTech.PhialeGrid.Wpf.Surface.Presenters
{
    /// <summary>
    /// Presenter dla nagłówka kolumny.
    /// </summary>
    public sealed class GridColumnHeaderPresenter : ContentControl
    {
        public GridColumnHeaderPresenter()
        {
            this.SetValue(ClipToBoundsProperty, true);
            this.SetResourceReference(StyleProperty, "PgGridSurfaceHeaderPresenterStyle");
            this.SetResourceReference(BackgroundProperty, "PgHeaderBackgroundBrush");
            this.SetResourceReference(BorderBrushProperty, "PgHeaderBorderBrush");
            this.SetResourceReference(BorderThicknessProperty, "PgGridColumnHeaderBorderThickness");
            this.SetValue(VerticalContentAlignmentProperty, VerticalAlignment.Center);
            this.SetValue(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
            this.SetValue(PaddingProperty, new Thickness(0));
        }

        /// <summary>
        /// Dane nagłówka z snapshotu.
        /// </summary>
        public GridHeaderSurfaceItem HeaderData
        {
            get { return (GridHeaderSurfaceItem)GetValue(HeaderDataProperty); }
            set { SetValue(HeaderDataProperty, value); }
        }

        public static readonly DependencyProperty HeaderDataProperty =
            DependencyProperty.Register(
                nameof(HeaderData),
                typeof(GridHeaderSurfaceItem),
                typeof(GridColumnHeaderPresenter),
                new PropertyMetadata(null, OnHeaderDataChanged));

        private static void OnHeaderDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridColumnHeaderPresenter)d;
            var headerData = (GridHeaderSurfaceItem)e.NewValue;

            if (headerData == null)
            {
                presenter.Content = null;
                AutomationProperties.SetName(presenter, string.Empty);
                AutomationProperties.SetAutomationId(presenter, string.Empty);
                return;
            }

            AutomationProperties.SetAutomationId(presenter, "surface.column-header." + headerData.HeaderKey);
            AutomationProperties.SetName(presenter, BuildAutomationName(headerData));
            presenter.Content = BuildHeaderContent(presenter, headerData);

            if (headerData.IsSelected)
            {
                presenter.SetResourceReference(BackgroundProperty, "PgHeaderSelectedBackgroundBrush");
            }
            else
            {
                presenter.SetResourceReference(BackgroundProperty, "PgHeaderBackgroundBrush");
            }
        }

        /// <summary>
        /// Bounds nagłówka.
        /// </summary>
        public GridBounds Bounds
        {
            get { return (GridBounds)GetValue(BoundsProperty); }
            set { SetValue(BoundsProperty, value); }
        }

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds),
                typeof(GridBounds),
                typeof(GridColumnHeaderPresenter),
                new PropertyMetadata(GridBounds.Empty, OnBoundsChanged));

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridColumnHeaderPresenter)d;
            var bounds = (GridBounds)e.NewValue;
            
            Canvas.SetLeft(presenter, bounds.X);
            Canvas.SetTop(presenter, bounds.Y);
            presenter.Width = bounds.Width;
            presenter.Height = bounds.Height;
        }

        private static FrameworkElement BuildHeaderContent(GridColumnHeaderPresenter presenter, GridHeaderSurfaceItem headerData)
        {
            var glyph = GetGlyph(headerData.IconKey);
            var headerPadding = ResolveHeaderPadding(presenter);
            var container = new Grid();
            container.IsHitTestVisible = false;
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var layoutRoot = new Grid();
            layoutRoot.Margin = headerPadding;
            layoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            layoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(layoutRoot, 0);
            container.Children.Add(layoutRoot);

            var titleText = new TextBlock
            {
                Text = headerData.DisplayText ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            titleText.Style = ResolveRequiredStyle(presenter, "PgGridHeaderTextStyle");
            AutomationProperties.SetAutomationId(titleText, "surface.column-header." + headerData.HeaderKey + ".text");
            Grid.SetColumn(titleText, 0);
            layoutRoot.Children.Add(titleText);

            if (!string.IsNullOrEmpty(glyph) || !string.IsNullOrEmpty(headerData.SortOrderText))
            {
                var indicatorPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(indicatorPanel, 1);
                layoutRoot.Children.Add(indicatorPanel);

                if (!string.IsNullOrEmpty(glyph))
                {
                    var glyphText = new TextBlock
                    {
                        Text = glyph,
                        Margin = new Thickness(6, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    glyphText.Style = ResolveRequiredStyle(presenter, "PgGridSortGlyphTextStyle");
                    AutomationProperties.SetAutomationId(glyphText, "surface.column-header." + headerData.HeaderKey + ".sort-glyph");
                    indicatorPanel.Children.Add(glyphText);
                }

                if (!string.IsNullOrEmpty(headerData.SortOrderText))
                {
                    var orderBadge = new Border
                    {
                        Margin = new Thickness(4, 0, 0, 0),
                        Padding = new Thickness(4, 0, 4, 0),
                        CornerRadius = new CornerRadius(8),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    orderBadge.SetResourceReference(Border.BackgroundProperty, "PgSortBadgeBackgroundBrush");
                    orderBadge.SetResourceReference(Border.BorderBrushProperty, "PgSortBadgeBorderBrush");
                    orderBadge.BorderThickness = new Thickness(1);

                    var orderText = new TextBlock
                    {
                        Text = headerData.SortOrderText,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    orderText.SetResourceReference(TextBlock.ForegroundProperty, "PgSortBadgeTextBrush");
                    AutomationProperties.SetAutomationId(orderText, "surface.column-header." + headerData.HeaderKey + ".sort-order");
                    orderBadge.Child = orderText;
                    indicatorPanel.Children.Add(orderBadge);
                }
            }

            var separator = new Border
            {
                Width = 1,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0),
            };
            separator.SetResourceReference(Border.BackgroundProperty, "PgHeaderSeparatorBrush");
            Grid.SetColumn(separator, 1);
            container.Children.Add(separator);

            return container;
        }

        private static Thickness ResolveHeaderPadding(FrameworkElement owner)
        {
            if (owner?.TryFindResource("PgGridHeaderPadding") is Thickness thickness)
            {
                return thickness;
            }

            throw new InvalidOperationException("Missing required grid resource 'PgGridHeaderPadding'.");
        }

        private static string GetGlyph(string iconKey)
        {
            switch (iconKey)
            {
                case "sort-asc":
                    return "\u25b2";
                case "sort-desc":
                    return "\u25bc";
                default:
                    return string.Empty;
            }
        }

        private static string BuildAutomationName(GridHeaderSurfaceItem headerData)
        {
            var baseName = "Column header " + (headerData.DisplayText ?? string.Empty);
            var glyph = GetGlyph(headerData.IconKey);
            if (!string.IsNullOrEmpty(glyph) && string.IsNullOrEmpty(headerData.SortOrderText))
            {
                return baseName + " sorted";
            }

            if (!string.IsNullOrEmpty(glyph) && !string.IsNullOrEmpty(headerData.SortOrderText))
            {
                return baseName + " sort order " + headerData.SortOrderText;
            }

            return baseName;
        }

        private static Style ResolveRequiredStyle(FrameworkElement owner, string resourceKey)
        {
            if (owner?.TryFindResource(resourceKey) is Style style)
            {
                return style;
            }

            throw new InvalidOperationException("Missing required grid style '" + resourceKey + "'.");
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GridColumnHeaderPresenterAutomationPeer(this);
        }

        private sealed class GridColumnHeaderPresenterAutomationPeer : FrameworkElementAutomationPeer
        {
            public GridColumnHeaderPresenterAutomationPeer(GridColumnHeaderPresenter owner)
                : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.HeaderItem;
            }

            protected override string GetClassNameCore()
            {
                return nameof(GridColumnHeaderPresenter);
            }

            protected override string GetNameCore()
            {
                var owner = (GridColumnHeaderPresenter)Owner;
                return AutomationProperties.GetName(owner) ?? base.GetNameCore();
            }
        }
    }
}

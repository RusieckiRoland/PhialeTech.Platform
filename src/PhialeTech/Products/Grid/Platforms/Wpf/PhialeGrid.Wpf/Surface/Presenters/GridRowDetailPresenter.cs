using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using PhialeGrid.Core.Details;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface;

namespace PhialeTech.PhialeGrid.Wpf.Surface.Presenters
{
    public sealed class GridRowDetailPresenter : ContentControl
    {
        public GridRowDetailPresenter()
        {
            SetValue(ClipToBoundsProperty, true);
            SetValue(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
            SetValue(VerticalContentAlignmentProperty, VerticalAlignment.Stretch);
            SetResourceReference(StyleProperty, "PgGridRowDetailPresenterStyle");
            Focusable = false;
        }

        public IGridRowDetailContentFactory ContentFactory
        {
            get { return (IGridRowDetailContentFactory)GetValue(ContentFactoryProperty); }
            set { SetValue(ContentFactoryProperty, value); }
        }

        public static readonly DependencyProperty ContentFactoryProperty =
            DependencyProperty.Register(
                nameof(ContentFactory),
                typeof(IGridRowDetailContentFactory),
                typeof(GridRowDetailPresenter),
                new PropertyMetadata(null, OnContentFactoryChanged));

        public GridOverlaySurfaceItem OverlayData
        {
            get { return (GridOverlaySurfaceItem)GetValue(OverlayDataProperty); }
            set { SetValue(OverlayDataProperty, value); }
        }

        public static readonly DependencyProperty OverlayDataProperty =
            DependencyProperty.Register(
                nameof(OverlayData),
                typeof(GridOverlaySurfaceItem),
                typeof(GridRowDetailPresenter),
                new PropertyMetadata(null, OnOverlayDataChanged));

        public GridBounds Bounds
        {
            get { return (GridBounds)GetValue(BoundsProperty); }
            set { SetValue(BoundsProperty, value); }
        }

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register(
                nameof(Bounds),
                typeof(GridBounds),
                typeof(GridRowDetailPresenter),
                new PropertyMetadata(GridBounds.Empty, OnBoundsChanged));

        public void Clear()
        {
            Content = null;
            OverlayData = null;
            ContentFactory = null;
            AutomationProperties.SetAutomationId(this, string.Empty);
            AutomationProperties.SetName(this, string.Empty);
        }

        private static void OnContentFactoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GridRowDetailPresenter)d).RefreshContent();
        }

        private static void OnOverlayDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GridRowDetailPresenter)d).RefreshContent();
        }

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (GridRowDetailPresenter)d;
            var bounds = (GridBounds)e.NewValue;

            Canvas.SetLeft(presenter, bounds.X);
            Canvas.SetTop(presenter, bounds.Y);
            presenter.Width = bounds.Width;
            presenter.Height = bounds.Height;
        }

        private void RefreshContent()
        {
            if (OverlayData == null)
            {
                Content = null;
                AutomationProperties.SetAutomationId(this, string.Empty);
                AutomationProperties.SetName(this, string.Empty);
                return;
            }

            if (!(OverlayData.Payload is GridRowDetailSurfacePayload payload))
            {
                throw new InvalidOperationException("Row detail overlay requires GridRowDetailSurfacePayload.");
            }

            if (ContentFactory == null)
            {
                throw new InvalidOperationException("Row detail overlay requires IGridRowDetailContentFactory.");
            }

            var wpfContext = new GridRowDetailWpfContext(payload.Context, payload.ContentDescriptor);
            var content = ContentFactory.CreateContent(wpfContext);
            if (content == null)
            {
                throw new InvalidOperationException("Row detail content factory returned null.");
            }

            Content = content;
            AutomationProperties.SetAutomationId(this, "surface.row-detail." + payload.OwnerRowKey);
            AutomationProperties.SetName(this, "Row detail " + payload.OwnerRowKey);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GridRowDetailPresenterAutomationPeer(this);
        }

        private sealed class GridRowDetailPresenterAutomationPeer : FrameworkElementAutomationPeer
        {
            public GridRowDetailPresenterAutomationPeer(GridRowDetailPresenter owner)
                : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.Pane;
            }

            protected override string GetClassNameCore()
            {
                return nameof(GridRowDetailPresenter);
            }
        }
    }
}

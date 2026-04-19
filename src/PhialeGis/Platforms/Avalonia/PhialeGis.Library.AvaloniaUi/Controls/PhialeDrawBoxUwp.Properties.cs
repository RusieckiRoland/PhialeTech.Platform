// PhialeDrawBoxAvalonia.Properties.cs
using Avalonia;
using Avalonia.Controls;
using PhialeGis.Library.Abstractions.Interactions;

namespace PhialeGis.Library.AvaloniaUi.Controls
{
    public sealed partial class PhialeDrawBoxAvalonia
    {
        /// <summary>
        /// Controls the visibility of the vertical scrollbar.
        /// Avalonia: use boolean styled property (true = visible).
        /// </summary>
        public static readonly StyledProperty<bool> VerticalScrollBarVisibleProperty =
            AvaloniaProperty.Register<PhialeDrawBoxAvalonia, bool>(
                nameof(VerticalScrollBarVisible), false);

        public bool VerticalScrollBarVisible
        {
            get => GetValue(VerticalScrollBarVisibleProperty);
            set => SetValue(VerticalScrollBarVisibleProperty, value);
        }

        /// <summary>
        /// Controls the visibility of the horizontal scrollbar.
        /// Avalonia: use boolean styled property (true = visible).
        /// </summary>
        public static readonly StyledProperty<bool> HorizontalScrollBarVisibleProperty =
            AvaloniaProperty.Register<PhialeDrawBoxAvalonia, bool>(
                nameof(HorizontalScrollBarVisible), false);

        public bool HorizontalScrollBarVisible
        {
            get => GetValue(HorizontalScrollBarVisibleProperty);
            set => SetValue(HorizontalScrollBarVisibleProperty, value);
        }

        // ---------------------------------------------------------------------
        //  Avalonia does not have WinRT projection limits:
        //  we can expose IGisInteractionManager directly as a styled property.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Provides a handle to an interaction manager instance.
        /// </summary>
        public static readonly StyledProperty<IGisInteractionManager?> GisInteractionManagerProperty =
            AvaloniaProperty.Register<PhialeDrawBoxAvalonia, IGisInteractionManager?>(
                nameof(GisInteractionManager), defaultValue: null);

        public IGisInteractionManager? GisInteractionManager
        {
            get => GetValue(GisInteractionManagerProperty);
            set => SetValue(GisInteractionManagerProperty, value);
        }

        /// <summary>
        /// When true, the control forwards mouse events that originate from pen/touch
        /// interaction, preventing duplicated handling scenarios.
        /// </summary>
        public static readonly StyledProperty<bool> ForwardMouseEventsOnPenOrTouchInteractionProperty =
            AvaloniaProperty.Register<PhialeDrawBoxAvalonia, bool>(
                nameof(ForwardMouseEventsOnPenOrTouchInteraction), false);

        public bool ForwardMouseEventsOnPenOrTouchInteraction
        {
            get => GetValue(ForwardMouseEventsOnPenOrTouchInteractionProperty);
            set => SetValue(ForwardMouseEventsOnPenOrTouchInteractionProperty, value);
        }

        // Hook property change handlers (equivalent of UWP's DP callbacks)
        static PhialeDrawBoxAvalonia()
        {
            VerticalScrollBarVisibleProperty.Changed.AddClassHandler<PhialeDrawBoxAvalonia>(
                (o, _) => o.UpdateScrollBarsVisibility());
            HorizontalScrollBarVisibleProperty.Changed.AddClassHandler<PhialeDrawBoxAvalonia>(
                (o, _) => o.UpdateScrollBarsVisibility());

            GisInteractionManagerProperty.Changed.AddClassHandler<PhialeDrawBoxAvalonia>(OnGisInteractionManagerChanged);
        }

        private static void OnGisInteractionManagerChanged(PhialeDrawBoxAvalonia d, AvaloniaPropertyChangedEventArgs e)
        {
            var drawBox = d;
            var redrawable = drawBox.Redrawable;

            var oldManager = e.OldValue as IGisInteractionManager;
            var newManager = e.NewValue as IGisInteractionManager;

            if (oldManager != null)
            {
                drawBox.AttachInteractionManager(oldManager, null);
                oldManager.UnregisterControl(redrawable);
            }

            if (newManager != null)
            {
                drawBox.AttachInteractionManager(null, newManager);
                newManager.RegisterControl(redrawable);
            }

            drawBox.RefreshInteractionStatus();
            drawBox.Invalidate();
        }

        private void UpdateScrollBarsVisibility()
        {
            if (_scrollBarVertical is not null) _scrollBarVertical.IsVisible = VerticalScrollBarVisible;
            if (_scrollBarHorizontal is not null) _scrollBarHorizontal.IsVisible = HorizontalScrollBarVisible;
        }
    }
}

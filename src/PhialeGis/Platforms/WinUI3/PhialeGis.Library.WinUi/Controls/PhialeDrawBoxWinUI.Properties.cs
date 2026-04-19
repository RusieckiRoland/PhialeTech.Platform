// PhialeGis.Library.WinUi/Controls/PhialeDrawBoxWinUI.Properties.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using PhialeGis.Library.Abstractions.Interactions;

namespace PhialeGis.Library.WinUi.Controls
{
    public sealed partial class PhialeDrawBoxWinUI
    {
        /// <summary>Default manipulation flags applied to the control.</summary>
        public ManipulationModes DefaultManipulationMode { get; set; } =
            ManipulationModes.TranslateX | ManipulationModes.TranslateY |
            ManipulationModes.Scale | ManipulationModes.Rotate;

        // -------- Scrollbar visibility ----------------------------------------
        public Visibility VerticalScrollBarVisible
        {
            get => (Visibility)GetValue(VerticalScrollBarVisibleProperty);
            set => SetValue(VerticalScrollBarVisibleProperty, value);
        }
        public static readonly DependencyProperty VerticalScrollBarVisibleProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisible), typeof(Visibility),
                typeof(PhialeDrawBoxWinUI), new PropertyMetadata(Visibility.Collapsed));

        public Visibility HorizontalScrollBarVisible
        {
            get => (Visibility)GetValue(HorizontalScrollBarVisibleProperty);
            set => SetValue(HorizontalScrollBarVisibleProperty, value);
        }
        public static readonly DependencyProperty HorizontalScrollBarVisibleProperty =
            DependencyProperty.Register(nameof(HorizontalScrollBarVisible), typeof(Visibility),
                typeof(PhialeDrawBoxWinUI), new PropertyMetadata(Visibility.Collapsed));

        // -------- WinRT rule: expose manager as object on the DP surface -------
        public object GisInteractionManager
        {
            get => GetValue(GisInteractionManagerProperty);
            set => SetValue(GisInteractionManagerProperty, value);
        }
        public static readonly DependencyProperty GisInteractionManagerProperty =
            DependencyProperty.Register(nameof(GisInteractionManager), typeof(object),
                typeof(PhialeDrawBoxWinUI), new PropertyMetadata(default(object), OnGisInteractionManagerChanged));

        private static void OnGisInteractionManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var drawBox = (PhialeDrawBoxWinUI)d;
            var redrawable = drawBox.Redrawable;

            var oldManager = e.OldValue as IGisInteractionManager;
            var newManager = e.NewValue as IGisInteractionManager;

            drawBox.AttachInteractionManager(oldManager, newManager);
            oldManager?.UnregisterControl(redrawable);
            newManager?.RegisterControl(redrawable);

            drawBox.RefreshInteractionStatus();
            drawBox.Invalidate();
        }

        /// <summary>Internal strongly-typed accessor (not WinRT surface).</summary>
        internal IGisInteractionManager? InteractionManagerInternal
            => GisInteractionManager as IGisInteractionManager;
    }
}

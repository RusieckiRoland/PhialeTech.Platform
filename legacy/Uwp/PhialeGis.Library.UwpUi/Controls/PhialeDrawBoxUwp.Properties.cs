//PhialeDrawBoxUwp.Properties.cs

using PhialeGis.Library.Abstractions.Interactions;
using Windows.UI.Xaml;

namespace PhialeGis.Library.UwpUi.Controls
{
    public partial class PhialeDrawBoxUwp
    {
        /// <summary>
        /// Controls the visibility of the vertical scrollbar.
        /// </summary>
        public Visibility VerticalScrollBarVisible
        {
            get { return (Visibility)GetValue(VerticalScrollBarVisibleProperty); }
            set { SetValue(VerticalScrollBarVisibleProperty, value); }
        }

        /// <summary>
        /// DependencyProperty backing <see cref="VerticalScrollBarVisible"/>.
        /// </summary>
        public static DependencyProperty VerticalScrollBarVisibleProperty { get; } =
            DependencyProperty.Register(
                "VerticalScrollBarVisible",
                typeof(Visibility),
                typeof(PhialeDrawBoxUwp),
                new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Controls the visibility of the horizontal scrollbar.
        /// </summary>
        public Visibility HorizontalScrollBarVisible
        {
            get { return (Visibility)GetValue(HorizontalScrollBarVisibleProperty); }
            set { SetValue(HorizontalScrollBarVisibleProperty, value); }
        }

        /// <summary>
        /// DependencyProperty backing <see cref="HorizontalScrollBarVisible"/>.
        /// </summary>
        public static DependencyProperty HorizontalScrollBarVisibleProperty { get; } =
            DependencyProperty.Register(
                "HorizontalScrollBarVisible",
                typeof(Visibility),
                typeof(PhialeDrawBoxUwp),
                new PropertyMetadata(Visibility.Collapsed));

        // ---------------------------------------------------------------------
        //  IMPORTANT: WinRT projection rule
        //  Public surface of a UWP control cannot expose non-WinRT types.
        //  Therefore, we expose the manager as 'object' at the public/DP level,
        //  and cast to IGisInteractionManager internally where WinRT rules
        //  no longer apply.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Provides a handle to an interaction manager instance.
        /// 
        /// NOTE: Exposed as <see cref="object"/> to satisfy WinRT projection
        /// constraints (UWP cannot surface non-WinRT interfaces in public API).
        /// Internally, the control casts the value to <see cref="IGisInteractionManager"/>
        /// to call domain-specific methods (Register/Unregister).
        /// </summary>
        public object GisInteractionManager
        {
            get { return GetValue(GisInteractionManagerProperty); }
            set { SetValue(GisInteractionManagerProperty, value); }
        }

        /// <summary>
        /// DependencyProperty backing <see cref="GisInteractionManager"/>.
        /// 
        /// Type is <see cref="object"/> on purpose — do not change to a non-WinRT type.
        /// </summary>
        public static DependencyProperty GisInteractionManagerProperty { get; } =
            DependencyProperty.Register(
                "GisInteractionManager",
                typeof(object),                  // ← keep as object for WinRT compatibility
                typeof(PhialeDrawBoxUwp),
                new PropertyMetadata(null, OnGisInteractionManagerChanged));

        /// <summary>
        /// Called when the interaction manager DP changes.
        /// We detach the old manager and attach the new one, casting internally
        /// to <see cref="IGisInteractionManager"/> (safe inside control code).
        /// </summary>
        private static void OnGisInteractionManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var drawBox = (PhialeDrawBoxUwp)d;

            // Assuming 'Redrawable' is your internal composition target (IRenderingComposition).
            var redrawable = drawBox.Redrawable;

            // Internal-only cast: this is not part of WinRT public surface.
            var oldManager = e.OldValue as IGisInteractionManager;
            var newManager = e.NewValue as IGisInteractionManager;

            if (oldManager != null && redrawable != null)
            {
                // Detach the control from the old manager.
                oldManager.UnregisterControl(redrawable);
            }

            if (newManager != null && redrawable != null)
            {
                // Attach the control to the new manager.
                newManager.RegisterControl(redrawable); // -Roland
            }

            // Request a visual update after switching managers.
            drawBox.Invalidate();
        }

        /// <summary>
        /// When true, the control forwards mouse events that originate from pen/touch
        /// interaction, preventing duplicated handling scenarios.
        /// 
        /// NOTE: DP name kept as-is to avoid breaking existing XAML bindings.
        /// </summary>
        public static DependencyProperty ForwardMouseEventsOnPenOrTouchInteractionPropety { get; } =
            DependencyProperty.Register(
                "IgnoreMouseEventsFromStylus",
                typeof(bool),
                typeof(PhialeDrawBoxUwp),
                new PropertyMetadata(false));

        /// <summary>
        /// CLR wrapper for <see cref="ForwardMouseEventsOnPenOrTouchInteractionPropety"/>.
        /// </summary>
        public bool ForwardMouseEventsOnPenOrTouchInteraction
        {
            get { return (bool)GetValue(ForwardMouseEventsOnPenOrTouchInteractionPropety); }
            set { SetValue(ForwardMouseEventsOnPenOrTouchInteractionPropety, value); }
        }

        // ---------------------------------------------------------------------
        //  Optional convenience (internal-only):
        //  Use this to access the strongly-typed manager within the library
        //  without re-casting in multiple places.
        //  NOTE: In C# 7.3 reference types are nullable by default at compile-time,
        //  so this property may still return null if the cast fails.
        // ---------------------------------------------------------------------
        /// <summary>
        /// Strongly-typed internal accessor for the interaction manager.
        /// Not part of the WinRT API surface. May return null if not set/cast fails.
        /// </summary>
        internal IGisInteractionManager InteractionManagerInternal
        {
            get { return GisInteractionManager as IGisInteractionManager; }
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    [ContentProperty(nameof(PlaygroundContent))]
    public sealed partial class PhialeWorkspacePlayground : UserControl
    {
        public static readonly DependencyProperty PlaygroundContentProperty =
            DependencyProperty.Register(
                nameof(PlaygroundContent),
                typeof(object),
                typeof(PhialeWorkspacePlayground),
                new PropertyMetadata(null, OnPlaygroundContentChanged));

        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register(
                nameof(HorizontalScrollBarVisibility),
                typeof(ScrollBarVisibility),
                typeof(PhialeWorkspacePlayground),
                new PropertyMetadata(ScrollBarVisibility.Auto));

        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(
                nameof(VerticalScrollBarVisibility),
                typeof(ScrollBarVisibility),
                typeof(PhialeWorkspacePlayground),
                new PropertyMetadata(ScrollBarVisibility.Disabled));

        public PhialeWorkspacePlayground()
        {
            InitializeComponent();
        }

        public object PlaygroundContent
        {
            get => GetValue(PlaygroundContentProperty);
            set => SetValue(PlaygroundContentProperty, value);
        }

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
            set => SetValue(HorizontalScrollBarVisibilityProperty, value);
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        private static void OnPlaygroundContentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var playground = (PhialeWorkspacePlayground)dependencyObject;
            playground.PlaygroundContentPresenter.Content = e.NewValue;
        }
    }
}

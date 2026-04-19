using System;
using System.Windows;
using System.Windows.Shell;
using PhialeTech.Shell.Abstractions.Presentation;
using PhialeTech.Shell.Presentation;

namespace PhialeTech.Shell.Wpf.Controls
{
    public class PhialeWindow : Window
    {
        public static readonly DependencyProperty ShellStateProperty =
            DependencyProperty.Register(
                nameof(ShellState),
                typeof(ApplicationShellState),
                typeof(PhialeWindow),
                new FrameworkPropertyMetadata(null, OnShellStateChanged));

        public static readonly DependencyProperty CaptionHeightProperty =
            DependencyProperty.Register(
                nameof(CaptionHeight),
                typeof(double),
                typeof(PhialeWindow),
                new FrameworkPropertyMetadata(0d, OnChromeMetricChanged));

        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register(
                nameof(ResizeBorderThickness),
                typeof(Thickness),
                typeof(PhialeWindow),
                new FrameworkPropertyMetadata(default(Thickness), OnChromeMetricChanged));

        public static readonly DependencyProperty WindowCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(WindowCornerRadius),
                typeof(CornerRadius),
                typeof(PhialeWindow),
                new FrameworkPropertyMetadata(default(CornerRadius), OnChromeMetricChanged));

        static PhialeWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PhialeWindow), new FrameworkPropertyMetadata(typeof(PhialeWindow)));
        }

        public ApplicationShellState ShellState
        {
            get { return (ApplicationShellState)GetValue(ShellStateProperty); }
            set { SetValue(ShellStateProperty, value); }
        }

        public double CaptionHeight
        {
            get { return (double)GetValue(CaptionHeightProperty); }
            set { SetValue(CaptionHeightProperty, value); }
        }

        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)GetValue(ResizeBorderThicknessProperty); }
            set { SetValue(ResizeBorderThicknessProperty, value); }
        }

        public CornerRadius WindowCornerRadius
        {
            get { return (CornerRadius)GetValue(WindowCornerRadiusProperty); }
            set { SetValue(WindowCornerRadiusProperty, value); }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyWindowChrome();
        }

        private static void OnShellStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                ApplicationShellStateValidator.Validate((ApplicationShellState)e.NewValue);
            }
        }

        private static void OnChromeMetricChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (PhialeWindow)d;
            if (window.IsLoaded)
            {
                window.ApplyWindowChrome();
            }
        }

        private void ApplyWindowChrome()
        {
            if (CaptionHeight <= 0d)
            {
                throw new InvalidOperationException("PhialeWindow requires CaptionHeight to be provided by styles.");
            }

            if (ResizeBorderThickness.Left <= 0d ||
                ResizeBorderThickness.Top <= 0d ||
                ResizeBorderThickness.Right <= 0d ||
                ResizeBorderThickness.Bottom <= 0d)
            {
                throw new InvalidOperationException("PhialeWindow requires ResizeBorderThickness to be provided by styles.");
            }

            var chrome = WindowChrome.GetWindowChrome(this) ?? new WindowChrome();
            chrome.CaptionHeight = CaptionHeight;
            chrome.ResizeBorderThickness = ResizeBorderThickness;
            chrome.CornerRadius = WindowCornerRadius;
            chrome.GlassFrameThickness = new Thickness(0);
            chrome.UseAeroCaptionButtons = false;
            WindowChrome.SetWindowChrome(this, chrome);
        }
    }
}

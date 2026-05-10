using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeTech.WebHost.Wpf.Controls;

namespace PhialeTech.WebHost.Wpf.Tests.Scroll
{
    public sealed class PhialeWebComponentScrollHostArchitectureTests
    {
        [Test]
        public void ScrollHost_IsImplementedInWpfFrontend_WithoutScrollViewer()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("sealed class PhialeWebComponentScrollHost"));
            Assert.That(code, Does.Contain("namespace PhialeTech.WebHost.Wpf.Controls"));
            Assert.That(code, Does.Not.Contain("ScrollViewer"));
        }

        [Test]
        public void ScrollHost_RemainsNonFocusable_AndClipsViewport()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("Focusable = false"));
            Assert.That(code, Does.Contain("IsTabStop = false"));
            Assert.That(code, Does.Contain("ClipToBounds = true"));
            Assert.That(code, Does.Contain("PreviewMouseWheel += OnPreviewMouseWheel"));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_MeasureWithInfiniteAvailableSize_ReturnsFiniteDesiredSize()
        {
            var host = new PhialeWebComponentScrollHost
            {
                HostedContent = new Border
                {
                    Width = 320d,
                    Height = 480d
                }
            };

            host.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Multiple(() =>
            {
                Assert.That(double.IsInfinity(host.DesiredSize.Width), Is.False);
                Assert.That(double.IsInfinity(host.DesiredSize.Height), Is.False);
                Assert.That(double.IsNaN(host.DesiredSize.Width), Is.False);
                Assert.That(double.IsNaN(host.DesiredSize.Height), Is.False);
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_MeasureWithFiniteAvailableHeight_UsesContentHeight()
        {
            var host = new PhialeWebComponentScrollHost
            {
                HostedContent = new Border
                {
                    Width = 320d,
                    Height = 180d
                }
            };

            host.Measure(new Size(720d, 640d));

            Assert.That(host.DesiredSize.Height, Is.EqualTo(180d).Within(0.5d));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_MeasureWithFiniteAvailableHeight_UsesViewportHeight_WhenContentIsTaller()
        {
            var host = new PhialeWebComponentScrollHost
            {
                HostedContent = new Border
                {
                    Width = 320d,
                    Height = 480d
                }
            };

            host.Measure(new Size(720d, 120d));

            Assert.That(host.DesiredSize.Height, Is.EqualTo(120d).Within(0.5d));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_ViewportPanelReportsViewportHeight_WhenMeasuredContentIsTallerThanFiniteViewport()
        {
            var host = new PhialeWebComponentScrollHost
            {
                HostedContent = new Border
                {
                    Width = 320d,
                    Height = 480d
                }
            };

            host.Measure(new Size(720d, 120d));

            var viewport = GetViewportPanel(host);
            Assert.That(viewport.DesiredSize.Height, Is.EqualTo(120d).Within(0.5d));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_WhenArrangedBelowContentHeight_KeepsViewportConstrained()
        {
            var host = new PhialeWebComponentScrollHost
            {
                HostedContent = new Border
                {
                    Width = 320d,
                    Height = 480d
                }
            };

            host.Measure(new Size(320d, 120d));
            host.Arrange(new Rect(0d, 0d, 320d, 120d));

            Assert.That(host.ActualHeight, Is.EqualTo(120d).Within(0.5d));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_ViewportPaddingCreatesInsetInsideChrome()
        {
            var content = new Border
            {
                Width = 80d,
                Height = 40d,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var host = new PhialeWebComponentScrollHost
            {
                BorderThickness = new Thickness(1d),
                ViewportPadding = new Thickness(12d),
                HostedContent = content
            };

            host.Measure(new Size(200d, 120d));
            host.Arrange(new Rect(0d, 0d, 200d, 120d));

            var chrome = GetChromeBorder(host);
            var chromeTopLeft = chrome.TransformToAncestor(host).Transform(new Point(0d, 0d));
            var contentTopLeft = content.TransformToAncestor(chrome).Transform(new Point(0d, 0d));

            Assert.Multiple(() =>
            {
                Assert.That(chromeTopLeft.X, Is.EqualTo(0d).Within(0.5d));
                Assert.That(chromeTopLeft.Y, Is.EqualTo(0d).Within(0.5d));
                Assert.That(contentTopLeft.X, Is.EqualTo(13d).Within(0.5d));
                Assert.That(contentTopLeft.Y, Is.EqualTo(13d).Within(0.5d));
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_WebViewLikeZeroDesiredContent_KeepsScrollMetricsStableAfterArrange()
        {
            var content = new ZeroDesiredHeightElement();
            var host = new PhialeWebComponentScrollHost
            {
                Width = 800d,
                Height = 520d,
                BorderThickness = new Thickness(0d),
                HostedContent = content
            };

            host.Measure(new Size(800d, 520d));
            host.Arrange(new Rect(0d, 0d, 800d, 520d));
            host.Measure(new Size(800d, 520d));
            host.Arrange(new Rect(0d, 0d, 800d, 520d));

            var scrollBar = GetVerticalScrollBar(host);
            var viewport = GetViewportPanel(host);

            Assert.Multiple(() =>
            {
                Assert.That(content.MeasureCallCount, Is.GreaterThan(0));
                Assert.That(content.LastArrangeSize.Height, Is.EqualTo(520d).Within(0.5d));
                Assert.That(viewport.ActualHeight, Is.EqualTo(520d).Within(0.5d));
                Assert.That(scrollBar.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(scrollBar.Maximum, Is.EqualTo(0d).Within(0.5d));
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_WhenHostedDescendantGrowsAfterFirstLayout_ShowsVerticalScrollBar()
        {
            var content = new MutableDesiredHeightElement(240d);
            var host = new PhialeWebComponentScrollHost
            {
                Width = 800d,
                Height = 520d,
                BorderThickness = new Thickness(0d),
                HostedContent = content
            };
            var window = new Window
            {
                Width = 800d,
                Height = 520d,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None
            };

            try
            {
                window.Show();
                host.Measure(new Size(800d, 520d));
                host.Arrange(new Rect(0d, 0d, 800d, 520d));
                host.UpdateLayout();

                content.SetDesiredHeight(900d);
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                host.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

                var scrollBar = GetVerticalScrollBar(host);

                Assert.Multiple(() =>
                {
                    Assert.That(content.MeasureCallCount, Is.GreaterThan(1));
                    Assert.That(scrollBar.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(scrollBar.Maximum, Is.GreaterThan(300d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScrollHost_WhenContentIsTallerThanVisibleHost_UsesHostViewportForScrollMaximum()
        {
            var host = new PhialeWebComponentScrollHost
            {
                Width = 1488d,
                Height = 548d,
                BorderThickness = new Thickness(1d),
                HostedContent = new Border
                {
                    Width = 1486d,
                    Height = 982d
                }
            };

            host.Measure(new Size(1488d, 548d));
            host.Arrange(new Rect(0d, 0d, 1488d, 548d));
            host.UpdateLayout();

            var scrollBar = GetVerticalScrollBar(host);

            Assert.Multiple(() =>
            {
                Assert.That(scrollBar.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(scrollBar.Maximum, Is.GreaterThan(400d));
                Assert.That(scrollBar.ViewportSize, Is.EqualTo(546d).Within(0.5d));
            });
        }

        private static string GetCodePath()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Repository root could not be located from the test output directory.");
            }

            return Path.Combine(
                directory.FullName,
                "src",
                "PhialeTech",
                "Shared",
                "Platforms",
                "Wpf",
                "PhialeTech.WebHost.Wpf",
                "Controls",
                "PhialeWebComponentScrollHost.cs");
        }

        private static FrameworkElement GetViewportPanel(PhialeWebComponentScrollHost host)
        {
            var field = typeof(PhialeWebComponentScrollHost).GetField("_viewport", BindingFlags.Instance | BindingFlags.NonPublic);
            var viewport = field?.GetValue(host) as FrameworkElement;
            Assert.That(viewport, Is.Not.Null);
            return viewport;
        }

        private static Border GetChromeBorder(PhialeWebComponentScrollHost host)
        {
            var field = typeof(PhialeWebComponentScrollHost).GetField("_chromeBorder", BindingFlags.Instance | BindingFlags.NonPublic);
            var chrome = field?.GetValue(host) as Border;
            Assert.That(chrome, Is.Not.Null);
            return chrome;
        }

        private static ScrollBar GetVerticalScrollBar(PhialeWebComponentScrollHost host)
        {
            var field = typeof(PhialeWebComponentScrollHost).GetField("_verticalScrollBar", BindingFlags.Instance | BindingFlags.NonPublic);
            var scrollBar = field?.GetValue(host) as ScrollBar;
            Assert.That(scrollBar, Is.Not.Null);
            return scrollBar;
        }

        private sealed class ZeroDesiredHeightElement : FrameworkElement
        {
            public int MeasureCallCount { get; private set; }

            public Size LastArrangeSize { get; private set; }

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureCallCount++;
                return new Size(0d, 0d);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                LastArrangeSize = finalSize;
                return finalSize;
            }
        }

        private sealed class MutableDesiredHeightElement : FrameworkElement
        {
            private double _desiredHeight;

            public MutableDesiredHeightElement(double desiredHeight)
            {
                _desiredHeight = desiredHeight;
            }

            public int MeasureCallCount { get; private set; }

            public void SetDesiredHeight(double desiredHeight)
            {
                _desiredHeight = desiredHeight;
                InvalidateMeasure();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureCallCount++;
                return new Size(320d, _desiredHeight);
            }
        }
    }
}


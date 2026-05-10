using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NUnit.Framework;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridSurfacePresenterTemplateTests
    {
        [Test]
        public void GridCellPresenter_WhenCurrentRowBackgroundIsSet_RendersTemplateBorder()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "col-1")
                {
                    DisplayText = "Alpha",
                    IsCurrentRow = true,
                },
                Bounds = new GridBounds(0, 0, 120, 28),
            };

            var window = GridSurfaceTestHost.CreateHostWindow(presenter, width: 160, height: 80);
            try
            {
                window.Resources["PgGridSurfaceCellPresenterStyle"] = CreatePresenterStyle();
                window.Resources["PgCurrentRowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xD7, 0xE8, 0xFF));
                window.Resources["PgCurrentRowBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x5B, 0x9B, 0xFF));
                window.Resources["PgPrimaryTextBrush"] = new SolidColorBrush(Colors.Black);
                window.Resources["PgGridLineBrush"] = new SolidColorBrush(Colors.LightGray);
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var border = GridSurfaceTestHost.FindVisualChildren<Border>(presenter).FirstOrDefault();

                Assert.Multiple(() =>
                {
                    Assert.That(border, Is.Not.Null);
                    Assert.That(border.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(((SolidColorBrush)border.Background).Color, Is.EqualTo(Color.FromRgb(0xD7, 0xE8, 0xFF)));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void GridOverlayPresenter_WhenRowHighlightIsApplied_RemainsTransparent()
        {
            var presenter = new GridOverlayPresenter
            {
                OverlayData = new GridOverlaySurfaceItem("row-highlight", GridOverlayKind.RowHighlight),
                Bounds = new GridBounds(0, 0, 320, 28),
            };

            var window = GridSurfaceTestHost.CreateHostWindow(presenter, width: 360, height: 80);
            try
            {
                window.Resources["PgGridSurfaceOverlayPresenterStyle"] = CreatePresenterStyle();
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(window);

                var border = GridSurfaceTestHost.FindVisualChildren<Border>(presenter).FirstOrDefault();

                Assert.Multiple(() =>
                {
                    Assert.That(border, Is.Not.Null);
                    Assert.That(border.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(((SolidColorBrush)border.Background).Color, Is.EqualTo(Colors.Transparent));
                    Assert.That(border.BorderThickness.Top, Is.EqualTo(0d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static Style CreatePresenterStyle()
        {
            var style = new Style(typeof(ContentControl));
            style.Setters.Add(new Setter(Control.TemplateProperty, CreateTemplate()));
            return style;
        }

        private static ControlTemplate CreateTemplate()
        {
            var template = new ControlTemplate(typeof(ContentControl));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent),
            });
            borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent),
            });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent),
            });

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new System.Windows.Data.Binding("HorizontalContentAlignment")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent),
            });
            contentFactory.SetBinding(ContentPresenter.VerticalAlignmentProperty, new System.Windows.Data.Binding("VerticalContentAlignment")
            {
                RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent),
            });
            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;
            return template;
        }
    }
}


using System.Linq;
using System.Windows.Controls;
using System.Threading;
using NUnit.Framework;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridColumnHeaderPresenterTests
    {
        [Test]
        public void HeaderData_WithSortIconKey_AppendsSortGlyphToContent()
        {
            var presenter = new GridColumnHeaderPresenter
            {
                HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
                {
                    DisplayText = "City",
                    IconKey = "sort-asc",
                },
            };

            Assert.That(GridSurfaceTestHost.ReadVisibleText(presenter), Does.Contain("City"));
            Assert.That(GridSurfaceTestHost.ReadVisibleText(presenter), Does.Contain("▲"));
        }

        [Test]
        public void HeaderData_WithoutSortIconKey_UsesDisplayTextOnly()
        {
            var presenter = new GridColumnHeaderPresenter
            {
                HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
                {
                    DisplayText = "City",
                },
            };

            Assert.That(GridSurfaceTestHost.ReadVisibleText(presenter), Is.EqualTo("City"));
        }

        [Test]
        public void HeaderData_WithSortOrderText_RendersDedicatedSortOrderBadge()
        {
            var presenter = new GridColumnHeaderPresenter
            {
                HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
                {
                    DisplayText = "City",
                    IconKey = "sort-asc",
                    SortOrderText = "2",
                },
            };

            var badgeText = GridSurfaceTestHost.FindElementByAutomationId<System.Windows.Controls.TextBlock>(presenter, "surface.column-header.col-1.sort-order");

            Assert.Multiple(() =>
            {
                Assert.That(GridSurfaceTestHost.ReadVisibleText(presenter), Does.Contain("2"));
                Assert.That(badgeText, Is.Not.Null);
                Assert.That(badgeText.Text, Is.EqualTo("2"));
            });
        }

        [Test]
        public void HeaderData_ShouldRenderSeparatorWithoutHorizontalInset()
        {
            var presenter = new GridColumnHeaderPresenter
            {
                Width = 180,
                Height = 30,
                HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
                {
                    DisplayText = "City",
                },
            };

            var window = GridSurfaceTestHost.CreateHostWindow(presenter, width: 220, height: 80);
            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(presenter);

                var separator = GridSurfaceTestHost.FindVisualChildren<Border>(presenter)
                    .FirstOrDefault(border => border.Width == 1d && border.HorizontalAlignment == System.Windows.HorizontalAlignment.Right);

                Assert.Multiple(() =>
                {
                    Assert.That(separator, Is.Not.Null);
                    if (separator != null)
                    {
                        Assert.That(separator.Margin, Is.EqualTo(new System.Windows.Thickness(0)));
                    }
                });
            }
            finally
            {
                window.Close();
            }
        }
    }
}

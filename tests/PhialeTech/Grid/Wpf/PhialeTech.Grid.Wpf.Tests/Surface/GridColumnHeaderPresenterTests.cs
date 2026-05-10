using System.Linq;
using System;
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
            var presenter = CreatePresenter();
            presenter.HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
            {
                DisplayText = "City",
                IconKey = "sort-asc",
            };

            Assert.That(GridSurfaceTestHost.ReadVisibleText(presenter), Does.Contain("City"));
            Assert.That(GridSurfaceTestHost.ReadVisibleText(presenter), Does.Contain("▲"));
        }

        [Test]
        public void HeaderData_WithSortIconKey_UsesDedicatedSortGlyphStyle()
        {
            var presenter = CreatePresenter();
            presenter.HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
            {
                DisplayText = "City",
                IconKey = "sort-asc",
            };

            var glyphText = GridSurfaceTestHost.FindElementByAutomationId<TextBlock>(presenter, "surface.column-header.col-1.sort-glyph");

            Assert.Multiple(() =>
            {
                Assert.That(glyphText, Is.Not.Null);
                if (glyphText != null)
                {
                    Assert.That(glyphText.Style, Is.SameAs(presenter.TryFindResource("PgGridSortGlyphTextStyle")));
                    Assert.That(glyphText.FontSize, Is.GreaterThan(12d));
                    Assert.That(glyphText.FontWeight, Is.EqualTo(System.Windows.FontWeights.SemiBold));
                }
            });
        }

        [Test]
        public void HeaderData_WithoutSortIconKey_UsesDisplayTextOnly()
        {
            var presenter = CreatePresenter();
            presenter.HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
            {
                DisplayText = "City",
            };

            Assert.That(GridSurfaceTestHost.ReadVisibleText(presenter), Is.EqualTo("City"));
        }

        [Test]
        public void HeaderData_WithSortOrderText_RendersDedicatedSortOrderBadge()
        {
            var presenter = CreatePresenter();
            presenter.HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
            {
                DisplayText = "City",
                IconKey = "sort-asc",
                SortOrderText = "2",
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
            var presenter = CreatePresenter();
            presenter.Width = 180;
            presenter.Height = 30;
            presenter.HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
            {
                DisplayText = "City",
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

        private static GridColumnHeaderPresenter CreatePresenter()
        {
            var presenter = new GridColumnHeaderPresenter();
            LoadGridResources(presenter);
            return presenter;
        }

        private static void LoadGridResources(System.Windows.FrameworkElement element)
        {
            element.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/ThemeTokens.Day.xaml", UriKind.Absolute),
            });
            element.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/Controls.Core.xaml", UriKind.Absolute),
            });
            element.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/PhialeGrid.Shared.xaml", UriKind.Absolute),
            });
            element.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/PhialeTech.Styles.Wpf;component/Themes/PhialeGrid.Controls.xaml", UriKind.Absolute),
            });
        }
    }
}


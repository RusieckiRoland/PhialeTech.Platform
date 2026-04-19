using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using NUnit.Framework;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridRowHeaderPresenterTests
    {
        [Test]
        public void Presenter_WhenCurrentIndicatorEnabled_RendersDedicatedIndicatorSlot()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.Current,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Polygon>(indicator).Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenCurrentIndicatorEnabled_CentersTriangleVerticallyInIndicatorSlot()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.Current,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<Border>(presenter, "surface.row-indicator.row-1");
                var triangle = GridSurfaceTestHost.FindVisualChildren<Polygon>(indicator).Single();

                var slotBounds = indicator.TransformToAncestor(window)
                    .TransformBounds(new Rect(0, 0, indicator.ActualWidth, indicator.ActualHeight));
                var triangleBounds = triangle.TransformToAncestor(window)
                    .TransformBounds(new Rect(0, 0, triangle.ActualWidth, triangle.ActualHeight));

                var slotCenterY = slotBounds.Top + (slotBounds.Height / 2d);
                var triangleCenterY = triangleBounds.Top + (triangleBounds.Height / 2d);

                Assert.That(triangleCenterY, Is.EqualTo(slotCenterY).Within(1d));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenInvalidIndicatorEnabled_RendersErrorGlyph()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.Invalid,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Ellipse>(indicator).Count(), Is.EqualTo(1));
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Rectangle>(indicator).Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenCurrentAndEditedIndicatorEnabled_RendersCompositeGlyph()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.CurrentAndEdited,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Polygon>(indicator).Any(), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Path>(indicator).Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenCurrentAndInvalidIndicatorEnabled_RendersCompositeGlyph()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.CurrentAndInvalid,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Polygon>(indicator).Any(), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Ellipse>(indicator).Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenEditingIndicatorEnabled_RendersVerticalEditMarkerInsteadOfTriangle()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.Editing,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Polygon>(indicator).Any(), Is.False);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Path>(indicator).Any(), Is.False);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Border>(indicator).Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenEditingAndEditedAndInvalidIndicatorEnabled_RendersCompositeEditingGlyph()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.EditingAndEditedAndInvalid,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Path>(indicator).Any(), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Ellipse>(indicator).Any(), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Border>(indicator).Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenIndicatorTooltipProvided_AssignsToolTipToIndicatorSlot()
        {
            const string toolTip = "Edited fields:\n- Object name: Before -> After";
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.Edited,
                RowIndicatorToolTip = toolTip,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<Border>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(indicator.ToolTip, Is.EqualTo(toolTip));
                    Assert.That(ToolTipService.GetPlacement(indicator), Is.EqualTo(PlacementMode.Right));
                    Assert.That(ToolTipService.GetHorizontalOffset(indicator), Is.EqualTo(10d).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenMultiSelectEnabled_RendersSeparateCheckboxSlot()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                SelectionCheckboxWidth = 18d,
                ShowSelectionCheckbox = true,
                IsSelectionCheckboxChecked = true,
                RowIndicatorState = GridRowIndicatorState.Edited,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.row-indicator.row-1");
                var checkbox = GridSurfaceTestHost.FindElementByAutomationId<Grid>(presenter, "surface.row-selector.row-1");
                var checkboxSlot = GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(presenter, "surface.row-checkbox-slot.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(checkbox, Is.Not.Null);
                    Assert.That(checkboxSlot, Is.Not.Null);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Path>(checkbox).Any(), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Border>(checkbox).Any(), Is.True);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Path>(indicator).Any(), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenSelectionCheckboxRendered_DelegatesHitTestingToStableMarkerSlot()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                SelectionCheckboxWidth = 18d,
                ShowSelectionCheckbox = true,
            });

            try
            {
                var checkbox = GridSurfaceTestHost.FindElementByAutomationId<Grid>(presenter, "surface.row-selector.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(checkbox, Is.Not.Null);
                    Assert.That(checkbox.IsHitTestVisible, Is.False);
                    Assert.That(checkbox.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(((SolidColorBrush)checkbox.Background).Color, Is.EqualTo(Colors.Transparent));
                    Assert.That(GridSurfaceTestHost.FindElementByAutomationId<Border>(presenter, "surface.row-checkbox-slot.row-1").IsHitTestVisible, Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenRowNumberHeaderRendered_ShowsDedicatedNumberColumn()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowNumberHeader)
            {
                ShowRowNumber = true,
                RowNumberText = "7",
                RowMarkerWidth = 28d,
            });

            try
            {
                var numberSlot = GridSurfaceTestHost.FindElementByAutomationId<Border>(presenter, "surface.row-number.row-1");
                var numberText = GridSurfaceTestHost.FindVisualChildren<TextBlock>(presenter).FirstOrDefault(text => text.Text == "7");

                Assert.Multiple(() =>
                {
                    Assert.That(numberSlot, Is.Not.Null);
                    Assert.That(numberText, Is.Not.Null);
                    Assert.That(numberSlot.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(
                        ((SolidColorBrush)numberSlot.Background).Color,
                        Is.EqualTo(((SolidColorBrush)window.Resources["PgRowIndicatorColumnBackgroundBrush"]).Color));
                    Assert.That(numberText.Foreground, Is.TypeOf<SolidColorBrush>());
                    Assert.That(
                        ((SolidColorBrush)numberText.Foreground).Color,
                        Is.EqualTo(((SolidColorBrush)window.Resources["PgRowNumberTextBrush"]).Color));
                    Assert.That(numberText.FontWeight, Is.EqualTo(FontWeights.Normal));
                    Assert.That(numberText.FontSize, Is.EqualTo(10d).Within(0.1d));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenIndicatorColumnRendered_UsesVisibleDedicatedColumnBackground()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.Empty,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<Border>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(indicator.ActualWidth, Is.EqualTo(18d).Within(1d));
                    Assert.That(indicator.Background, Is.TypeOf<SolidColorBrush>());
                    Assert.That(((SolidColorBrush)indicator.Background).Color, Is.Not.EqualTo(Colors.Transparent));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenIndicatorFeatureIsDisabled_ButStateColumnWidthExists_KeepsEmptyStateSlotVisible()
        {
            var (presenter, window) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = false,
                RowIndicatorWidth = 18d,
                RowIndicatorState = GridRowIndicatorState.Current,
            });

            try
            {
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<Border>(presenter, "surface.row-indicator.row-1");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(indicator.ActualWidth, Is.EqualTo(18d).Within(1d));
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Polygon>(indicator).Any(), Is.False);
                    Assert.That(GridSurfaceTestHost.FindVisualChildren<Path>(indicator).Any(), Is.False);
                    Assert.That(indicator.Background, Is.TypeOf<SolidColorBrush>());
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void Presenter_WhenUtilityColumnsRendered_UsesSubtleInternalSeparators()
        {
            var (rowStatePresenter, rowStateWindow) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                SelectionCheckboxWidth = 18d,
                ShowSelectionCheckbox = true,
                RowIndicatorState = GridRowIndicatorState.Current,
            });

            var (rowNumberPresenter, rowNumberWindow) = CreatePresenter(new GridHeaderSurfaceItem("row-2", GridHeaderKind.RowNumberHeader)
            {
                ShowRowNumber = true,
                RowNumberText = "12",
                RowMarkerWidth = 28d,
            });

            try
            {
                var expectedSeparator = (SolidColorBrush)rowStateWindow.Resources["PgDetailsBorderLightBrush"];
                var indicator = GridSurfaceTestHost.FindElementByAutomationId<Border>(rowStatePresenter, "surface.row-indicator.row-1");
                var checkboxSlot = GridSurfaceTestHost.FindElementByAutomationId<Border>(rowStatePresenter, "surface.row-checkbox-slot.row-1");
                var rowNumber = GridSurfaceTestHost.FindElementByAutomationId<Border>(rowNumberPresenter, "surface.row-number.row-2");

                Assert.Multiple(() =>
                {
                    Assert.That(indicator, Is.Not.Null);
                    Assert.That(checkboxSlot, Is.Not.Null);
                    Assert.That(rowNumber, Is.Not.Null);
                    Assert.That(((SolidColorBrush)indicator.BorderBrush).Color, Is.EqualTo(expectedSeparator.Color));
                    Assert.That(((SolidColorBrush)checkboxSlot.BorderBrush).Color, Is.EqualTo(expectedSeparator.Color));
                    Assert.That(((SolidColorBrush)rowNumber.BorderBrush).Color, Is.EqualTo(expectedSeparator.Color));
                });
            }
            finally
            {
                rowStateWindow.Close();
                rowNumberWindow.Close();
            }
        }

        [Test]
        public void Presenter_WhenRowStateAndRowNumbersRendered_UsesSingleSubtleOuterChrome()
        {
            var (rowStatePresenter, rowStateWindow) = CreatePresenter(new GridHeaderSurfaceItem("row-1", GridHeaderKind.RowHeader)
            {
                ShowRowIndicator = true,
                RowIndicatorWidth = 18d,
                SelectionCheckboxWidth = 18d,
                ShowSelectionCheckbox = true,
            });

            var (rowNumberPresenter, rowNumberWindow) = CreatePresenter(new GridHeaderSurfaceItem("row-2", GridHeaderKind.RowNumberHeader)
            {
                ShowRowNumber = true,
                RowNumberText = "12",
                RowMarkerWidth = 28d,
            });

            try
            {
                var expectedSeparator = (SolidColorBrush)rowStateWindow.Resources["PgDetailsBorderLightBrush"];
                var rowNumberSlot = GridSurfaceTestHost.FindElementByAutomationId<Border>(rowNumberPresenter, "surface.row-number.row-2");

                Assert.Multiple(() =>
                {
                    Assert.That(((SolidColorBrush)rowStatePresenter.BorderBrush).Color, Is.EqualTo(expectedSeparator.Color));
                    Assert.That(((SolidColorBrush)rowNumberPresenter.BorderBrush).Color, Is.EqualTo(expectedSeparator.Color));
                    Assert.That(rowStatePresenter.BorderThickness, Is.EqualTo(new Thickness(0, 0, 0, 1)));
                    Assert.That(rowNumberPresenter.BorderThickness, Is.EqualTo(new Thickness(0, 0, 1, 1)));
                    Assert.That(rowNumberSlot.BorderThickness, Is.EqualTo(new Thickness(0)));
                });
            }
            finally
            {
                rowStateWindow.Close();
                rowNumberWindow.Close();
            }
        }

        private static (GridRowHeaderPresenter Presenter, Window Window) CreatePresenter(GridHeaderSurfaceItem header)
        {
            var presenter = new GridRowHeaderPresenter
            {
                HeaderData = header,
                Bounds = new GridBounds(0, 0, 80, 24),
            };

            var window = GridSurfaceTestHost.CreateHostWindow(presenter, width: 80, height: 60);
            window.Resources["PgHeaderBackgroundBrush"] = new SolidColorBrush(Colors.White);
            window.Resources["PgHeaderBorderBrush"] = new SolidColorBrush(Colors.SlateGray);
            window.Resources["PgDetailsBorderLightBrush"] = new SolidColorBrush(Color.FromRgb(0xD9, 0xE0, 0xEA));
            window.Resources["PgCurrentRowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xD7, 0xE8, 0xFF));
            window.Resources["PgRowIndicatorColumnBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF0, 0xF3, 0xF7));
            window.Resources["PgRowIndicatorColumnCurrentBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xD7, 0xE8, 0xFF));
            window.Resources["PgAccentBrush"] = new SolidColorBrush(Colors.RoyalBlue);
            window.Resources["PgMutedTextBrush"] = new SolidColorBrush(Colors.DimGray);
            window.Resources["PgRowNumberTextBrush"] = new SolidColorBrush(Color.FromRgb(0x6C, 0x7A, 0x89));
            window.Resources["Brush.Danger.Border"] = new SolidColorBrush(Colors.IndianRed);
            window.Resources["Brush.Danger.Fill"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xD7, 0xDA));
            window.Resources["Brush.Danger.Text"] = new SolidColorBrush(Colors.White);
            window.Resources["Brush.Warning.Border"] = new SolidColorBrush(Color.FromRgb(0xE6, 0x7E, 0x00));
            window.Show();
            GridSurfaceTestHost.FlushDispatcher(window);
            presenter.ApplyTemplate();
            presenter.UpdateLayout();
            return (presenter, window);
        }
    }
}

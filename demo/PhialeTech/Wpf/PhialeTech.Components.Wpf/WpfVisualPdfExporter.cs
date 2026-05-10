using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PhialeTech.Components.Shared.Services;

namespace PhialeTech.Components.Wpf
{
    internal static class WpfVisualPdfExporter
    {
        private const double ExportDpi = 144d;
        private const double PageMarginPoints = 28d;

        public static void ExportFrameworkElementToPdf(FrameworkElement visual, string outputPath, string documentTitle)
        {
            if (visual == null)
            {
                throw new ArgumentNullException(nameof(visual));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path is required.", nameof(outputPath));
            }

            visual.UpdateLayout();

            var sourceWidth = ResolveVisualWidth(visual);
            var sourceHeight = ResolveVisualHeight(visual);

            if (sourceWidth <= 0d || sourceHeight <= 0d)
            {
                throw new InvalidOperationException("The selected visual has no renderable size.");
            }

            using (var document = new PdfDocument())
            {
                document.Info.Title = string.IsNullOrWhiteSpace(documentTitle) ? "Preview foundations" : documentTitle;

                var pagePrototype = document.AddPage();
                pagePrototype.Size = PageSize.A4;
                pagePrototype.Orientation = PageOrientation.Landscape;

                var pageContentWidth = pagePrototype.Width.Point - (PageMarginPoints * 2d);
                var pageContentHeight = pagePrototype.Height.Point - (PageMarginPoints * 2d);
                document.Pages.Remove(pagePrototype);

                var slices = DemoPdfPaginationPlanner.PlanSlices(
                    sourceWidth,
                    sourceHeight,
                    pageContentWidth,
                    pageContentHeight);

                foreach (var slice in slices)
                {
                    var page = document.AddPage();
                    page.Size = PageSize.A4;
                    page.Orientation = PageOrientation.Landscape;

                    using (var gfx = XGraphics.FromPdfPage(page))
                    using (var image = XImage.FromBitmapSource(
                               RenderSlice(
                                   visual,
                                   new Rect(0d, slice.SourceOffsetY, sourceWidth, slice.SourceHeight))))
                    {
                        gfx.DrawImage(
                            image,
                            PageMarginPoints,
                            PageMarginPoints,
                            pageContentWidth,
                            slice.RenderedHeight);
                    }
                }

                document.Save(outputPath);
            }
        }

        private static double ResolveVisualWidth(FrameworkElement visual)
        {
            return visual.ActualWidth > 0d
                ? visual.ActualWidth
                : Math.Max(visual.RenderSize.Width, visual.DesiredSize.Width);
        }

        private static double ResolveVisualHeight(FrameworkElement visual)
        {
            return visual.ActualHeight > 0d
                ? visual.ActualHeight
                : Math.Max(visual.RenderSize.Height, visual.DesiredSize.Height);
        }

        private static BitmapSource RenderSlice(FrameworkElement visual, Rect sourceRect)
        {
            var scale = ExportDpi / 96d;
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(sourceRect.Width * scale));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(sourceRect.Height * scale));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var visualBrush = new VisualBrush(visual)
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top,
                    ViewboxUnits = BrushMappingMode.Absolute,
                    Viewbox = sourceRect
                };

                drawingContext.DrawRectangle(visualBrush, null, new Rect(0d, 0d, sourceRect.Width, sourceRect.Height));
            }

            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, ExportDpi, ExportDpi, PixelFormats.Pbgra32);
            bitmap.Render(drawingVisual);
            bitmap.Freeze();
            return bitmap;
        }
    }
}


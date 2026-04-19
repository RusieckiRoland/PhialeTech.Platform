using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PhialeTech.Components.Shared.Services;

namespace PhialeTech.Components.Wpf
{
    internal sealed class WpfDemoBookPdfExporter
    {
        private const double PageMarginPoints = 28d;
        private const double ChapterTopOffset = 120d;
        private const double ExampleHeaderHeight = 76d;

        public void Export(
            string outputPath,
            string documentTitle,
            IReadOnlyList<WpfDemoBookChapterCapture> chapters)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path is required.", nameof(outputPath));
            }

            if (chapters == null || chapters.Count == 0)
            {
                throw new InvalidOperationException("No chapters were provided for export.");
            }

            using (var document = new PdfDocument())
            {
                document.Info.Title = string.IsNullOrWhiteSpace(documentTitle) ? "PhialeTech Store book" : documentTitle;

                foreach (var chapter in chapters)
                {
                    AddChapterIntroPage(document, chapter);

                    foreach (var example in chapter.Examples)
                    {
                        AddExamplePages(document, chapter, example);
                    }
                }

                document.Save(outputPath);
            }
        }

        private static void AddChapterIntroPage(PdfDocument document, WpfDemoBookChapterCapture chapter)
        {
            var page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                var titleFont = new XFont("Arial", 24d, XFontStyleEx.Bold);
                var bodyFont = new XFont("Arial", 12d, XFontStyleEx.Regular);
                var chapterLabelFont = new XFont("Arial", 11d, XFontStyleEx.Bold);
                var bounds = new XRect(PageMarginPoints, PageMarginPoints, page.Width.Point - (PageMarginPoints * 2d), page.Height.Point - (PageMarginPoints * 2d));

                gfx.DrawString("Rozdział", chapterLabelFont, XBrushes.SteelBlue, new XRect(bounds.X, bounds.Y, bounds.Width, 18d), XStringFormats.TopLeft);
                gfx.DrawString(chapter.Title ?? string.Empty, titleFont, XBrushes.Black, new XRect(bounds.X, bounds.Y + 28d, bounds.Width, 34d), XStringFormats.TopLeft);
                gfx.DrawLine(XPens.LightSteelBlue, bounds.X, bounds.Y + 76d, bounds.Right, bounds.Y + 76d);
                gfx.DrawString(
                    chapter.Description ?? string.Empty,
                    bodyFont,
                    XBrushes.DimGray,
                    new XRect(bounds.X, bounds.Y + ChapterTopOffset, bounds.Width, bounds.Height - ChapterTopOffset),
                    XStringFormats.TopLeft);
            }
        }

        private static void AddExamplePages(PdfDocument document, WpfDemoBookChapterCapture chapter, WpfDemoBookExampleCapture example)
        {
            if (example?.Snapshot == null)
            {
                return;
            }

            var sourceWidth = example.ViewportWidth;
            var sourceHeight = example.ViewportHeight;
            if (sourceWidth <= 0d || sourceHeight <= 0d)
            {
                sourceWidth = example.Snapshot.PixelWidth * 96d / Math.Max(1d, example.Snapshot.DpiX);
                sourceHeight = example.Snapshot.PixelHeight * 96d / Math.Max(1d, example.Snapshot.DpiY);
            }

            var prototype = document.AddPage();
            prototype.Size = PageSize.A4;
            prototype.Orientation = PageOrientation.Landscape;
            var pageContentWidth = prototype.Width.Point - (PageMarginPoints * 2d);
            var pageContentHeight = prototype.Height.Point - (PageMarginPoints * 2d) - ExampleHeaderHeight;
            document.Pages.Remove(prototype);

            var slices = DemoPdfPaginationPlanner.PlanSlices(
                sourceWidth,
                sourceHeight,
                pageContentWidth,
                pageContentHeight);

            for (var index = 0; index < slices.Count; index++)
            {
                var slice = slices[index];
                var page = document.AddPage();
                page.Size = PageSize.A4;
                page.Orientation = PageOrientation.Landscape;

                using (var gfx = XGraphics.FromPdfPage(page))
                using (var image = XImage.FromBitmapSource(CreateSliceBitmap(example.Snapshot, example.ViewportHeight, slice.SourceOffsetY, slice.SourceHeight)))
                {
                    DrawExampleHeader(gfx, page, chapter, example, index == 0);
                    gfx.DrawImage(
                        image,
                        PageMarginPoints,
                        PageMarginPoints + ExampleHeaderHeight,
                        pageContentWidth,
                        slice.RenderedHeight);
                }
            }
        }

        private static void DrawExampleHeader(XGraphics gfx, PdfPage page, WpfDemoBookChapterCapture chapter, WpfDemoBookExampleCapture example, bool showDescription)
        {
            var chapterFont = new XFont("Arial", 10d, XFontStyleEx.Bold);
            var titleFont = new XFont("Arial", 18d, XFontStyleEx.Bold);
            var bodyFont = new XFont("Arial", 11d, XFontStyleEx.Regular);
            var width = page.Width.Point - (PageMarginPoints * 2d);

            gfx.DrawString(chapter.Title ?? string.Empty, chapterFont, XBrushes.SteelBlue, new XRect(PageMarginPoints, PageMarginPoints, width, 14d), XStringFormats.TopLeft);
            gfx.DrawString(example.Title ?? string.Empty, titleFont, XBrushes.Black, new XRect(PageMarginPoints, PageMarginPoints + 16d, width, 24d), XStringFormats.TopLeft);
            if (showDescription)
            {
                gfx.DrawString(example.Description ?? string.Empty, bodyFont, XBrushes.DimGray, new XRect(PageMarginPoints, PageMarginPoints + 42d, width, 30d), XStringFormats.TopLeft);
            }

            gfx.DrawLine(XPens.Gainsboro, PageMarginPoints, PageMarginPoints + ExampleHeaderHeight - 8d, page.Width.Point - PageMarginPoints, PageMarginPoints + ExampleHeaderHeight - 8d);
        }

        private static BitmapSource CreateSliceBitmap(BitmapSource source, double fullViewportHeight, double sourceOffsetY, double sourceHeight)
        {
            var pixelOffsetY = (int)Math.Round(source.PixelHeight * (sourceOffsetY / Math.Max(1d, fullViewportHeight)));
            var pixelHeight = Math.Max(1, (int)Math.Round(source.PixelHeight * (sourceHeight / Math.Max(1d, fullViewportHeight))));
            if (pixelOffsetY + pixelHeight > source.PixelHeight)
            {
                pixelHeight = Math.Max(1, source.PixelHeight - pixelOffsetY);
            }

            var cropped = new CroppedBitmap(source, new System.Windows.Int32Rect(0, pixelOffsetY, source.PixelWidth, pixelHeight));
            cropped.Freeze();
            return cropped;
        }
    }

    internal sealed class WpfDemoBookChapterCapture
    {
        public WpfDemoBookChapterCapture(string title, string description, IReadOnlyList<WpfDemoBookExampleCapture> examples)
        {
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Examples = examples ?? Array.Empty<WpfDemoBookExampleCapture>();
        }

        public string Title { get; }

        public string Description { get; }

        public IReadOnlyList<WpfDemoBookExampleCapture> Examples { get; }
    }

    internal sealed class WpfDemoBookExampleCapture
    {
        public WpfDemoBookExampleCapture(string title, string description, BitmapSource snapshot, double viewportWidth, double viewportHeight)
        {
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
        }

        public string Title { get; }

        public string Description { get; }

        public BitmapSource Snapshot { get; }

        public double ViewportWidth { get; }

        public double ViewportHeight { get; }
    }
}

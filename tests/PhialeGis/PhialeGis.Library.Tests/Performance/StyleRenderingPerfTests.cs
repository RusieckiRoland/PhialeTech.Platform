using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Renderer.Skia.Styling;
using PhialeGis.Library.Renderer.Skia.ViewportProjections;
using SkiaSharp;

namespace PhialeGis.Library.Tests.Performance
{
    [TestFixture]
    [NonParallelizable]
    [Category("Performance")]
    public sealed class StyleRenderingPerfTests
    {
        private const string RunPerfEnvVar = "PHIALEGIS_RUN_PERFORMANCE_TESTS";

        [Test]
        public void Render_10000_MixedStyledLines_ReportsElapsedAndAllocations()
        {
            RequirePerformanceRunEnabled();

            var lineCatalog = new InMemoryLineTypeCatalog();
            var symbolCatalog = new InMemorySymbolCatalog();

            Assert.That(lineCatalog.TryGet(BuiltInStyleIds.LineDash, out var dashLine), Is.True);
            Assert.That(lineCatalog.TryGet(BuiltInStyleIds.LineDoubleTrack, out var rasterLine), Is.True);
            Assert.That(lineCatalog.TryGet(BuiltInStyleIds.LineTicksPerpendicular, out var vectorLine), Is.True);
            Assert.That(symbolCatalog.TryGet(BuiltInStyleIds.SymbolTick, out var tickSymbol), Is.True);

            var symbolCache = new SymbolCache();
            var renderer = new SkiaLineStyleRenderer(symbolCache);
            var viewport = new PerfViewport();
            var projector = new SkiaViewportProjector(viewport);
            var workload = CreateLineWorkload(dashLine, rasterLine, vectorLine, tickSymbol);

            using var surface = SKSurface.Create(new SKImageInfo(2400, 900));
            Assert.That(surface, Is.Not.Null);
            var canvas = surface.Canvas;

            projector.PrepareMatrix();
            try
            {
                RenderLines(renderer, canvas, projector, workload);
                var metrics = Measure(() =>
                {
                    canvas.Clear(SKColors.White);
                    RenderLines(renderer, canvas, projector, workload);
                    canvas.Flush();
                });

                WriteMetrics("10k mixed styled lines", metrics);
                Assert.That(metrics.Elapsed, Is.LessThan(TimeSpan.FromSeconds(20)));
                Assert.That(metrics.AllocatedBytes, Is.LessThan(512L * 1024L * 1024L));
            }
            finally
            {
                projector.ReleaseMatrix();
            }
        }

        [Test]
        public void Render_10000_Symbols_ReportsElapsedAndAllocations()
        {
            RequirePerformanceRunEnabled();

            var symbolCatalog = new InMemorySymbolCatalog();
            Assert.That(symbolCatalog.TryGet(BuiltInStyleIds.SymbolSquare, out var squareSymbol), Is.True);
            Assert.That(symbolCatalog.TryGet(BuiltInStyleIds.SymbolTriangle, out var triangleSymbol), Is.True);

            var symbolCache = new SymbolCache();
            var renderer = new SkiaSymbolRenderer(symbolCache);
            var viewport = new PerfViewport();
            var requests = CreateSymbolWorkload(squareSymbol, triangleSymbol);

            using var surface = SKSurface.Create(new SKImageInfo(2400, 2400));
            Assert.That(surface, Is.Not.Null);
            var canvas = surface.Canvas;

            RenderSymbols(renderer, canvas, viewport, requests);
            var metrics = Measure(() =>
            {
                canvas.Clear(SKColors.White);
                RenderSymbols(renderer, canvas, viewport, requests);
                canvas.Flush();
            });

            WriteMetrics("10k symbols", metrics);
            Assert.That(metrics.Elapsed, Is.LessThan(TimeSpan.FromSeconds(20)));
            Assert.That(metrics.AllocatedBytes, Is.LessThan(256L * 1024L * 1024L));
        }

        private static void RequirePerformanceRunEnabled()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable(RunPerfEnvVar), "1", StringComparison.Ordinal))
            {
                Assert.Ignore($"Set {RunPerfEnvVar}=1 to execute performance tests.");
            }
        }

        private static PerfMetrics Measure(Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();
            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

            return new PerfMetrics(
                stopwatch.Elapsed,
                allocatedAfter - allocatedBefore,
                GC.CollectionCount(0) - gen0,
                GC.CollectionCount(1) - gen1,
                GC.CollectionCount(2) - gen2);
        }

        private static void WriteMetrics(string scenario, PerfMetrics metrics)
        {
            TestContext.Progress.WriteLine(
                $"[PERF] Scenario={scenario}; ElapsedMs={metrics.Elapsed.TotalMilliseconds:F2}; AllocatedBytes={metrics.AllocatedBytes}; Gen0={metrics.Gen0Collections}; Gen1={metrics.Gen1Collections}; Gen2={metrics.Gen2Collections}");
        }

        private static IReadOnlyList<LineWorkItem> CreateLineWorkload(
            LineTypeDefinition dashLine,
            LineTypeDefinition rasterLine,
            LineTypeDefinition vectorLine,
            SymbolDefinition tickSymbol)
        {
            var result = new List<LineWorkItem>(10_000);
            for (var i = 0; i < 10_000; i++)
            {
                var column = i % 100;
                var row = i / 100;
                var x = column * 22d;
                var y = row * 7d;
                var points = new List<PhPoint>(3)
                {
                    new PhPoint(x, y),
                    new PhPoint(x + 8d, y + 2d),
                    new PhPoint(x + 16d, y)
                };

                switch (i % 3)
                {
                    case 0:
                        result.Add(new LineWorkItem(points, dashLine, null));
                        break;
                    case 1:
                        result.Add(new LineWorkItem(points, rasterLine, null));
                        break;
                    default:
                        result.Add(new LineWorkItem(points, vectorLine, tickSymbol));
                        break;
                }
            }

            return result;
        }

        private static IReadOnlyList<SymbolRenderRequest> CreateSymbolWorkload(SymbolDefinition squareSymbol, SymbolDefinition triangleSymbol)
        {
            var result = new List<SymbolRenderRequest>(10_000);
            for (var i = 0; i < 10_000; i++)
            {
                var column = i % 100;
                var row = i / 100;
                result.Add(new SymbolRenderRequest
                {
                    ModelX = column * 20d + 8d,
                    ModelY = row * 20d + 8d,
                    Symbol = i % 2 == 0 ? squareSymbol : triangleSymbol,
                    Size = i % 2 == 0 ? 8d : 10d,
                    RotationDegrees = i % 2 == 0 ? 0d : 12d
                });
            }

            return result;
        }

        private static void RenderLines(
            SkiaLineStyleRenderer renderer,
            SKCanvas canvas,
            SkiaViewportProjector projector,
            IReadOnlyList<LineWorkItem> workload)
        {
            for (var i = 0; i < workload.Count; i++)
            {
                var item = workload[i];
                renderer.DrawPolyline(canvas, projector, item.Points, item.LineType, item.StampSymbol);
            }
        }

        private static void RenderSymbols(
            SkiaSymbolRenderer renderer,
            SKCanvas canvas,
            IViewport viewport,
            IReadOnlyList<SymbolRenderRequest> requests)
        {
            for (var i = 0; i < requests.Count; i++)
            {
                renderer.DrawSymbol(canvas, viewport, requests[i]);
            }
        }

        private sealed class PerfViewport : IViewport
        {
            public double Scale => 1d;

            public double GetDpiX() => 96d;

            public double GetDpiY() => 96d;

            public void ModelToScreen(double modelX, double modelY, out float screenX, out float screenY)
            {
                screenX = (float)modelX;
                screenY = (float)modelY;
            }

            public bool Zoom(double factor) => true;

            public bool PanByScreenOffset(double dx, double dy) => true;

            public void GetModelToScreenAffine(out double m11, out double m12, out double m21, out double m22, out double tx, out double ty)
            {
                m11 = 1d;
                m12 = 0d;
                m21 = 0d;
                m22 = 1d;
                tx = 0d;
                ty = 0d;
            }

            public void GetScreenToModelAffine(out double m11, out double m12, out double m21, out double m22, out double tx, out double ty)
            {
                m11 = 1d;
                m12 = 0d;
                m21 = 0d;
                m22 = 1d;
                tx = 0d;
                ty = 0d;
            }
        }

        private sealed class LineWorkItem
        {
            public LineWorkItem(IList<PhPoint> points, LineTypeDefinition lineType, SymbolDefinition stampSymbol)
            {
                Points = points;
                LineType = lineType;
                StampSymbol = stampSymbol;
            }

            public IList<PhPoint> Points { get; }

            public LineTypeDefinition LineType { get; }

            public SymbolDefinition StampSymbol { get; }
        }

        private sealed class PerfMetrics
        {
            public PerfMetrics(TimeSpan elapsed, long allocatedBytes, int gen0Collections, int gen1Collections, int gen2Collections)
            {
                Elapsed = elapsed;
                AllocatedBytes = allocatedBytes;
                Gen0Collections = gen0Collections;
                Gen1Collections = gen1Collections;
                Gen2Collections = gen2Collections;
            }

            public TimeSpan Elapsed { get; }

            public long AllocatedBytes { get; }

            public int Gen0Collections { get; }

            public int Gen1Collections { get; }

            public int Gen2Collections { get; }
        }
    }
}

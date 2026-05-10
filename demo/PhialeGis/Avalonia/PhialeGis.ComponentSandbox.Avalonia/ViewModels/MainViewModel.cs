// MainWindowViewModel.cs (Avalonia)
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using Avalonia.Platform.Storage;
using PhialeGis.ComponentSandbox.Avalonia.Core;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Sync.Io;
using PhialeGis.Library.Sync.Utils;

namespace PhialeGis.ComponentSandbox.Avalonia.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        public IGisInteractionManager GisInteractionManager { get; }
        public PhGis Gis { get; }

        public RelayCommand ChangeViewAction { get; }
        public RelayCommand ImportLayer { get; }
        public RelayCommand OpenSecondWindow { get; }

        public MainWindowViewModel(IGisInteractionManager gisInteractionManager, PhGis gis)
        {
            GisInteractionManager = gisInteractionManager ?? throw new ArgumentNullException(nameof(gisInteractionManager));
            Gis = gis ?? throw new ArgumentNullException(nameof(gis));

            ChangeViewAction = new RelayCommand(DoChangeViewAction);
            ImportLayer = new RelayCommand(async _ => await DoImportLayerAsync(null));
            OpenSecondWindow = new RelayCommand(DoOpenSecondWindow);

            Debug.WriteLine("[AVALONIA] VM created");
            GisInteractionManager.InvalidateAll();
        }

        private void DoOpenSecondWindow(object? param)
        {
            // var newMapWindow = new MapWithDslEditorWindow();
            // newMapWindow.Show();
        }

        private void DoChangeViewAction(object? param)
        {
            AvaDiag.LogHost(GetMainWindow());
            Debug.WriteLine("[AVALONIA] ChangeViewAction → ApplyVisualWindow(0,0,400,300)");
            GisInteractionManager.ApplyVisualWindow(0, 0, 400, 300);
            GisInteractionManager.InvalidateAll();
        }

        private async Task DoImportLayerAsync(object? param)
        {
            var owner = GetMainWindow();
            AvaDiag.LogHost(owner);

            if (owner?.StorageProvider == null)
            {
                Debug.WriteLine("[AVALONIA] FilePicker: no storage provider available");
                return;
            }

            var result = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select FlatGeobuf (*.fgb)",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("FlatGeobuf (*.fgb)") { Patterns = new[] { "*.fgb" } },
                    FilePickerFileTypes.All
                }
            });

            if (result == null || result.Count == 0)
            {
                Debug.WriteLine("[AVALONIA] FilePicker: cancelled");
                return;
            }

            var picked = result[0];
            var filePath = picked.TryGetLocalPath();
            try
            {
                PhLayer layer;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    using var stream = File.OpenRead(filePath);
                    layer = FgbLayerLoader.AddLayerFromFgb(Gis, stream, Path.GetFileNameWithoutExtension(filePath));
                }
                else
                {
                    await using var stream = await picked.OpenReadAsync();
                    layer = FgbLayerLoader.AddLayerFromFgb(Gis, stream, Path.GetFileNameWithoutExtension(picked.Name));
                }

                if (LayerBoundingBox.TryCompute(layer, out var bbox))
                {
                    var w = bbox.MaxX - bbox.MinX;
                    var h = bbox.MaxY - bbox.MinY;

                    AvaDiag.LogBBox("raw", bbox.MinX, bbox.MinY, bbox.MaxX, bbox.MaxY);

                    const double minSize = 1e-6;
                    if (w < minSize) { bbox.MinX -= 0.5; bbox.MaxX += 0.5; w = bbox.MaxX - bbox.MinX; }
                    if (h < minSize) { bbox.MinY -= 0.5; bbox.MaxY += 0.5; h = bbox.MaxY - bbox.MinY; }

                    const double pad = 0.05;
                    var x1 = bbox.MinX - w * pad;
                    var y1 = bbox.MinY - h * pad;
                    var x2 = bbox.MaxX + w * pad;
                    var y2 = bbox.MaxY + h * pad;

                    AvaDiag.LogBBox("padded", x1, y1, x2, y2);
                    AvaDiag.LogApplyWindow(x1, y1, x2, y2);

                    GisInteractionManager.ApplyVisualWindow(x1, y1, x2, y2);
                }
                else
                {
                    Debug.WriteLine("[AVALONIA] LayerBoundingBox.TryCompute: FALSE");
                }

                GisInteractionManager.InvalidateAll();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AVALONIA] DoImportLayerAsync EX: {ex}");
            }
        }

        private static Window? GetMainWindow()
        {
            return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        }

        // ====== DIAG ======
        private static class AvaDiag
        {
            public static void LogHost(Window? win)
            {
                try
                {
                    if (win == null)
                    {
                        Debug.WriteLine("[AVALONIA] Host: <null window>");
                        return;
                    }

                    var tl = TopLevel.GetTopLevel(win);
                    var rs = tl?.RenderScaling ?? 1.0;
                    var client = win.ClientSize;        // DIP
                    var bounds = win.Bounds;            // w praktyce DIP * rs
                    var screen = tl?.Screens?.ScreenFromVisual(tl);
                    var scaling = screen?.Scaling ?? rs;

                    Debug.WriteLine($"[AVALONIA] Host: ClientSize={client.Width:0.##}x{client.Height:0.##} DIP | " +
                                    $"Bounds={bounds.Width:0.##}x{bounds.Height:0.##} | " +
                                    $"RenderScaling={rs:0.##} | ScreenScaling={scaling:0.##}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AVALONIA] LogHost EX: {ex}");
                }
            }

            public static void LogBBox(string label, double minX, double minY, double maxX, double maxY)
            {
                var w = maxX - minX; var h = maxY - minY;
                Debug.WriteLine($"[AVALONIA] BBox[{label}]: X[{minX}, {maxX}] Y[{minY}, {maxY}] size=({w}, {h})");
            }

            public static void LogApplyWindow(double x1, double y1, double x2, double y2)
            {
                Debug.WriteLine($"[AVALONIA] ApplyVisualWindow: ({x1}, {y1}) → ({x2}, {y2}) size=({x2 - x1}, {y2 - y1})");
            }
        }
    }
}


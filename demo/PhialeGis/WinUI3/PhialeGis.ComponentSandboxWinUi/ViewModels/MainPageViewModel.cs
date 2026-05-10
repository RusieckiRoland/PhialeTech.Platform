// MainPageViewModel.cs (WinUI)
using Microsoft.UI.Xaml;
using PhialeGis.ComponentSandboxWinUi.Core;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Sync.Io;
using PhialeGis.Library.Sync.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinRT.Interop;

namespace PhialeGis.ComponentSandboxWinUi.ViewModels
{
    public sealed class MainPageViewModel : ViewModelBase
    {
        private readonly ISecondaryViewService _secondaryViewService;
        private readonly IFilePickerService _filePicker;

        public IGisInteractionManager GisInteractionManager { get; }
        public RelayCommand ChangeViewAction { get; }
        public RelayCommand ImportLayer { get; }
        public RelayCommand OpenSecondWindow { get; }
        public PhGis Gis { get; }

        public MainPageViewModel(
            IGisInteractionManager gisInteractionManager,
            PhGis gis,
            ISecondaryViewService secondaryViewService,
            IFilePickerService filePicker)
        {
            GisInteractionManager = gisInteractionManager ?? throw new ArgumentNullException(nameof(gisInteractionManager));
            Gis = gis ?? throw new ArgumentNullException(nameof(gis));
            _secondaryViewService = secondaryViewService ?? throw new ArgumentNullException(nameof(secondaryViewService));
            _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));

            ChangeViewAction = new RelayCommand(DoChangeViewAction);
            ImportLayer = new RelayCommand(async () => await DoImportLayerAsync());
            OpenSecondWindow = new RelayCommand(async () => await _secondaryViewService.OpenMapWindowAsync());

            Debug.WriteLine("[WINUI] VM created");
            GisInteractionManager.InvalidateAll();
        }

        private async Task DoImportLayerAsync()
        {
            var owner = ((App)Application.Current).MainAppWindow;
            WinUiDiag.LogHost(owner);

            var stream = await _filePicker.PickFgbAsync(owner);
            if (stream is null)
            {
                Debug.WriteLine("[WINUI] FilePicker: cancelled");
                return;
            }

            try
            {
                PhLayer layer;
                using (stream)
                {
                    layer = FgbLayerLoader.AddLayerFromFgb(Gis, stream, "Imported");
                }

                if (LayerBoundingBox.TryCompute(layer, out var bbox))
                {
                    var w = bbox.MaxX - bbox.MinX;
                    var h = bbox.MaxY - bbox.MinY;

                    WinUiDiag.LogBBox("raw", bbox.MinX, bbox.MinY, bbox.MaxX, bbox.MaxY);

                    const double minSize = 1e-6;
                    if (w < minSize) { bbox.MinX -= 0.5; bbox.MaxX += 0.5; w = bbox.MaxX - bbox.MinX; }
                    if (h < minSize) { bbox.MinY -= 0.5; bbox.MaxY += 0.5; h = bbox.MaxY - bbox.MinY; }

                    const double pad = 0.05;
                    var x1 = bbox.MinX - w * pad;
                    var y1 = bbox.MinY - h * pad;
                    var x2 = bbox.MaxX + w * pad;
                    var y2 = bbox.MaxY + h * pad;

                    WinUiDiag.LogBBox("padded", x1, y1, x2, y2);
                    WinUiDiag.LogApplyWindow(x1, y1, x2, y2);

                    GisInteractionManager.ApplyVisualWindow(x1, y1, x2 - x1, y2 - y1);
                }
                else
                {
                    Debug.WriteLine("[WINUI] LayerBoundingBox.TryCompute: FALSE");
                }

                GisInteractionManager.InvalidateAll();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WINUI] DoImportLayerAsync EX: {ex}");
            }
        }

        private void DoChangeViewAction()
        {
            var owner = ((App)Application.Current).MainAppWindow;
            WinUiDiag.LogHost(owner);

            Debug.WriteLine("[WINUI] ChangeViewAction → ApplyVisualWindow(0,0,400,300)");
            GisInteractionManager.ApplyVisualWindow(0, 0, 400, 300);
            GisInteractionManager.InvalidateAll();
        }

        // ====== DIAG ======
        private static class WinUiDiag
        {
            [DllImport("user32.dll")]
            private static extern uint GetDpiForWindow(IntPtr hWnd);

            public static void LogHost(Window owner)
            {
                try
                {
                    var hwnd = WindowNative.GetWindowHandle(owner);
                    uint dpi = 0;
                    try { dpi = GetDpiForWindow(hwnd); } catch { /* ok on older systems */ }

                    var appWin = owner.AppWindow;
                    var aw = appWin?.Size.Width ?? -1;
                    var ah = appWin?.Size.Height ?? -1;

                    var root = owner.Content as FrameworkElement;
                    var vw = root?.ActualWidth ?? double.NaN;
                    var vh = root?.ActualHeight ?? double.NaN;

                    var rs = (dpi > 0) ? dpi / 96.0 : 1.0;

                    // fallback, żeby zawsze mieć liczby jak w Avalonia
                    var clientWidth = double.IsNaN(vw) || vw <= 0 ? aw / rs : vw;
                    var clientHeight = double.IsNaN(vh) || vh <= 0 ? ah / rs : vh;
                    var boundsWidth = aw / rs;
                    var boundsHeight = ah / rs;

                    Debug.WriteLine($"[WINUI] Host: ClientSize={clientWidth:0.##}x{clientHeight:0.##} DIP | " +
                                    $"Bounds={boundsWidth:0.##}x{boundsHeight:0.##} | " +
                                    $"RenderScaling={rs:0.##} | ScreenScaling={(dpi / 96.0):0.##}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WINUI] LogHost EX: {ex}");
                }
            }

            public static void LogBBox(string label, double minX, double minY, double maxX, double maxY)
            {
                var w = maxX - minX;
                var h = maxY - minY;
                Debug.WriteLine($"[WINUI] BBox[{label}]: X[{minX}, {maxX}] Y[{minY}, {maxY}] size=({w}, {h})");
            }

            public static void LogApplyWindow(double x1, double y1, double x2, double y2)
            {
                Debug.WriteLine($"[WINUI] ApplyVisualWindow: ({x1}, {y1}) → ({x2}, {y2}) size=({x2 - x1}, {y2 - y1})");
            }
        }
    }
}


using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;                               // OpenFileDialog for WPF

using PhialeGis.ComponentSandbox.Core;              // ISecondaryViewService (WPF variant)
using PhialeGis.ComponentSandbox.Views;
using PhialeGis.Library.Abstractions.Interactions;  // IGisInteractionManager, RelayCommand
using PhialeGis.Library.Domain.Map;                 // PhGis, PhLayer
using PhialeGis.Library.Sync.Io;                    // FgbLayerLoader
using PhialeGis.Library.Sync.Utils;                 // LayerBoundingBox

namespace PhialeGis.ComponentSandbox.ViewModels
{
    /// <summary>
    /// WPF mirror of the WinUI MainPageViewModel.
    /// Commands: ChangeViewAction, ImportLayer, OpenSecondWindow.
    /// </summary>
    public sealed class MainWindowViewModel : ViewModelBase
    {
        

        public IGisInteractionManager GisInteractionManager { get; }
        public PhGis Gis { get; }

        public RelayCommand ChangeViewAction { get; }
        public RelayCommand ImportLayer { get; }
        public RelayCommand OpenSecondWindow { get; }

        public MainWindowViewModel(
            IGisInteractionManager gisInteractionManager,
            PhGis gis
            )
        {
            GisInteractionManager = gisInteractionManager ?? throw new ArgumentNullException(nameof(gisInteractionManager));
            Gis = gis ?? throw new ArgumentNullException(nameof(gis));
            

            ChangeViewAction = new RelayCommand(DoChangeViewAction);
            ImportLayer = new RelayCommand(async _ => await DoImportLayerAsync(null));
            OpenSecondWindow = new RelayCommand(DoOpenSecondWindow);

            // IMPORTANT: AttachPhGis is done earlier (e.g., in App).
            // Here we only request an initial redraw.
            GisInteractionManager.InvalidateAll();
        }

        private void DoOpenSecondWindow(Object? param)
        {
            var newMapWindow = new MapWithDslEditorWindow();
            newMapWindow.Show();
        }

        /// <summary>
        /// Simple demo view change to match WinUI sample.
        /// </summary>
        private void DoChangeViewAction(object? param)
        {
            GisInteractionManager.ApplyVisualWindow(0, 0, 400, 300);
            
        }

        /// <summary>
        /// Imports an FGB layer via standard WPF OpenFileDialog and zooms to its bounding box.
        /// </summary>
        private async Task DoImportLayerAsync(object? param)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Select FlatGeobuf (*.fgb)",
                Filter = "FlatGeobuf (*.fgb)|*.fgb|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            var result = ofd.ShowDialog();
            if (result != true || string.IsNullOrWhiteSpace(ofd.FileName))
                return;

            try
            {
                PhLayer layer;
                using (var stream = File.OpenRead(ofd.FileName))
                {
                    // Load layer from FGB stream
                    layer = FgbLayerLoader.AddLayerFromFgb(Gis, stream, Path.GetFileNameWithoutExtension(ofd.FileName));
                }

                // Compute bbox and apply a padded visual window
                if (LayerBoundingBox.TryCompute(layer, out var bbox))
                {
                    var w = bbox.MaxX - bbox.MinX;
                    var h = bbox.MaxY - bbox.MinY;

                    // Guard against degenerate extents
                    const double minSize = 1e-6;
                    if (w < minSize) { bbox.MinX -= 0.5; bbox.MaxX += 0.5; w = bbox.MaxX - bbox.MinX; }
                    if (h < minSize) { bbox.MinY -= 0.5; bbox.MaxY += 0.5; h = bbox.MaxY - bbox.MinY; }

                    const double pad = 0.05; // 5% padding
                    GisInteractionManager.ApplyVisualWindow(
                        bbox.MinX - w * pad,
                        bbox.MinY - h * pad,
                        bbox.MaxX + w * pad,
                        bbox.MaxY + h * pad);
                }

                GisInteractionManager.InvalidateAll();
                await Task.CompletedTask; // keep async signature consistent
            }
            catch (Exception ex)
            {
                // Keep it simple for now; hook into your logger if needed
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}


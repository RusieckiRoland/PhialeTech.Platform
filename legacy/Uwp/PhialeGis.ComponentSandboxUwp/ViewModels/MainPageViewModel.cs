using PhialeGis.ComponentSandboxUwp.Core;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Sync.Io;
using PhialeGis.Library.Sync.Utils;
using System;
using System.IO;
using System.Threading.Tasks; // use Task alias
using Windows.Storage.Pickers;

namespace PhialeGis.ComponentSandboxUwp.ViewModels
{
    /// <summary>
    /// Main page ViewModel driving the map and DSL editor.
    /// Keeps platform-specific windowing in ISecondaryViewService.
    /// </summary>
    internal class MainPageViewModel : ViewModelBase
    {
        private readonly ISecondaryViewService _secondaryViewService;

        public IGisInteractionManager GisInteractionManager { get; }
        public RelayCommand ChangeViewAction { get; }
        public RelayCommand ImportLayer { get; }
        public RelayCommand OpenSecondWindow { get; }   // NEW
        public PhGis Gis { get; }

        public MainPageViewModel(
            IGisInteractionManager gisInteractionManager,
            PhGis gis,
            ISecondaryViewService secondaryViewService) // NEW
        {
            GisInteractionManager = gisInteractionManager ?? throw new ArgumentNullException(nameof(gisInteractionManager));
            Gis = gis ?? throw new ArgumentNullException(nameof(gis));
            _secondaryViewService = secondaryViewService ?? throw new ArgumentNullException(nameof(secondaryViewService));

            ChangeViewAction = new RelayCommand(DoChangeViewAction);

            // Important: async lambda for ICommand
            ImportLayer = new RelayCommand(async () => await DoImportLayerAsync());

            // Open a new window hosting another map (second viewport)
            OpenSecondWindow = new RelayCommand(async () => await _secondaryViewService.OpenMapWindowAsync()); // NEW

            GisInteractionManager.InvalidateAll();
        }

        /// <summary>
        /// Opens a file and imports a layer into the GIS model.
        /// After import, fits the visual window around the layer bbox with a small padding.
        /// </summary>
        private async Task DoImportLayerAsync()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".fgb");

            var file = await picker.PickSingleFileAsync();
            if (file == null) return; // user canceled

            try
            {
                PhLayer layer;
                using (var s = await file.OpenStreamForReadAsync())
                {
                    var name = Path.GetFileNameWithoutExtension(file.Name);
                    layer = FgbLayerLoader.AddLayerFromFgb(Gis, s, name);
                }

                if (LayerBoundingBox.TryCompute(layer, out var bbox))
                {
                    var w = bbox.MaxX - bbox.MinX;
                    var h = bbox.MaxY - bbox.MinY;

                    // Minimum window size if bbox degenerates to a point/line.
                    const double minSize = 1e-6;
                    if (w < minSize) { bbox.MinX -= 0.5; bbox.MaxX += 0.5; w = bbox.MaxX - bbox.MinX; }
                    if (h < minSize) { bbox.MinY -= 0.5; bbox.MaxY += 0.5; h = bbox.MaxY - bbox.MinY; }

                    const double pad = 0.05;
                    GisInteractionManager.ApplyVisualWindow(
                        bbox.MinX - w * pad,
                        bbox.MinY - h * pad,
                        bbox.MaxX + w * pad,
                        bbox.MaxY + h * pad);
                }

                GisInteractionManager.InvalidateAll();
            }
            catch (InvalidOperationException ex)
            {
                // For example: PH_FGB not defined or missing packages – log for now.
                System.Diagnostics.Debug.WriteLine(ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Demonstration action: sets a fixed visual window.
        /// </summary>
        private void DoChangeViewAction()
        {
            GisInteractionManager.ApplyVisualWindow(0, 0, 400, 300);
        }
    }
}

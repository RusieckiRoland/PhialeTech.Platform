using System;

using PhialeGis.ComponentSandbox.Core;              // ISecondaryViewService (WPF variant)
using PhialeGis.Library.Abstractions.Interactions;  // IGisInteractionManager, RelayCommand
using PhialeGis.Library.Domain.Map;                 // PhGis, PhLayer

namespace PhialeGis.ComponentSandbox.ViewModels
{
    /// <summary>
    /// WPF mirror of the WinUI MainPageViewModel.
    /// Commands: ChangeViewAction, ImportLayer, OpenSecondWindow.
    /// </summary>
    public sealed class MapWithDslEditorViewModel : ViewModelBase
    {


        public IGisInteractionManager GisInteractionManager { get; }
        public PhGis Gis { get; }

        public RelayCommand ChangeViewAction { get; }
        public RelayCommand ImportLayer { get; }
        public RelayCommand OpenSecondWindow { get; }


        public MapWithDslEditorViewModel(
            IGisInteractionManager gisInteractionManager,
            PhGis gis
            )
        {
            GisInteractionManager = gisInteractionManager ?? throw new ArgumentNullException(nameof(gisInteractionManager));
            Gis = gis ?? throw new ArgumentNullException(nameof(gis));

            GisInteractionManager.InvalidateAll();
        }

    }
}

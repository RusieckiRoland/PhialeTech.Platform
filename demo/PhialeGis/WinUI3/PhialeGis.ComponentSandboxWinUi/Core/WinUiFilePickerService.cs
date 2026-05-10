// Services/WinUiFilePickerService.cs
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;                 // InitializeWithWindow / WindowNative

namespace PhialeGis.ComponentSandboxWinUi.Core
{
    internal sealed class WinUiFilePickerService : IFilePickerService
    {
        private readonly Microsoft.UI.Xaml.Window _window;

        public WinUiFilePickerService(Microsoft.UI.Xaml.Window window)
        {
            _window = window;
        }

        public async Task<Stream?> PickFgbAsync(Window owner)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".fgb");

            // WinUI 3: musimy zainicjalizować pickera uchwytem okna
            var hwnd = WindowNative.GetWindowHandle(owner);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is null) return null;

            return await file.OpenStreamForReadAsync();
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Shared;

namespace PhialeTech.Components.Avalonia
{
    internal static class DemoCefRuntime
    {
        private static bool _isInitialized;

        public static void EnsureInitialized()
        {
            if (_isInitialized || CefRuntimeLoader.IsLoaded)
            {
                _isInitialized = true;
                return;
            }

            string baseDirectory = AppContext.BaseDirectory;
            string processName = OperatingSystem.IsWindows()
                ? "Xilium.CefGlue.BrowserProcess.exe"
                : "Xilium.CefGlue.BrowserProcess";
            string browserProcessPath = Path.Combine(baseDirectory, "CefGlueBrowserProcess", processName);
            string cacheRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PhialeTech",
                "Demo",
                "CefCache");

            Directory.CreateDirectory(cacheRoot);

            var settings = new CefSettings
            {
                NoSandbox = true,
                WindowlessRenderingEnabled = true,
                RootCachePath = cacheRoot
            };

            if (File.Exists(browserProcessPath))
            {
                settings.BrowserSubprocessPath = browserProcessPath;
            }

            CefRuntimeLoader.Initialize(
                settings,
                Array.Empty<KeyValuePair<string, string>>(),
                Array.Empty<CustomScheme>());

            _isInitialized = true;
        }
    }
}

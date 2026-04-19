using Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Shared;

namespace PhialeGis.ComponentSandbox.Avalonia;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        StartupTrace.Log($"Main: enter; baseDir={AppContext.BaseDirectory}");
        StartupTrace.Log($"Main: logFile={StartupTrace.LogFilePath}");
        StartupTrace.Log($"Main: cmd={Environment.CommandLine}");
        StartupTrace.Log($"Main: asmPath={typeof(Program).Assembly.Location}");
        StartupTrace.Log($"Main: asmHasDiagV2={AssemblyContainsMarker(typeof(Program).Assembly.Location, "diag-v2")}");

        EnsureWslGuiEnvironment();
        StartupTrace.Log($"Main: env DISPLAY={Environment.GetEnvironmentVariable("DISPLAY") ?? "<null>"}");
        StartupTrace.Log($"Main: env WAYLAND_DISPLAY={Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") ?? "<null>"}");
        StartupTrace.Log($"Main: env XDG_RUNTIME_DIR={Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") ?? "<null>"}");

        try
        {
            InitializeCefGlue();
            StartupTrace.Log("Main: CEF initialized");
        }
        catch (Exception ex)
        {
            StartupTrace.Log($"Main: CEF init failed: {ex}");
            throw;
        }

        try
        {
            var exitCode = BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
            StartupTrace.Log($"Main: lifetime ended; exitCode={exitCode}");
        }
        catch (Exception ex)
        {
            StartupTrace.Log($"Main: lifetime failed: {ex}");
            throw;
        }
    }

    private static bool AssemblyContainsMarker(string asmPath, string marker)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(asmPath) || !File.Exists(asmPath))
                return false;

            var bytes = File.ReadAllBytes(asmPath);
            var text = Encoding.Unicode.GetString(bytes);
            return text.Contains(marker, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .LogToTrace();

    private static void EnsureWslGuiEnvironment()
    {
        if (!OperatingSystem.IsLinux())
            return;

        var wslDistro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
        if (string.IsNullOrWhiteSpace(wslDistro))
            return;

        var display = Environment.GetEnvironmentVariable("DISPLAY");
        var wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        var runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");

        if (string.IsNullOrWhiteSpace(runtimeDir))
        {
            runtimeDir = TryResolveRuntimeDir();
            if (!string.IsNullOrWhiteSpace(runtimeDir))
                Environment.SetEnvironmentVariable("XDG_RUNTIME_DIR", runtimeDir);
        }

        if (string.IsNullOrWhiteSpace(wayland) && !string.IsNullOrWhiteSpace(runtimeDir))
        {
            var defaultWaylandSocket = Path.Combine(runtimeDir, "wayland-0");
            if (File.Exists(defaultWaylandSocket))
            {
                Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", "wayland-0");
            }
        }

        if (string.IsNullOrWhiteSpace(display) && Directory.Exists("/tmp/.X11-unix"))
        {
            Environment.SetEnvironmentVariable("DISPLAY", ":0");
        }
    }

    private static string? TryResolveRuntimeDir()
    {
        const string runUserRoot = "/run/user";
        if (Directory.Exists(runUserRoot))
        {
            foreach (var candidate in Directory.GetDirectories(runUserRoot))
            {
                var name = Path.GetFileName(candidate);
                if (!string.IsNullOrWhiteSpace(name) && int.TryParse(name, out _))
                    return candidate;
            }
        }

        const string wslgRuntimeDir = "/mnt/wslg/runtime-dir";
        if (Directory.Exists(wslgRuntimeDir))
            return wslgRuntimeDir;

        return null;
    }

    private static void InitializeCefGlue()
    {
        if (CefRuntimeLoader.IsLoaded)
        {
            StartupTrace.Log("CEF: runtime already loaded");
            return;
        }

        var baseDir = AppContext.BaseDirectory;
        var processName = OperatingSystem.IsWindows()
            ? "Xilium.CefGlue.BrowserProcess.exe"
            : "Xilium.CefGlue.BrowserProcess";
        var browserProcessPath = Path.Combine(baseDir, "CefGlueBrowserProcess", processName);
        StartupTrace.Log($"CEF: browserProcessPath={browserProcessPath}; exists={File.Exists(browserProcessPath)}");

        var cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhialeGis",
            "CefCache");
        Directory.CreateDirectory(cacheRoot);
        StartupTrace.Log($"CEF: cacheRoot={cacheRoot}");

        var settings = new CefSettings
        {
            NoSandbox = true,
            WindowlessRenderingEnabled = true,
            RootCachePath = cacheRoot
        };

        if (File.Exists(browserProcessPath))
            settings.BrowserSubprocessPath = browserProcessPath;

        CefRuntimeLoader.Initialize(
            settings,
            Array.Empty<KeyValuePair<string, string>>(),
            Array.Empty<CustomScheme>());

        StartupTrace.Log($"CEF: init done; IsLoaded={CefRuntimeLoader.IsLoaded}");
    }
}

internal static class StartupTrace
{
    private static readonly object Sync = new();
    public static readonly string LogFilePath = Path.Combine(
        Environment.CurrentDirectory,
        "phialegis-avalonia-startup.log");

    public static void Log(string message)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Environment.ProcessId}] {message}{Environment.NewLine}";
            lock (Sync)
            {
                File.AppendAllText(LogFilePath, line);
            }
        }
        catch
        {
            // diagnostics must never break startup
        }
    }
}

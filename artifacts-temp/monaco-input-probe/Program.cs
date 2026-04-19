using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PhialeTech.Components.Wpf;
using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.MonacoEditor.Wpf.Controls;
using PhialeTech.WebHost.Wpf;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        int exitCode = 1;

        try
        {
            var app = new Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
            app.Startup += async (_, __) =>
            {
                try
                {
                    bool showcaseWorks = await ProbeShowcaseEditorAsync().ConfigureAwait(true);
                    bool yamlWorks = await ProbeYamlEditorAsync().ConfigureAwait(true);

                    Console.WriteLine("Showcase editor typing works: " + showcaseWorks);
                    Console.WriteLine("YAML document editor typing works: " + yamlWorks);
                    exitCode = 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Probe failed: " + ex);
                    exitCode = 1;
                }
                finally
                {
                    app.Shutdown();
                }
            };

            app.Run();
            return exitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Probe failed: " + ex);
            return 1;
        }
    }

    private static async Task<bool> ProbeShowcaseEditorAsync()
    {
        var showcase = new MonacoEditorShowcaseView("en", "light");
        var editor = FindPrivateField<PhialeMonacoEditor>(showcase, "_editor");
        if (editor == null)
        {
            throw new InvalidOperationException("Could not access showcase Monaco editor.");
        }

        var window = CreateHostWindow(showcase, "Showcase probe");

        try
        {
            window.Show();
            await WaitForIdleAsync(window.Dispatcher, 300).ConfigureAwait(true);
            await EnsureMonacoReadyAsync(editor).ConfigureAwait(true);
            await editor.SetValueAsync(string.Empty).ConfigureAwait(true);
            FocusEditor(window, editor);
            await WaitForIdleAsync(window.Dispatcher, 300).ConfigureAwait(true);
            SendTextToActiveWindow("abc");
            await WaitForIdleAsync(window.Dispatcher, 600).ConfigureAwait(true);

            var value = await editor.GetValueAsync().ConfigureAwait(true) ?? string.Empty;
            Console.WriteLine("Showcase editor value: [" + Escape(value) + "]");
            return value.Contains("abc", StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
            await WaitForIdleAsync(window.Dispatcher, 200).ConfigureAwait(true);
        }
    }

    private static async Task<bool> ProbeYamlEditorAsync()
    {
        var editor = new PhialeMonacoEditor(
            new WpfWebComponentHostFactory(),
            new MonacoEditorOptions
            {
                InitialTheme = "light",
                InitialLanguage = "yaml",
                InitialValue = string.Empty,
            });

        var root = new Grid();
        root.Children.Add(editor);
        var window = CreateHostWindow(root, "Yaml probe");

        try
        {
            window.Show();
            await WaitForIdleAsync(window.Dispatcher, 300).ConfigureAwait(true);
            await EnsureMonacoReadyAsync(editor).ConfigureAwait(true);
            await editor.SetValueAsync(string.Empty).ConfigureAwait(true);
            FocusEditor(window, editor);
            await WaitForIdleAsync(window.Dispatcher, 300).ConfigureAwait(true);
            SendTextToActiveWindow("xyz");
            await WaitForIdleAsync(window.Dispatcher, 600).ConfigureAwait(true);

            var value = await editor.GetValueAsync().ConfigureAwait(true) ?? string.Empty;
            Console.WriteLine("YAML editor value: [" + Escape(value) + "]");
            return value.Contains("xyz", StringComparison.Ordinal);
        }
        finally
        {
            editor.Dispose();
            window.Close();
            await WaitForIdleAsync(window.Dispatcher, 200).ConfigureAwait(true);
        }
    }

    private static Window CreateHostWindow(FrameworkElement content, string title)
    {
        return new Window
        {
            Title = title,
            Width = 1280,
            Height = 720,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = content,
            ShowInTaskbar = true,
        };
    }

    private static async Task EnsureMonacoReadyAsync(PhialeMonacoEditor editor)
    {
        await editor.InitializeAsync().ConfigureAwait(true);
        await editor.SetThemeAsync("light").ConfigureAwait(true);
        await editor.SetLanguageAsync("yaml").ConfigureAwait(true);
    }

    private static void FocusEditor(Window window, PhialeMonacoEditor editor)
    {
        window.Dispatcher.Invoke(() =>
        {
            window.Activate();
            window.Focus();
            editor.FocusEditor();
        });
    }

    private static T FindPrivateField<T>(object instance, string fieldName)
        where T : class
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(instance) as T;
    }

    private static async Task WaitForIdleAsync(Dispatcher dispatcher, int milliseconds)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < milliseconds)
        {
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(15).ConfigureAwait(true);
        }
    }

    private static void SendTextToActiveWindow(string text)
    {
        foreach (var character in text)
        {
            var key = VkKeyScan(character);
            if (key == -1)
            {
                throw new InvalidOperationException("Unable to translate character '" + character + "'.");
            }

            var virtualKey = (byte)(key & 0xff);
            var shiftState = (byte)((key >> 8) & 0xff);
            var useShift = (shiftState & 1) == 1;

            if (useShift)
            {
                keybd_event((byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift), 0, 0, UIntPtr.Zero);
            }

            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
            keybd_event(virtualKey, 0, KeyEventFKeyUp, UIntPtr.Zero);

            if (useShift)
            {
                keybd_event((byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift), 0, KeyEventFKeyUp, UIntPtr.Zero);
            }

            Thread.Sleep(25);
        }
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private const uint KeyEventFKeyUp = 0x0002;

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}

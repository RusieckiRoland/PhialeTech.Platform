using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeTech.Components.Wpf;
using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.MonacoEditor.Wpf.Controls;
using PhialeTech.WebHost.Wpf;

namespace PhialeTech.WebHost.Wpf.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public sealed class MonacoEditorKeyboardInputLiveTests
    {
        [Test]
        [Explicit("Requires an interactive Windows desktop because it sends real keyboard input to a live WebView2 WPF window.")]
        public void MonacoEditorShowcaseView_WhenFocused_AcceptsRealKeyboardInput()
        {
            var showcase = new MonacoEditorShowcaseView("en", "light");
            var editor = FindPrivateField<PhialeMonacoEditor>(showcase, "_editor");
            Assert.That(editor, Is.Not.Null);

            var window = CreateHostWindow(showcase);

            try
            {
                window.Show();
                FlushDispatcher(window);

                EnsureMonacoReadyAsync(editor, string.Empty).GetAwaiter().GetResult();
                FocusEditor(window, editor);
                FlushDispatcher(window);

                SendTextToActiveWindow("abc");
                FlushDispatcher(window);
                WaitForIdle(window.Dispatcher, 250);

                var value = editor.GetValueAsync().GetAwaiter().GetResult() ?? string.Empty;
                Assert.That(value, Does.Contain("abc"), "Focused Monaco showcase editor should accept real keyboard input.");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        [Explicit("Requires an interactive Windows desktop because it sends real keyboard input to a live WebView2 WPF window.")]
        public void YamlDocumentEditor_WhenFocused_AcceptsRealKeyboardInput()
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
            var window = CreateHostWindow(root);

            try
            {
                window.Show();
                FlushDispatcher(window);

                EnsureMonacoReadyAsync(editor, string.Empty).GetAwaiter().GetResult();
                FocusEditor(window, editor);
                FlushDispatcher(window);

                SendTextToActiveWindow("xyz");
                FlushDispatcher(window);
                WaitForIdle(window.Dispatcher, 250);

                var value = editor.GetValueAsync().GetAwaiter().GetResult() ?? string.Empty;
                Assert.That(value, Does.Contain("xyz"), "Focused WPF-hosted Monaco editor should accept real keyboard input.");
            }
            finally
            {
                editor.Dispose();
                window.Close();
            }
        }

        private static Window CreateHostWindow(FrameworkElement content)
        {
            return new Window
            {
                Width = 1280,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = content,
                ShowInTaskbar = true,
            };
        }

        private static async Task EnsureMonacoReadyAsync(PhialeMonacoEditor editor, string initialValue)
        {
            Assert.That(editor, Is.Not.Null);

            await editor.InitializeAsync().ConfigureAwait(true);
            await editor.SetThemeAsync("light").ConfigureAwait(true);
            await editor.SetLanguageAsync("yaml").ConfigureAwait(true);
            await editor.SetValueAsync(initialValue ?? string.Empty).ConfigureAwait(true);
        }

        private static void FocusEditor(Window window, PhialeMonacoEditor editor)
        {
            window.Dispatcher.Invoke(() =>
            {
                window.Activate();
                window.Focus();
                editor.FocusEditor();
                Keyboard.Focus(window);
            });
        }

        private static T FindPrivateField<T>(object instance, string fieldName)
            where T : class
        {
            if (instance == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return null;
            }

            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(instance) as T;
        }

        private static void FlushDispatcher(DispatcherObject dispatcherObject)
        {
            Assert.That(dispatcherObject, Is.Not.Null);
            dispatcherObject.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcherObject.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        private static void WaitForIdle(Dispatcher dispatcher, int milliseconds)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);
            while (DateTime.UtcNow < deadline)
            {
                dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                Thread.Sleep(15);
            }
        }

        private static void SendTextToActiveWindow(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (var character in text)
            {
                var key = VkKeyScan(character);
                Assert.That(key, Is.Not.EqualTo((short)-1), "Unable to translate character '" + character + "' to a virtual key.");

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

                Thread.Sleep(20);
            }
        }

        private const uint KeyEventFKeyUp = 0x0002;

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    }
}

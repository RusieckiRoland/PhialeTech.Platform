using Avalonia.Controls;
using System;

namespace PhialeGis.ComponentSandbox.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        StartupTrace.Log("MainWindow.ctor: begin [diag-v2]");
        InitializeComponent();
        Opened += OnOpened;
        StartupTrace.Log("MainWindow.ctor: end [diag-v2]");
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        StartupTrace.Log("MainWindow.OnOpened: entered");

        try
        {
            var editor = this.FindControl<Control>("DslEditor");
            if (editor == null)
            {
                StartupTrace.Log("MainWindow.OnOpened: DslEditor control not found");
                return;
            }

            var t = editor.GetType();
            var asm = t.Assembly;
            StartupTrace.Log(
                $"MainWindow.OnOpened: DslEditor type={t.FullName}; asm={asm.GetName().Name}; loc={asm.Location}");

            var gimProp = t.GetProperty("GisInteractionManager");
            var gim = gimProp?.GetValue(editor);
            StartupTrace.Log(
                $"MainWindow.OnOpened: DslEditor.GisInteractionManager={(gim == null ? "<null>" : gim.GetType().FullName)}");
        }
        catch (Exception ex)
        {
            StartupTrace.Log("MainWindow.OnOpened: diag failed: " + ex);
        }
    }
}


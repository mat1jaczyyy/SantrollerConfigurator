using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;

namespace GuitarConfigurator.NetCore;

public static class ProgramWindowsDebug
{
    public static void Main(string[] args)
    {
        var tr1 = new TextWriterTraceListener(Console.Out);
        Trace.Listeners.Add(tr1);
        Directory.CreateDirectory(AssetUtils.GetAppDataFolder());
        var tr2 = new TextWriterTraceListener(File.CreateText(Path.Combine(AssetUtils.GetAppDataFolder(),
            "build.log")));
        Trace.Listeners.Add(tr2);
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
            // Make sure we kill all python processes on exit
            var lifetime = (ClassicDesktopStyleApplicationLifetime) Application.Current!.ApplicationLifetime!;
            lifetime.Exit += PlatformIo.Exit;
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            PlatformIo.Exit(null, new ControlledApplicationLifetimeExitEventArgs(0));
        }

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, _) =>
            PlatformIo.Exit(sender, new ControlledApplicationLifetimeExitEventArgs(0));
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
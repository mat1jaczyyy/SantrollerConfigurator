using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

namespace GuitarConfigurator.NetCore;

public static class Program
{
    public static void Main(string[] args)
    {
        Directory.CreateDirectory(AssetUtils.GetAppDataFolder());
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
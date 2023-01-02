using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

namespace GuitarConfigurator.NetCore
{
    public static class Program
    {
        public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UseReactiveUI()
                .UsePlatformDetect()
                .LogToTrace();
    }
}

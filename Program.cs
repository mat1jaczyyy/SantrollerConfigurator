using System;
using Avalonia;
using Avalonia.ReactiveUI;
using GuitarConfiguratorSharp.Views;
using GuitarConfiguratorSharp.ViewModels;
using ReactiveUI;
using Splat;
using System.Reflection;

namespace GuitarConfiguratorSharp.NetCore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().Start<MainWindow>(
                () => new MainWindowViewModel()
            );
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            // Router uses Splat.Locator to resolve views for
            // view models, so we need to register our views.
            //
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetAssembly(typeof(MainWindowViewModel)));

            return AppBuilder
                .Configure<App>()
                .UseReactiveUI()
                .UsePlatformDetect()
                .LogToDebug();
        }
    }
}
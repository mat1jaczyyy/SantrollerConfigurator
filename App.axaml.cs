using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GuitarConfiguratorSharp.ViewModels;
using GuitarConfiguratorSharp.Views;
using ReactiveUI;
using Splat;
using System;

namespace GuitarConfiguratorSharp
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (!(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)) throw new System.Exception("Invalid ApplicationLifetime");
            Locator.CurrentMutable.RegisterConstant<IScreen>(new MainWindowViewModel());
            Locator.CurrentMutable.Register<IViewFor<ConfigViewModel>>(() => new ConfigView());
            Locator.CurrentMutable.Register<IViewFor<MainViewModel>>(() => new MainView());
            lifetime.MainWindow = new MainWindow { DataContext = Locator.Current.GetService<IScreen>() };
            lifetime.Exit += (_,_)=>{
                Environment.Exit(0);
            };
            base.OnFrameworkInitializationCompleted();
        }
    }
}
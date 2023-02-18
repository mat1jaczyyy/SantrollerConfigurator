using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GuitarConfigurator.NetCore.ViewModels;
using GuitarConfigurator.NetCore.Views;
using ReactiveUI;
using Splat;

namespace GuitarConfigurator.NetCore;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            throw new Exception("Invalid ApplicationLifetime");
        Locator.CurrentMutable.RegisterConstant<IScreen>(new MainWindowViewModel());
        Locator.CurrentMutable.Register<IViewFor<ConfigViewModel>>(() => new ConfigView());
        Locator.CurrentMutable.Register<IViewFor<MainViewModel>>(() => new MainView());
        lifetime.MainWindow = new MainWindow {DataContext = Locator.Current.GetService<IScreen>()};
        lifetime.Exit += (_, _) => { Environment.Exit(0); };
        base.OnFrameworkInitializationCompleted();
    }
}
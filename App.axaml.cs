using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GuitarConfiguratorSharp.ViewModels;
using GuitarConfiguratorSharp.Views;
using ReactiveUI;
using Splat;

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
            Locator.CurrentMutable.RegisterConstant<IScreen>(new MainWindowViewModel());
            Locator.CurrentMutable.Register<IViewFor<ConfigViewModel>>(() => new ConfigView());
            new MainWindow { DataContext = Locator.Current.GetService<IScreen>() }.Show();

            base.OnFrameworkInitializationCompleted();
        }
    }
}
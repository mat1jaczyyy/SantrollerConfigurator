using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class InitialConfigureView : ReactiveUserControl<InitialConfigViewModel>
{
    public InitialConfigureView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.WhenActivated(disposables => { disposables(ViewModel!.RegisterConnections()); });
        AvaloniaXamlLoader.Load(this);
    }
}
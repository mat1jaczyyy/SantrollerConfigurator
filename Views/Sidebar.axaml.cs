using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class SidebarView : ReactiveUserControl<ConfigViewModel>
{
    public SidebarView()
    {
        InitializeComponent();
    }
    public ConfigViewModel Model => ViewModel!;
    private void InitializeComponent()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Views
{
    public partial class ConfigView : ReactiveUserControl<ConfigViewModel>
    {        
        public ConfigView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }
    }
}
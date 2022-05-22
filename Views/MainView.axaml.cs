using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfiguratorSharp.ViewModels;
using ReactiveUI;
namespace GuitarConfiguratorSharp.Views
{
    public partial class MainView : ReactiveUserControl<MainViewModel>
    {
        public MainView()
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
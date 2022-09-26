using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfiguratorSharp.NetCore.Configuration;
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
            this.WhenActivated(disposables =>
            {
                disposables(ViewModel!.ShowDialog.RegisterHandler(DoShowDialogAsync));
                ViewModel!.Main.SelectedDevice!.LoadConfiguration(ViewModel);
            });
            AvaloniaXamlLoader.Load(this);
        }

        private async Task DoShowDialogAsync(InteractionContext<Output, AddInputWindowViewModel?> interaction)
        {
            AddInputWindowViewModel model = new AddInputWindowViewModel(interaction.Input);
            var dialog = new AddInputWindow
            {
                DataContext = model
            };
            
            var result = await dialog.ShowDialog<AddInputWindowViewModel?>((Window) this.VisualRoot!);
            interaction.SetOutput(result);
        }
    }
}
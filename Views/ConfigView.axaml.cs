using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Views
{
    public class ConfigView : ReactiveUserControl<ConfigViewModel>
    {
        public ConfigView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.WhenActivated(disposables =>
            {
                disposables(ViewModel!.ShowPinSelectDialog.RegisterHandler(DoShowDialogAsync));
                disposables(ViewModel!.ShowUnoShortDialog.RegisterHandler(DoShowUnoDialogAsync));
                disposables(Observable.StartAsync(() => ViewModel!.Main.SelectedDevice!.LoadConfiguration(ViewModel))
                    .Subscribe());
            });
            AvaloniaXamlLoader.Load(this);
        }

        private async Task DoShowUnoDialogAsync(InteractionContext<Arduino, ShowUnoShortWindowViewModel?> interaction)
        {
            ShowUnoShortWindowViewModel model = new ShowUnoShortWindowViewModel(interaction.Input);
            var dialog = new UnoShortWindow()
            {
                DataContext = model
            };

            var result = await dialog.ShowDialog<ShowUnoShortWindowViewModel?>((Window) VisualRoot!);
            interaction.SetOutput(result);
        }

        private async Task DoShowDialogAsync(InteractionContext<InputWithPin, SelectPinWindowViewModel?> interaction)
        {
            SelectPinWindowViewModel model = new SelectPinWindowViewModel(interaction.Input);
            var dialog = new SelectPinWindow
            {
                DataContext = model
            };

            var result = await dialog.ShowDialog<SelectPinWindowViewModel?>((Window) VisualRoot!);
            interaction.SetOutput(result);
        }
    }
}
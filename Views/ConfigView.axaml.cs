using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.Configuration;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

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
            disposables(ViewModel!.ShowIssueDialog.RegisterHandler(DoShowDialogAsync));
            disposables(ViewModel!.ShowUnoShortDialog.RegisterHandler(DoShowUnoDialogAsync));
            disposables(ViewModel!.ShowYesNoDialog.RegisterHandler(DoShowYesNoDialogAsync));
            disposables(ViewModel!.ShowBindAllDialog.RegisterHandler(DoShowBindAllDialog));
            ViewModel!.Main.SelectedDevice!.LoadConfiguration(ViewModel);
            disposables(
                ViewModel!.WhenAnyValue(x => x.Main.SelectedDevice!).OfType<Santroller>()
                    .ObserveOn(RxApp.MainThreadScheduler).Subscribe(s => s.StartTicking(ViewModel)));
        });
        AvaloniaXamlLoader.Load(this);
    }

    private async Task DoShowUnoDialogAsync(InteractionContext<Arduino, ShowUnoShortWindowViewModel?> interaction)
    {
        var model = new ShowUnoShortWindowViewModel(interaction.Input);
        var dialog = new UnoShortWindow
        {
            DataContext = model
        };

        var result = await dialog.ShowDialog<ShowUnoShortWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(result);
    }

    private async Task DoShowDialogAsync(
        InteractionContext<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?> interaction)
    {
        var model = new RaiseIssueWindowViewModel(interaction.Input);
        var dialog = new RaiseIssueWindow
        {
            DataContext = model
        };
        var result = await dialog.ShowDialog<RaiseIssueWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(result);
    }

    private async Task DoShowYesNoDialogAsync(
        InteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
    {
        var model = new AreYouSureWindowViewModel(interaction.Input.yesText, interaction.Input.noText,
            interaction.Input.text);
        var dialog = new AreYouSureWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<AreYouSureWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }

    private async Task DoShowBindAllDialog(
        InteractionContext<(ConfigViewModel model, Microcontroller microcontroller, Output output, DirectInput input),
            BindAllWindowViewModel> interaction)
    {
        var model = new BindAllWindowViewModel(interaction.Input.model, interaction.Input.microcontroller,
            interaction.Input.output, interaction.Input.input);
        var dialog = new BindAllWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<BindAllWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }
}
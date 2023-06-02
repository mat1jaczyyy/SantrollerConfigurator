using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Views;

public partial class ConfigView : ReactiveUserControl<ConfigViewModel>
{
    public ConfigView()
    {
        InitializeComponent();
    }

    public ConfigViewModel Model => ViewModel!;

    private void InitializeComponent()
    {
        this.WhenActivated(disposables =>
        {
            disposables(ViewModel!.ShowUnpluggedDialog.RegisterHandler(DoShowUnpluggedDialogAsync));
            disposables(ViewModel!.ShowIssueDialog.RegisterHandler(DoShowDialogAsync));
            disposables(ViewModel!.ShowYesNoDialog.RegisterHandler(DoShowYesNoDialogAsync));
            disposables(ViewModel!.ShowBindAllDialog.RegisterHandler(DoShowBindAllDialogAsync));
            disposables(ViewModel!.SaveConfig.RegisterHandler(DoSaveConfigAsync));
            disposables(ViewModel!.LoadConfig.RegisterHandler(DoLoadConfigAsync));
            disposables(ViewModel!.RegisterConnections());
            disposables(
                ViewModel!.WhenAnyValue(x => x.Device).OfType<Santroller>()
                    .ObserveOn(RxApp.MainThreadScheduler).Subscribe(s => s.StartTicking(ViewModel)));
            TopLevel.GetTopLevel(GetValue(VisualParentProperty))!.KeyDown += (sender, args) => ViewModel!.OnKeyEvent(args);
            TopLevel.GetTopLevel(GetValue(VisualParentProperty))!.PointerMoved += (sender, args) => ViewModel!.OnMouseEvent(args.GetCurrentPoint(GetValue(VisualParentProperty)).Position);
            TopLevel.GetTopLevel(GetValue(VisualParentProperty))!.PointerPressed += (sender, args) => ViewModel!.OnMouseEvent(args.GetCurrentPoint(GetValue(VisualParentProperty)).Properties.PointerUpdateKind);
            TopLevel.GetTopLevel(GetValue(VisualParentProperty))!.PointerWheelChanged += (sender, args) => ViewModel!.OnMouseEvent(args);
            if (ViewModel!.ShowUnoDialog && ViewModel!.Device is Arduino arduino) DoShowUnoDialog(arduino);
        });
        AvaloniaXamlLoader.Load(this);
    }

    private async void DoSaveConfigAsync(InteractionContext<ConfigViewModel, Unit> obj)
    {
        var extension = "." + obj.Input.Microcontroller.Board.ArdwiinoName + "config";
        var file = await ((Window) VisualRoot!).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            ShowOverwritePrompt = true, DefaultExtension = extension, SuggestedFileName = "controller" + extension,
            FileTypeChoices = new[] {new FilePickerFileType(extension) {Patterns = new[] {"*" + extension}}}
        });
        if (file == null) return;
        await using var stream = await file.OpenWriteAsync();
        Serializer.Serialize(stream, new SerializedConfiguration(obj.Input));
    }

    private async void DoLoadConfigAsync(InteractionContext<ConfigViewModel, Unit> obj)
    {
        var extension = "." + obj.Input.Microcontroller.Board.ArdwiinoName + "config";
        var file = await ((Window) VisualRoot!).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] {new FilePickerFileType(extension) {Patterns = new[] {"*" + extension}}}
        });
        if (!file.Any()) return;
        await using var stream = await file.First().OpenReadAsync();
        Serializer.Deserialize<SerializedConfiguration>(stream).LoadConfiguration(obj.Input);
    }

    private void DoShowUnoDialog(Arduino device)
    {
        var model = new ShowUnoShortWindowViewModel(device);
        var dialog = new UnoShortWindow
        {
            DataContext = model
        };

        dialog.ShowDialog<ShowUnoShortWindowViewModel?>((Window) VisualRoot!);
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

    private async Task DoShowUnpluggedDialogAsync(
        InteractionContext<(string yesText, string noText, string text), AreYouSureWindowViewModel> interaction)
    {
        var model = new AreYouSureWindowViewModel(interaction.Input.yesText, interaction.Input.noText,
            interaction.Input.text);
        var dialog = new UnpluggedWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<AreYouSureWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
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

    private async Task DoShowBindAllDialogAsync(
        InteractionContext<(ConfigViewModel model, Output output, DirectInput input),
            BindAllWindowViewModel> interaction)
    {
        var model = new BindAllWindowViewModel(interaction.Input.model,
            interaction.Input.output, interaction.Input.input);
        var dialog = new BindAllWindow
        {
            DataContext = model
        };
        await dialog.ShowDialog<BindAllWindowViewModel?>((Window) VisualRoot!);
        interaction.SetOutput(model);
    }
}
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public abstract class InputWithPin : Input
{
    protected InputWithPin(ConfigViewModel model, DirectPinConfig pinConfig) :
        base(model)
    {
        PinConfig = pinConfig;
        DetectPinCommand =
            ReactiveCommand.CreateFromTask(DetectPinAsync, this.WhenAnyValue(s => s.Model.Main.Working).Select(s => !s));
        this.WhenAnyValue(x => x.PinConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Pin)));
    }


    public DirectPinConfig PinConfig { get; }

    public List<int> AvailablePins => Model.Microcontroller.GetAllPins(IsAnalog);

    public int Pin
    {
        get => PinConfig.Pin;
        set
        {
            PinConfig.Pin = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public DevicePinMode PinMode
    {
        get => PinConfig.PinMode;
        set
        {
            PinConfig.PinMode = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(PinConfigs));
        }
    }

    public override IList<PinConfig> PinConfigs => new List<PinConfig> {PinConfig};

    public string PinConfigText { get; private set; } = "Find Pin";

    protected abstract string DetectionText { get; }
    public ICommand DetectPinCommand { get; }

    private async Task DetectPinAsync()
    {
        if (Model.Device is Santroller santroller)
        {
            PinConfigText = DetectionText;
            this.RaisePropertyChanged(nameof(PinConfigText));
            Pin = await santroller.DetectPinAsync(IsAnalog, Pin, Model.Microcontroller);
            PinConfigText = "Find Pin";
            this.RaisePropertyChanged(nameof(PinConfigText));
        }
    }
}
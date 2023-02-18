using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration;

public abstract class InputWithPin : Input
{
    protected InputWithPin(ConfigViewModel model, Microcontroller microcontroller, DirectPinConfig pinConfig) :
        base(model)
    {
        Microcontroller = microcontroller;
        PinConfig = pinConfig;
        Microcontroller.AssignPin(PinConfig);
        DetectPinCommand =
            ReactiveCommand.CreateFromTask(DetectPin, this.WhenAnyValue(s => s.Model.Main.Working).Select(s => !s));
        this.WhenAnyValue(x => x.PinConfig.Pin).Subscribe(_ => this.RaisePropertyChanged(nameof(Pin)));
    }

    protected Microcontroller Microcontroller { get; }

    public DirectPinConfig PinConfig { get; }

    public List<int> AvailablePins => Microcontroller.GetAllPins(IsAnalog);

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

    public override void Dispose()
    {
        Microcontroller.UnAssignPins(PinConfig.Type);
    }

    private async Task DetectPin()
    {
        if (Model.Main.SelectedDevice is Santroller santroller)
        {
            PinConfigText = DetectionText;
            this.RaisePropertyChanged(nameof(PinConfigText));
            Pin = await santroller.DetectPin(IsAnalog, Pin, Microcontroller);
            PinConfigText = "Find Pin";
            this.RaisePropertyChanged(nameof(PinConfigText));
        }
    }
}
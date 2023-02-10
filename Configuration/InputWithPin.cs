using System;
using System.Collections.Generic;
using System.Linq;
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
        _pinConfig = pinConfig;
        Microcontroller.AssignPin(_pinConfig);
        DetectPinCommand =
            ReactiveCommand.CreateFromTask(DetectPin, this.WhenAnyValue(s => s.Model.Main.Working).Select(s => !s));
    }

    protected Microcontroller Microcontroller { get; }

    private DirectPinConfig _pinConfig;

    public DirectPinConfig PinConfig => _pinConfig;

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

    public override void Dispose()
    {
        Microcontroller.UnAssignPins(PinConfig.Type);
    }

    public override IList<PinConfig> PinConfigs => new List<PinConfig>() {_pinConfig};

    public string PinConfigText { get; private set; } = "Find Pin";

    protected abstract string DetectionText { get; }
    public ICommand DetectPinCommand { get; }

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
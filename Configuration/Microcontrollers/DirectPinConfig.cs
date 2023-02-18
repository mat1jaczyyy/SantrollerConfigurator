using System.Collections.Generic;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class DirectPinConfig : PinConfig
{
    private int _pin;
    private DevicePinMode _pinMode;

    public DirectPinConfig(ConfigViewModel model, string type, int pin, DevicePinMode pinMode) : base(model)
    {
        Type = type;
        PinMode = pinMode;
        Pin = pin;
    }

    public override string Type { get; }
    public override string Definition => "";

    public DevicePinMode PinMode
    {
        get => _pinMode;
        set => this.RaiseAndSetIfChanged(ref _pinMode, value);
    }

    public int Pin
    {
        get => _pin;
        set
        {
            this.RaiseAndSetIfChanged(ref _pin, value);
            Update();
        }
    }

    public override IEnumerable<int> Pins => new List<int> {Pin};


    public override string Generate()
    {
        return "";
    }
}
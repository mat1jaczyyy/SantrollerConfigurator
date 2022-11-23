using System.Collections.Generic;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class DirectPinConfig : PinConfig
{
    public override string Type { get; }
    public override string Definition => "";
    private DevicePinMode _pinMode;
    public DevicePinMode PinMode
    {
        get => _pinMode;
        set => this.RaiseAndSetIfChanged(ref _pinMode, value);
    }
    private int _pin;

    public int Pin
    {
        get => _pin;
        set => this.RaiseAndSetIfChanged(ref _pin, value);
    }


    public override string Generate()
    {
        return "";
    }

    public DirectPinConfig(string type, int pin, DevicePinMode pinMode)
    {
        Type = type;
        PinMode = pinMode;
        Pin = pin;
    }

    public override IEnumerable<int> Pins => new List<int> {Pin};
}
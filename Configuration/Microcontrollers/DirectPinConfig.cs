using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class DirectPinConfig : PinConfig
{
    public override string Type { get; }
    public override string Definition => "";
    public DevicePinMode PinMode { get; }
    public int Pin { get; }

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
}
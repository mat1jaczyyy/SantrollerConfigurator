using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class InputWithPin: Input
{
    public abstract DevicePinMode PinMode { get; }

    protected abstract Microcontroller Microcontroller { get; }
    public abstract int Pin { get; }
}
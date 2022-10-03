using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class InputWithPin: Input
{
    public abstract DevicePinMode PinMode { get; }

    protected abstract Microcontroller.Microcontroller Microcontroller { get; }
    public abstract int Pin { get; }
}
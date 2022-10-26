using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public abstract class PinConfig : ReactiveObject
{
    public abstract string Type { get; }
    public abstract string Definition { get; }
    public abstract string Generate();
}
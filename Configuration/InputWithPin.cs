using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class InputWithPin : Input
{
    protected abstract Microcontroller Microcontroller { get; }
    
    public abstract DirectPinConfig PinConfig { get; set; }
    
    public int Pin
    {
        get => PinConfig.Pin;
        set => PinConfig = new DirectPinConfig(PinConfig.Type, value, PinConfig.PinMode);
    }
    
    public DevicePinMode PinMode
    {
        get => PinConfig.PinMode;
        set => PinConfig = new DirectPinConfig(PinConfig.Type, PinConfig.Pin, value);
    }
    
    public override void Dispose()
    {
        Microcontroller.UnAssignPins(PinConfig.Type);
    }
}
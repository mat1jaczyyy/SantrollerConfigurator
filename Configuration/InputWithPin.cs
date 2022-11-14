using System.Collections.Generic;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class InputWithPin : Input
{
    protected InputWithPin(Microcontroller microcontroller, DirectPinConfig pinConfig)
    {
        Microcontroller = microcontroller;
        _pinConfig = pinConfig;
        Microcontroller.AssignPin(_pinConfig);
        Microcontroller.PinConfigs.CollectionChanged +=
            (_, _) => this.RaisePropertyChanged(nameof(AvailablePins));
    }

    protected Microcontroller Microcontroller { get; }

    private DirectPinConfig _pinConfig;
    public DirectPinConfig PinConfig
    {
        get => _pinConfig;
        set
        {
            _pinConfig = value;
            Microcontroller.AssignPin(_pinConfig);
        }
    }

    public List<int> AvailablePins => Microcontroller.GetFreePins();

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
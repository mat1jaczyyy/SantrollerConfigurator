using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

namespace GuitarConfiguratorSharp.NetCore.Configuration;
public class DirectInput : InputWithPin
{
    public DirectInput(int pin, DevicePinMode pinMode, Microcontroller microcontroller)
    {
        _pinConfig = new DirectPinConfig(Guid.NewGuid().ToString(), pin, pinMode);
        _microcontroller = microcontroller;
    }
     

    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    private IEnumerable<DevicePinMode> GetPinModes()
    {
        var modes = Enum.GetValues(typeof(DevicePinMode)).Cast<DevicePinMode>()
            .Where(mode => mode is not (DevicePinMode.Output or DevicePinMode.Analog));
        if (_microcontroller.Board.IsAvr())
        {
            return modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown));
        }

        return modes;
    }

    private DirectPinConfig _pinConfig;
    public override DirectPinConfig PinConfig
    {
        get => _pinConfig;
        set
        {
            _pinConfig = value;
            Microcontroller.AssignPin(_pinConfig);
        }
    }

    protected override Microcontroller Microcontroller => _microcontroller;

    private Microcontroller _microcontroller;

    public override SerializedInput GetJson()
    {
        return new SerializedDirectInput(PinConfig.Pin, PinConfig.PinMode);
    }

    public override bool IsAnalog => PinConfig.PinMode == DevicePinMode.Analog;
    public override bool IsUint => true;

    public override string Generate()
    {
        return IsAnalog
            ? _microcontroller.GenerateAnalogRead(PinConfig.Pin)
            : _microcontroller.GenerateDigitalRead(PinConfig.Pin, PinConfig.PinMode is DevicePinMode.PullUp);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings)
    {
        return string.Join(";\n", bindings.Select(binding => binding.Item2));
    }

    public override void Dispose()
    {  
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_DIRECT"};
    }
    
    public override List<DevicePin> Pins => new()
    {
        new (PinConfig.Pin, PinConfig.PinMode)
    };
}
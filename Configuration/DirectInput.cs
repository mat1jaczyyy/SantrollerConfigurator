using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore.Configuration;
public class DirectInput : InputWithPin
{
    public DirectInput(int pin, DevicePinMode pinMode, Microcontroller microcontroller): base(microcontroller, new DirectPinConfig(Guid.NewGuid().ToString(), pin, pinMode))
    {
    }
     

    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    private IEnumerable<DevicePinMode> GetPinModes()
    {
        var modes = Enum.GetValues(typeof(DevicePinMode)).Cast<DevicePinMode>()
            .Where(mode => mode is not (DevicePinMode.Output or DevicePinMode.Analog));
        if (Microcontroller.Board.IsAvr())
        {
            return modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown));
        }

        return modes;
    }

   
    public override SerializedInput GetJson()
    {
        return new SerializedDirectInput(PinConfig.Pin, PinConfig.PinMode);
    }

    public override bool IsAnalog => PinConfig.PinMode == DevicePinMode.Analog;
    public override bool IsUint => true;

    public override string Generate()
    {
        return IsAnalog
            ? Microcontroller.GenerateAnalogRead(PinConfig.Pin)
            : Microcontroller.GenerateDigitalRead(PinConfig.Pin, PinConfig.PinMode is DevicePinMode.PullUp);
    }

    public override InputType? InputType => IsAnalog? Types.InputType.AnalogPinInput : Types.InputType.DigitalPinInput;

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
using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public class DirectInput : InputWithPin
{
    public DirectInput(int pin, DevicePinMode pinMode, Microcontroller microcontroller) : base(microcontroller,
        new DirectPinConfig(Guid.NewGuid().ToString(), pin, pinMode))
    {
    }


    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    private IEnumerable<DevicePinMode> GetPinModes()
    {
        var modes = Enum.GetValues(typeof(DevicePinMode)).Cast<DevicePinMode>()
            .Where(mode => mode is not (DevicePinMode.Output or DevicePinMode.Analog));
        return Microcontroller.Board.IsAvr()
            ? modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown))
            : modes;
    }


    public override SerializedInput GetJson()
    {
        return new SerializedDirectInput(PinConfig.Pin, PinConfig.PinMode);
    }

    public override bool IsAnalog => PinConfig.PinMode == DevicePinMode.Analog;

    public bool IsUintDirect = true;
    public override bool IsUint => IsUintDirect;

    private string GenerateAnalogRead(int index)
    {
        var ret = Microcontroller.GenerateAnalogRead(index);
        // We are treating the uint from the sensor as an analog value, so we need to convert it here
        if (!IsUintDirect)
        {
            ret += $" - {short.MaxValue}";
        }

        return ret;
    }
    public override string Generate()
    {
        return IsAnalog
            ? GenerateAnalogRead(Microcontroller.GetChannel(PinConfig.Pin))
            : Microcontroller.GenerateDigitalRead(PinConfig.Pin, PinConfig.PinMode is DevicePinMode.PullUp);
    }

    public override InputType? InputType => IsAnalog ? Types.InputType.AnalogPinInput : Types.InputType.DigitalPinInput;

    public override string GenerateAll(List<Tuple<Input, string>> bindings)
    {
        if (Microcontroller is not AvrController) return string.Join(";\n", bindings.Select(binding => binding.Item2));
        Dictionary<string, string> fillPins = new();

        var binding = bindings.Where(s => s.Item1 is DirectInput {IsAnalog: true})
            .Select(s => ((s.Item1 as DirectInput)!.Pin, s.Item2)).OrderBy(s => s.Pin)
            .DistinctBy(s => s.Pin).ToList();
        for (var i = 0; i < binding.Count; i++)
        {
            var tuple = binding[i];
            fillPins[tuple.Item2] = GenerateAnalogRead(i);
        }
        return string.Join(";\n", bindings.Select(b => b.Item1.IsAnalog ? fillPins[b.Item2] : b.Item2));
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
        new(PinConfig.Pin, PinConfig.PinMode)
    };

    public void Update(Dictionary<int, int> analogRaw, Dictionary<int, bool> digitalRaw)
    {
        if (IsAnalog)
        {
            var raw = analogRaw.GetValueOrDefault(Pin, 0);
            if (!IsUintDirect)
            {
                raw -= short.MaxValue;
            }

            RawValue = raw;
        }
        else
        {
            RawValue = digitalRaw.GetValueOrDefault(Pin, true) ? 0 : 1;
        }
    }
}
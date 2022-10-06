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
        Pin = pin;
        PinMode = pinMode;
        _microcontroller = microcontroller;
    }

    public IEnumerable<DevicePinMode> DevicePinModes => GetPinModes();

    private IEnumerable<DevicePinMode> GetPinModes()
    {
        var modes = Enum.GetValues(typeof(DevicePinMode)).Cast<DevicePinMode>()
            .Where(mode => mode is not (DevicePinMode.Output or DevicePinMode.Analog));
        if (_microcontroller.Board.IsAVR())
        {
            return modes.Where(mode => mode is not (DevicePinMode.BusKeep or DevicePinMode.PullDown));
        }

        return modes;
    }

    public override DevicePinMode PinMode { get; }

    protected override Microcontroller Microcontroller => _microcontroller;

    private Microcontroller _microcontroller;
    public override int Pin { get; }

    public override SerializedInput GetJson()
    {
        return new SerializedDirectInput(Pin, PinMode);
    }

    public override bool IsAnalog => PinMode == DevicePinMode.Analog;

    public override string Generate()
    {
        return IsAnalog
            ? _microcontroller.GenerateAnalogRead(Pin)
            : _microcontroller.GenerateDigitalRead(Pin, PinMode is DevicePinMode.PullUp);
    }

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller controller)
    {
        return String.Join("\n", bindings.Select(binding => binding.Item2));
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_DIRECT"};
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public class DirectInput : IInput
{
    public DirectInput(int pin, DevicePinMode pinMode, Microcontroller.Microcontroller microcontroller)
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

    public DevicePinMode PinMode { get; }

    private Microcontroller.Microcontroller _microcontroller;
    public int Pin { get; }

    public bool IsAnalog => PinMode == DevicePinMode.Analog;

    public string Generate(bool xbox, Microcontroller.Microcontroller controller)
    {
        return IsAnalog
            ? controller.GenerateAnalogRead(Pin)
            : controller.GenerateDigitalRead(Pin, PinMode is DevicePinMode.PullUp);
    }

    public bool RequiresSpi()
    {
        return false;
    }

    public bool RequiresI2C()
    {
        return false;
    }

    public string GenerateAll(bool xbox, List<Tuple<IInput, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        return String.Join("\n", bindings.Select(binding => binding.Item2));
    }

    public IReadOnlyList<string> RequiredDefines()
    {
        return new[] {"INPUT_DIRECT"};
    }
}
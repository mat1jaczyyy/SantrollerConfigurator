using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public class DirectInput : IInput
{
    public DirectInput(int pin, DevicePinMode pinMode)
    {
        Pin = pin;
        PinMode = pinMode;
    }

    public DevicePinMode PinMode { get; }
    public int Pin { get; }

    public string Generate(bool xbox, Microcontroller.Microcontroller controller)
    {
        return IsAnalog() ? controller.GenerateAnalogRead(Pin) : controller.GenerateDigitalRead(Pin, PinMode is DevicePinMode.PullUp);
    }

    public bool IsAnalog()
    {
        return PinMode == DevicePinMode.Analog;
    }

    public bool RequiresSpi()
    {
        return true;
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
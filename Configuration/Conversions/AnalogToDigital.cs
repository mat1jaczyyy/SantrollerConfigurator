using System;
using System.Collections.Generic;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Conversions;

public class AnalogToDigital : IInput
{
    public IInput Child { get; }
    public AnalogToDigitalType AnalogToDigitalType { get; set; }
    public int Threshold { get; set; }

    public AnalogToDigital(IInput child, AnalogToDigitalType analogToDigitalType, int threshold)
    {
        Child = child;
        AnalogToDigitalType = analogToDigitalType;
        Threshold = threshold;
    }


    public string Generate(bool xbox, Microcontroller.Microcontroller controller)
    {
        switch (AnalogToDigitalType)
        {
            case AnalogToDigitalType.Trigger:
            case AnalogToDigitalType.JoyHigh:
                return $"{Child.Generate(xbox, controller)} > {Threshold}";
            case AnalogToDigitalType.JoyLow:
                return $"{Child.Generate(xbox, controller)} < {-Threshold}";
        }

        return "";
    }
    IInput IInput.InnermostInput()
    {
        return Child;
    }
    public bool IsAnalog()
    {
        return Child.IsAnalog();
    }

    public bool RequiresSpi()
    {
        return Child.RequiresSpi();
    }

    public bool RequiresI2C()
    {
        return Child.RequiresI2C();
    }

    public string GenerateAll(bool xbox, List<Tuple<IInput, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        throw new InvalidOperationException("Never call GenerateAll on AnalogToDigital, call it on its children");
    }

    public IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}
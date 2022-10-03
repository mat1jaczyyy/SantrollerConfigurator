using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Conversions;

public class AnalogToDigital : Input
{
    public Input Child { get; }
    public AnalogToDigitalType AnalogToDigitalType { get; set; }
    public int Threshold { get; set; }
    public IEnumerable<AnalogToDigitalType> AnalogToDigitalTypes =>
        Enum.GetValues(typeof(AnalogToDigitalType)).Cast<AnalogToDigitalType>();

    public AnalogToDigital(Input child, AnalogToDigitalType analogToDigitalType, int threshold)
    {
        Child = child;
        AnalogToDigitalType = analogToDigitalType;
        Threshold = threshold;
    }


    public override string Generate()
    {
        switch (AnalogToDigitalType)
        {
            case AnalogToDigitalType.Trigger:
            case AnalogToDigitalType.JoyHigh:
                return $"{Child.Generate()} > {Threshold}";
            case AnalogToDigitalType.JoyLow:
                return $"{Child.Generate()} < {-Threshold}";
        }

        return "";
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override bool IsAnalog => Child.IsAnalog;

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        throw new InvalidOperationException("Never call GenerateAll on AnalogToDigital, call it on its children");
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}
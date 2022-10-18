using System;
using System.Collections.Generic;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Conversions;

public class AnalogToDigital : Input
{
    public Input Child { get; }
    public AnalogToDigitalType AnalogToDigitalType { get; set; }
    public int Threshold { get; set; }
    public IEnumerable<AnalogToDigitalType> AnalogToDigitalTypes => Enum.GetValues<AnalogToDigitalType>();

    public AnalogToDigital(Input child, AnalogToDigitalType analogToDigitalType, int threshold)
    {
        Child = child;
        AnalogToDigitalType = analogToDigitalType;
        Threshold = threshold;
    }


    public override string Generate()
    {
        if (Child.IsUint)
        {
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate()}) > {short.MaxValue + Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate()}) < {short.MaxValue - Threshold}";
            }
        }
        else
        {

            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate()}) > {Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate()}) < {-Threshold}";
            }
        }

        return "";
    }

    public override SerializedInput GetJson()
    {
        return new SerializedAnalogToDigital(Child.GetJson(), AnalogToDigitalType, Threshold);
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override List<DevicePin> Pins => Child.Pins;

    public override bool IsAnalog => Child.IsAnalog;
    public override bool IsUint => Child.IsUint;

    public override string GenerateAll(List<Tuple<Input, string>> bindings)
    {
        throw new InvalidOperationException("Never call GenerateAll on AnalogToDigital, call it on its children");
    }

    public override void Dispose()
    {
        Child.Dispose();
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}
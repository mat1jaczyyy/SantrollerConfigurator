using System;
using System.Collections.Generic;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
public class DigitalToAnalog : Input
{
    public Input Child { get; }
    public int Value { get; set; }

    public DigitalToAnalog(Input child, int value)
    {
        Child = child;
        Value = value;
    }

    public override string Generate()
    {
        return $"({Child.Generate()}) * {Value}";
    }

    public override SerializedInput GetJson()
    {
        return new SerializedDigitalToAnalog(Child.GetJson(), Value);
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
        throw new InvalidOperationException("Never call GenerateAll on DigitalToAnalog, call it on its children");
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }

    public override void Dispose()
    {
        Child.Dispose();
    }
}
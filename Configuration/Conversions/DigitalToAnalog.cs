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
        return $"{Child.Generate()} * {Value}";
    }

    public override SerializedInput GetJson()
    {
        return new SerializedDigitalToAnalog(Child.GetJson(), Value);
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override bool IsAnalog => Child.IsAnalog;

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller controller)
    {
        throw new InvalidOperationException("Never call GenerateAll on DigitalToAnalog, call it on its children");
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}
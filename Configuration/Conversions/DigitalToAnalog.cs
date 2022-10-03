using System;
using System.Collections.Generic;

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

    public override Input InnermostInput()
    {
        return Child;
    }

    public override bool IsAnalog => Child.IsAnalog;

    public override string GenerateAll(bool xbox, List<Tuple<Input, string>> bindings,
        Microcontroller.Microcontroller controller)
    {
        throw new InvalidOperationException("Never call GenerateAll on DigitalToAnalog, call it on its children");
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}
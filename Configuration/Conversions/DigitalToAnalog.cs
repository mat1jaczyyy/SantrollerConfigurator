using System;
using System.Collections.Generic;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Conversions;

public class DigitalToAnalog : IInput
{
    public IInput Child { get; }
    public int Value { get; set; }

    public DigitalToAnalog(IInput child, int value)
    {
        Child = child;
        Value = value;
    }

    public string Generate(bool xbox, Microcontroller.Microcontroller controller)
    {
        return $"{Child.Generate(xbox, controller)} * {Value}";
    }

    IInput IInput.InnermostInput()
    {
        return Child;
    }

    public bool IsAnalog => Child.IsAnalog;

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
        throw new InvalidOperationException("Never call GenerateAll on DigitalToAnalog, call it on its children");
    }

    public IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}
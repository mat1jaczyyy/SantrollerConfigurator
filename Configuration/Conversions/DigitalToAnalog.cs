using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
public class DigitalToAnalog : Input
{
    public Input Child { get; }
    public int Value { get; set; }

    public DigitalToAnalog(Input child, int value)
    {
        Child = child;
        Value = value;
        this.WhenAnyValue(x => x.Child.RawValue).Subscribe(s => RawValue = s * Value);
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
    public override InputType? InputType => Child.InputType;

    public override bool IsAnalog => Child.IsAnalog;
    public override bool IsUint => Child.IsUint;

    public override void Update(Dictionary<int, int> analogRaw, Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        Child.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw, ps2ControllerType, wiiControllerType);
    }

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
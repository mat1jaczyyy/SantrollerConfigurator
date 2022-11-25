using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Collections;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
public class DigitalToAnalog : Input
{
    public Input Child { get; }
    public int Value { get; set; }

    public DigitalToAnalog(Input child, int value, ConfigViewModel model) : base(model)
    {
        Child = child;
        Value = value;
        this.WhenAnyValue(x => x.Child.RawValue).Subscribe(s => RawValue = s * Value);
    }

    public override string Generate()
    {
        return $"({Child.Generate()}) * {Value}";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedDigitalToAnalog(Child.Serialise(), Value);
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override List<DevicePin> Pins => Child.Pins;
    public override List<PinConfig> PinConfigs => Child.PinConfigs;
    public override InputType? InputType => Child.InputType;

    public override bool IsAnalog => Child.IsAnalog;
    public override bool IsUint => Child.IsUint;

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        Child.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw, ps2ControllerType, wiiControllerType);
    }

    public override string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings)
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
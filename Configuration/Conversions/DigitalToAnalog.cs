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
    public int On { get; set; }
    public int Off { get; set; }

    private readonly ObservableAsPropertyHelper<int> _minimum;
    public int Minimum => _minimum.Value;

    private readonly ObservableAsPropertyHelper<int> _maximum;
    public int Maximum => _maximum.Value;

    public DigitalToAnalog(Input child, int on, int off, ConfigViewModel model) : base(model)
    {
        Child = child;
        On = on;
        Off = off;
        this.WhenAnyValue(x => x.Child.RawValue).Subscribe(s => RawValue = s > 0 ? On : Off);
        _minimum = this.WhenAnyValue(x => x.Child.IsUint).Select(s => s ? (int) ushort.MinValue : short.MinValue)
            .ToProperty(this, x => x.Minimum);
        _maximum = this.WhenAnyValue(x => x.Child.IsUint).Select(s => s ? (int) ushort.MaxValue : short.MaxValue)
            .ToProperty(this, x => x.Maximum);
    }

    public override string Generate(bool xbox)
    {
        if (xbox)
        {
            return $"({Child.Generate(xbox)})?{On}:{Off}";
        }

        if (IsUint)
        {
            return $"({Child.Generate(xbox)})?{On >> 8}:{Off >> 8}";
        }

        return $"({Child.Generate(xbox)})?{(On >> 8) + 128}:{(Off >> 8) + 128}";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedDigitalToAnalog(Child.Serialise(), On, Off);
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override IList<DevicePin> Pins => Child.Pins;
    public override IList<PinConfig> PinConfigs => Child.PinConfigs;
    public override InputType? InputType => Child.InputType;

    public override bool IsAnalog => Child.IsAnalog;
    public override bool IsUint => Child.IsUint;

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        Child.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType);
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
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Conversions;

public class DigitalToAnalog : Input
{
    private readonly ObservableAsPropertyHelper<int> _maximum;

    private readonly ObservableAsPropertyHelper<int> _minimum;

    public DigitalToAnalog(Input child, int on, ConfigViewModel model) : base(model)
    {
        Child = child;
        On = on;
        this.WhenAnyValue(x => x.Child.RawValue).Subscribe(s => RawValue = s > 0 ? On : 0);
        _minimum = this.WhenAnyValue(x => x.Child.IsUint).Select(s => s ? (int) ushort.MinValue : short.MinValue)
            .ToProperty(this, x => x.Minimum);
        _maximum = this.WhenAnyValue(x => x.Child.IsUint).Select(s => s ? (int) ushort.MaxValue : short.MaxValue)
            .ToProperty(this, x => x.Maximum);
        IsAnalog = Child.IsAnalog;
    }

    public Input Child { get; }
    public int On { get; set; }
    public int Minimum => _minimum.Value;
    public int Maximum => _maximum.Value;

    public override IList<DevicePin> Pins => Child.Pins;
    public override IList<PinConfig> PinConfigs => Child.PinConfigs;
    public override InputType? InputType => Child.InputType;
    public override bool IsUint => Child.IsUint;

    public override string Generate(ConfigField mode)
    {
        var gen = Child.Generate(mode);
        return mode == ConfigField.Xbox360 ? $"({gen})?{On}:{{output}}" : $"({gen})?{(On >> 8) + 128}:{{output}}";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedDigitalToAnalog(Child.Serialise(), On);
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        Child.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType);
    }

    public override string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings,
        ConfigField mode)
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

    public override string GetImagePath()
    {
        return Child.GetImagePath();
    }
}
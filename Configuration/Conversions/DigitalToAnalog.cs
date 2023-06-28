using System;
using System.Collections.Generic;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Conversions;

public class DigitalToAnalog : Input
{
    private readonly bool _trigger;

    public DigitalToAnalog(Input child, bool inverted, int on, bool trigger, ConfigViewModel model) : base(model)
    {
        _trigger = trigger;
        Child = child;
        On = on;
        Tilt = false;
        Inverted = inverted;
        this.WhenAnyValue(x => x.Child.RawValue, x => x.Inverted)
            .Subscribe(s => RawValue = s.Item2 ? (s.Item1 > 0 ? 0 : On) : s.Item1 > 0 ? On : 0);
        if (trigger)
        {
            Minimum = ushort.MinValue;
            Maximum = ushort.MaxValue;
        }
        else
        {
            Minimum = short.MinValue;
            Maximum = short.MaxValue;
        }

        IsAnalog = Child.IsAnalog;
    }

    public DigitalToAnalog(Input child, bool inverted, ConfigViewModel model) : base(model)
    {
        _trigger = false;
        Child = child;
        On = 32767;
        Tilt = true;
        Inverted = inverted;
        this.WhenAnyValue(x => x.Child.RawValue, x => x.Inverted)
            .Subscribe(s => RawValue = s.Item2 ? (s.Item1 > 0 ? 0 : On) : s.Item1 > 0 ? On : 0);

        Minimum = short.MinValue;
        Maximum = short.MaxValue;
        IsAnalog = Child.IsAnalog;
    }

    public Input Child { get; }
    public int On { get; set; }
    public bool Tilt { get; }
    [Reactive] 
    public bool Inverted { get; set; }
    public int Minimum { get; }
    public int Maximum { get; }

    public override IList<DevicePin> Pins => Child.Pins;
    public override IList<PinConfig> PinConfigs => Child.PinConfigs;
    public override InputType? InputType => Child.InputType;
    public override bool IsUint => _trigger;

    public override string Title => Child.Title;

    public override string Generate()
    {
        return Child.Generate();
    }

    public override SerializedInput Serialise()
    {
        return new SerializedDigitalToAnalog(Child.Serialise(), On, Inverted, _trigger, Tilt);
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType,
        byte[] usbHostInputsRaw, byte[] usbHostRaw)
    {
        Child.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType, usbHostInputsRaw, usbHostRaw);
    }

    public override string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode)
    {
        throw new InvalidOperationException("Never call GenerateAll on DigitalToAnalog, call it on its children");
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}
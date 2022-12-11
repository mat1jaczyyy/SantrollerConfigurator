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

public class AnalogToDigital : Input
{
    public Input Child { get; }
    public AnalogToDigitalType AnalogToDigitalType { get; set; }
    public int Threshold { get; set; }
    public override InputType? InputType => Child.InputType;
    public IEnumerable<AnalogToDigitalType> AnalogToDigitalTypes => Enum.GetValues<AnalogToDigitalType>();

    public AnalogToDigital(Input child, AnalogToDigitalType analogToDigitalType, int threshold,
        ConfigViewModel model) : base(model)
    {
        Child = child;
        AnalogToDigitalType = analogToDigitalType;
        Threshold = threshold;
        this.WhenAnyValue(x => x.Child.RawValue).ObserveOn(RxApp.MainThreadScheduler).Subscribe(s => RawValue = Calculate(s));
    }


    public override string Generate(bool xbox)
    {
        if (Child.IsUint)
        {
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate(xbox)}) > {short.MaxValue + Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate(xbox)}) < {short.MaxValue - Threshold}";
            }
        }
        else
        {

            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate(xbox)}) > {Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate(xbox)}) < {-Threshold}";
            }
        }

        return "";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedAnalogToDigital(Child.Serialise(), AnalogToDigitalType, Threshold);
    }


    private int Calculate(int val)
    {
        if (Child.IsUint)
        {
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return val > short.MaxValue + Threshold ? 1 : 0;
                case AnalogToDigitalType.JoyLow:
                    return val < short.MaxValue - Threshold ? 1 : 0;
            }
        }
        else
        {

            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return val > Threshold ? 1 : 0;
                case AnalogToDigitalType.JoyLow:
                    return val < -Threshold ? 1 : 0;
            }
        }

        return 0;
    }

    public override Input InnermostInput()
    {
        return Child;
    }

    public override IList<DevicePin> Pins => Child.Pins;
    public override IList<PinConfig> PinConfigs => Child.PinConfigs;

    public override bool IsAnalog => Child.IsAnalog;
    public override bool IsUint => Child.IsUint;

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        Child.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw, ps2ControllerType, wiiControllerType);
    }

    public override string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings, bool shared,
        bool xbox)
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
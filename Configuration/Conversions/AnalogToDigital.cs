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
        IsAnalog = Child.IsAnalog;
        this.WhenAnyValue(x => x.Child.RawValue).ObserveOn(RxApp.MainThreadScheduler).Subscribe(s => RawValue = Calculate(s));
    }


    public override string Generate(DeviceEmulationMode mode)
    {
        if (Child.IsUint)
        {
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate(mode)}) > {short.MaxValue + Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate(mode)}) < {short.MaxValue - Threshold}";
            }
        }
        else
        {

            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate(mode)}) > {Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate(mode)}) < {-Threshold}";
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
    public override bool IsUint => Child.IsUint;

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        Child.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw, ps2ControllerType, wiiControllerType);
    }

    public override string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings, 
        DeviceEmulationMode mode)
    {
        throw new InvalidOperationException("Never call GenerateAll on AnalogToDigital, call it on its children");
    }

    public override void Dispose()
    {
        Child.Dispose();
    }

    public override string GetImagePath()
    {
        return Child.GetImagePath();
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child.RequiredDefines();
    }
}
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Conversions;

public class AnalogToDigital : Input
{
    public Input Child { get; }
    public AnalogToDigitalType AnalogToDigitalType { get; set; }
    public int Threshold { get; set; }
    public override InputType? InputType => Child.InputType;
    public IEnumerable<AnalogToDigitalType> AnalogToDigitalTypes => Enum.GetValues<AnalogToDigitalType>();

    public AnalogToDigital(Input child, AnalogToDigitalType analogToDigitalType, int threshold)
    {
        Child = child;
        AnalogToDigitalType = analogToDigitalType;
        Threshold = threshold;
        this.WhenAnyValue(x => x.Child.RawValue).ObserveOn(RxApp.MainThreadScheduler).Subscribe(s => RawValue = Calculate(s));
    }


    public override string Generate()
    {
        if (Child.IsUint)
        {
            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate()}) > {short.MaxValue + Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate()}) < {short.MaxValue - Threshold}";
            }
        }
        else
        {

            switch (AnalogToDigitalType)
            {
                case AnalogToDigitalType.Trigger:
                case AnalogToDigitalType.JoyHigh:
                    return $"({Child.Generate()}) > {Threshold}";
                case AnalogToDigitalType.JoyLow:
                    return $"({Child.Generate()}) < {-Threshold}";
            }
        }

        return "";
    }

    public override SerializedInput GetJson()
    {
        return new SerializedAnalogToDigital(Child.GetJson(), AnalogToDigitalType, Threshold);
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

    public override List<DevicePin> Pins => Child.Pins;

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
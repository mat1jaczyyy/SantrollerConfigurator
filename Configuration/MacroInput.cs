using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration;

public class MacroInput : Input
{
    public Input Child1 { get; }
    public Input Child2 { get; }
    public override InputType? InputType => Types.InputType.MacroInput;

    public MacroInput(Input child1, Input child2,
        ConfigViewModel model) : base(model)
    {
        Child1 = child1;
        Child2 = child2;
        this.WhenAnyValue(x => x.Child1, x => x.Child2)
            .Select(x => x.Item1.RawValue > 0 && x.Item2.RawValue > 0 ? 1 : 0).ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(s => RawValue = s);
        IsAnalog = false;
    }


    public override string Generate(DeviceEmulationMode mode)
    {
        return $"{Child1.Generate(mode)} && {Child2.Generate(mode)}";
    }

    public override SerializedInput Serialise()
    {
        return new SerializedMacroInput(Child1.Serialise(), Child2.Serialise());
    }

    public override Input InnermostInput()
    {
        return Child1;
    }

    public override IList<Input> Inputs()
    {
        return new List<Input> {Child1, Child2};
    }

    public override IList<DevicePin> Pins => Child1.Pins.Concat(Child2.Pins).ToList();
    public override IList<PinConfig> PinConfigs => Child1.PinConfigs.Concat(Child2.PinConfigs).ToList();

    public override bool IsUint => false;

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        Child1.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType);
        Child2.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType, wiiControllerType);
    }

    public override string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings, 
        DeviceEmulationMode mode)
    {
        throw new InvalidOperationException("Never call GenerateAll on MacroInput, call it on its children");
    }

    public override void Dispose()
    {
        Child1.Dispose();
        Child2.Dispose();
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return Child1.RequiredDefines().Concat(Child2.RequiredDefines()).ToList();
    }
}
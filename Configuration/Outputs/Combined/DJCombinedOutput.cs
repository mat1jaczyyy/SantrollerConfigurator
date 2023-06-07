using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class DjCombinedOutput : CombinedTwiOutput
{
    public DjCombinedOutput(ConfigViewModel model, int sda = -1, int scl = -1) :
        base(model, DjInput.DjTwiType, DjInput.DjTwiFreq, "DJ", sda, scl)
    {
        Outputs.Clear();
        Outputs.Connect().Filter(x => x is OutputAxis)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    [Reactive] public bool DetectedLeft { get; set; }

    [Reactive] public bool DetectedRight { get; set; }

    public override void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs)
    {
        Outputs.Clear();
        if (outputs.Any())
            Outputs.AddRange(outputs);
        else
            CreateDefaults();
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "DJ Turntable Inputs";
    }

    public override object GetOutputType()
    {
        return SimpleType.DjTurntableSimple;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();

        Outputs.AddRange(DjInputTypes.Where(s => s is not (DjInputType.LeftTurntable or DjInputType.RightTurntable))
            .Select(button => new DjButton(Model,
                new DjInput(button, Model, combined: true),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 5, button, true)));
        Outputs.Add(new DjAxis(Model, new DjInput(DjInputType.LeftTurntable, Model, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), 1, DjAxisType.LeftTableVelocity, true));
        Outputs.Add(new DjAxis(Model, new DjInput(DjInputType.RightTurntable, Model, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), 1, DjAxisType.RightTableVelocity, true));
    }

    public override void UpdateBindings()
    {
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjCombinedOutput(Sda, Scl, Outputs.Items.ToList());
    }

    public override void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType,
        byte[] rfRaw, byte[] usbHostRaw, byte[] bluetoothRaw, byte[] usbHostInputsRaw)
    {
        base.Update(analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, rfRaw, usbHostRaw, bluetoothRaw, usbHostInputsRaw);
        DetectedLeft = djLeftRaw.Any();
        DetectedRight = djRightRaw.Any();
    }
}
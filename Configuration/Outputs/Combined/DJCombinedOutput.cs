using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class DjCombinedOutput : CombinedTwiOutput
{

    private bool _detectedLeft;

    private bool _detectedRight;

    public DjCombinedOutput(ConfigViewModel model, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) :
        base(model, DjInput.DjTwiType, DjInput.DjTwiFreq, "DJ", sda, scl)
    {
        Outputs.Clear();
        if (outputs != null)
            Outputs.AddRange(outputs);
        else
            CreateDefaults();

        Outputs.Connect().Filter(x => x is OutputAxis)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    public bool DetectedLeft
    {
        get => _detectedLeft;
        set => this.RaiseAndSetIfChanged(ref _detectedLeft, value);
    }

    public bool DetectedRight
    {
        get => _detectedRight;
        set => this.RaiseAndSetIfChanged(ref _detectedRight, value);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "DJ Turntable Inputs";
    }

    public void CreateDefaults()
    {
        Outputs.Clear();

        Outputs.AddRange(DjInputTypes.Where(s => s is not (DjInputType.LeftTurntable or DjInputType.RightTurntable))
            .Select(button => new DjButton(Model,
                new DjInput(button, Model, combined: true),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 5, button)));
        Outputs.Add(new DjAxis(Model, new DjInput(DjInputType.LeftTurntable, Model, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), 0, 16, 0, DjAxisType.LeftTableVelocity));
        Outputs.Add(new DjAxis(Model, new DjInput(DjInputType.RightTurntable, Model, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), 0, 16, 0, DjAxisType.RightTableVelocity));
    }

    public override void UpdateBindings()
    {
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjCombinedOutput(Sda, Scl, Outputs.Items.ToList());
    }

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType,
        byte[] rfRaw)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType, rfRaw);
        DetectedLeft = djLeftRaw.Any();
        DetectedRight = djRightRaw.Any();
    }
    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return "Combined/DJ.png";
    }
}
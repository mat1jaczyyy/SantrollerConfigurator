using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class DjCombinedOutput : CombinedTwiOutput
{

    private readonly Microcontroller _microcontroller;

    public DjCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) :
        base(model, microcontroller, DjInput.DjTwiType, DjInput.DjTwiFreq, "DJ", sda, scl)
    {
        _microcontroller = microcontroller;
        Outputs.Clear();
        if (outputs != null)
        {
            Outputs.AddRange(outputs);
        }
        else
        {
            CreateDefaults();
        }

        Outputs.Connect().Filter(x => x is OutputAxis)
            .Bind(out var analogOutputs)
            .Subscribe();
        Outputs.Connect().Filter(x => x is OutputButton)
            .Bind(out var digitalOutputs)
            .Subscribe();
        AnalogOutputs = analogOutputs;
        DigitalOutputs = digitalOutputs;
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        
        Outputs.AddRange(DjInputTypes.Where(s => s is not (DjInputType.LeftTurntable or DjInputType.RightTurntable)).Select(button => new DjButton(Model,
            new DjInput(button, Model, _microcontroller, combined: true),
            Colors.Transparent, Colors.Transparent, Array.Empty<byte>(), 5, button)));
        Outputs.Add(new DjAxis(Model, new DjInput(DjInputType.LeftTurntable, Model, _microcontroller, combined: true),
            Colors.Transparent,
            Colors.Transparent, Array.Empty<byte>(), 0, 16, 0, DjAxisType.LeftTableVelocity));
        Outputs.Add(new DjAxis(Model, new DjInput(DjInputType.RightTurntable, Model, _microcontroller, combined: true),
            Colors.Transparent,
            Colors.Transparent, Array.Empty<byte>(), 0, 16, 0, DjAxisType.RightTableVelocity));
    }

    public override void UpdateBindings()
    {
        
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjCombinedOutput(Sda, Scl, Outputs.Items.ToList());
    }

    private bool _detectedLeft;

    public bool DetectedLeft
    {
        get => _detectedLeft;
        set => this.RaiseAndSetIfChanged(ref _detectedLeft, value);
    }

    private bool _detectedRight;

    public bool DetectedRight
    {
        get => _detectedRight;
        set => this.RaiseAndSetIfChanged(ref _detectedRight, value);
    }

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType);
        DetectedLeft = djLeftRaw.Any();
        DetectedRight = djRightRaw.Any();
    }
}
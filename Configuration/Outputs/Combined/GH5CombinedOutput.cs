using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class Gh5CombinedOutput : CombinedTwiOutput
{
    private static readonly Dictionary<Gh5NeckInputType, StandardButtonType> Buttons = new()
    {
        {Gh5NeckInputType.TapAll, StandardButtonType.A},
        {Gh5NeckInputType.Green, StandardButtonType.A},
        {Gh5NeckInputType.Red, StandardButtonType.B},
        {Gh5NeckInputType.Yellow, StandardButtonType.Y},
        {Gh5NeckInputType.Blue, StandardButtonType.X},
        {Gh5NeckInputType.Orange, StandardButtonType.LeftShoulder}
    };

    private static readonly Dictionary<Gh5NeckInputType, StandardButtonType> Taps = new()
    {
        {Gh5NeckInputType.TapGreen, StandardButtonType.A},
        {Gh5NeckInputType.TapRed, StandardButtonType.B},
        {Gh5NeckInputType.TapYellow, StandardButtonType.Y},
        {Gh5NeckInputType.TapBlue, StandardButtonType.X},
        {Gh5NeckInputType.TapOrange, StandardButtonType.LeftShoulder}
    };
    
    private static readonly Dictionary<Gh5NeckInputType, RBButtonType> TapsRb = new()
    {
        {Gh5NeckInputType.TapGreen, RBButtonType.UpperGreen},
        {Gh5NeckInputType.TapRed, RBButtonType.UpperRed},
        {Gh5NeckInputType.TapYellow, RBButtonType.UpperYellow},
        {Gh5NeckInputType.TapBlue, RBButtonType.UpperBlue},
        {Gh5NeckInputType.TapOrange, RBButtonType.UpperOrange}
    };


    private bool _detected;

    public Gh5CombinedOutput(ConfigViewModel model, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model,
        "gh5", 100000, "GH5", sda, scl)
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

    public bool Detected
    {
        get => _detected;
        set => this.RaiseAndSetIfChanged(ref _detected, value);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "GH5 Slider Inputs";
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var pair in Buttons)
            Outputs.Add(new ControllerButton(Model,
                new Gh5NeckInput(pair.Key, Model, combined: true), Colors.Green,
                Colors.Black, Array.Empty<byte>(), 5, pair.Value));
        foreach (var pair in Taps)
            Outputs.Add(new ControllerButton(Model,
                new Gh5NeckInput(pair.Key, Model, combined: true), Colors.Black,
                Colors.Black, Array.Empty<byte>(), 5, pair.Value));

        Outputs.Add(new ControllerAxis(Model,
            new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, combined: true),
            Colors.Black,
            Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, StandardAxisType.RightStickY));
    }
    
    public override IEnumerable<Output> ValidOutputs()
    {
        var tapAnalog =
            Outputs.Items.FirstOrDefault(s => s is {Enabled: true, Input: Gh5NeckInput {Input: Gh5NeckInputType.TapBar}});
        var tapFrets =
            Outputs.Items.FirstOrDefault(s => s is {Enabled: true, Input: Gh5NeckInput {Input: Gh5NeckInputType.TapAll}});
        if (tapAnalog == null && tapFrets == null) return Outputs.Items;
        var outputs = new List<Output>(Outputs.Items);
        // Map Tap bar to Upper frets on RB guitars
        if (tapAnalog != null && Model.DeviceType is DeviceControllerType.Guitar && Model.RhythmType is RhythmType.RockBand)
        {
            outputs.AddRange(TapsRb.Select(pair => new RbButton(Model, new Gh5NeckInput(pair.Key, Model, Sda, Scl, true), Colors.Black, Colors.Black, Array.Empty<byte>(), 5, pair.Value)));

            outputs.Remove(tapAnalog);
        }

        if (tapFrets == null) return outputs;
        foreach (var pair in Taps)
        {
            outputs.Add(new ControllerButton(Model, new Gh5NeckInput(pair.Key, Model, Sda, Scl, true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), 5, pair.Value));
            outputs.Remove(tapFrets);
        }

        return outputs;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGh5CombinedOutput(Sda, Scl, Outputs.Items.ToList());
    }

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType);
        _detected = gh5Raw.Any();
    }

    public override void UpdateBindings()
    {
        var axisController = Outputs.Items.FirstOrDefault(s => s is ControllerAxis);
        var axisGuitar = Outputs.Items.FirstOrDefault(s => s is GuitarAxis);
        if (Model.DeviceType is DeviceControllerType.Guitar)
        {
            if (axisController == null) return;
            Outputs.Remove(axisController);
            Outputs.Add(new GuitarAxis(Model,
                new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Sda, Scl,
                    combined: true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                GuitarAxisType.Slider));
        }
        else if (axisGuitar != null)
        {
            Outputs.Remove(axisGuitar);
            Outputs.Add(new ControllerAxis(Model,
                new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, Sda, Scl,
                    combined: true),
                Colors.Black,
                Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                StandardAxisType.LeftStickX));
        }
    }
    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return "Combined/GH5.png";
    }
}
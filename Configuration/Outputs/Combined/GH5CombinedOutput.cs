using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public class Gh5CombinedOutput : CombinedTwiOutput
{
    private readonly Microcontroller _microcontroller;

    private static readonly Dictionary<Gh5NeckInputType, StandardButtonType> Buttons = new()
    {
        {Gh5NeckInputType.Green, StandardButtonType.A},
        {Gh5NeckInputType.Red, StandardButtonType.B},
        {Gh5NeckInputType.Yellow, StandardButtonType.Y},
        {Gh5NeckInputType.Blue, StandardButtonType.X},
        {Gh5NeckInputType.Orange, StandardButtonType.LeftShoulder},
    };

    private static readonly Dictionary<Gh5NeckInputType, StandardButtonType> Taps = new()
    {
        {Gh5NeckInputType.TapGreen, StandardButtonType.A},
        {Gh5NeckInputType.TapRed, StandardButtonType.B},
        {Gh5NeckInputType.TapYellow, StandardButtonType.Y},
        {Gh5NeckInputType.TapBlue, StandardButtonType.X},
        {Gh5NeckInputType.TapOrange, StandardButtonType.LeftShoulder},
    };

    public Gh5CombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model,
        microcontroller,
        "gh5", 100000, "GH5", sda, scl)
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
    
    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return "GH5 Slider Inputs";
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        foreach (var pair in Buttons)
        {
            Outputs.Add(new ControllerButton(Model,
                new Gh5NeckInput(pair.Key, Model, _microcontroller, combined: true), Colors.Green,
                Colors.Transparent, Array.Empty<byte>(), 5, pair.Value));
        }
        foreach (var pair in Taps)
        {
            Outputs.Add(new ControllerButton(Model,
                new Gh5NeckInput(pair.Key, Model, _microcontroller, combined: true), Colors.Transparent,
                Colors.Transparent, Array.Empty<byte>(), 5, pair.Value));
        }

        Outputs.Add(new ControllerAxis(Model,
            new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, _microcontroller, combined: true),
            Colors.Transparent,
            Colors.Transparent, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, StandardAxisType.RightStickY));
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGh5CombinedOutput(Sda, Scl, Outputs.Items.ToList());
    }


    private bool _detected;

    public bool Detected
    {
        get => _detected;
        set => this.RaiseAndSetIfChanged(ref _detected, value);
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
    }
}
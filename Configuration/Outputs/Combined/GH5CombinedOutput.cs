using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public class Gh5CombinedOutput : CombinedTwiOutput
{
    private readonly Microcontroller _microcontroller;

    private readonly AvaloniaList<Output> _outputs = new();

    private static readonly Dictionary<Gh5NeckInputType, StandardButtonType> Buttons = new()
    {
        {Gh5NeckInputType.Green, StandardButtonType.A},
        {Gh5NeckInputType.Red, StandardButtonType.B},
        {Gh5NeckInputType.Yellow, StandardButtonType.Y},
        {Gh5NeckInputType.Blue, StandardButtonType.X},
        {Gh5NeckInputType.Orange, StandardButtonType.Lb},
    };

    private static readonly Dictionary<Gh5NeckInputType, StandardButtonType> Taps = new()
    {
        {Gh5NeckInputType.TapGreen, StandardButtonType.A},
        {Gh5NeckInputType.TapRed, StandardButtonType.B},
        {Gh5NeckInputType.TapYellow, StandardButtonType.Y},
        {Gh5NeckInputType.TapBlue, StandardButtonType.X},
        {Gh5NeckInputType.TapOrange, StandardButtonType.Lb},
    };

    public Gh5CombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model,
        microcontroller,
        "gh5", 100000, "GH5", sda, scl)
    {
        _microcontroller = microcontroller;
        if (outputs != null)
        {
            _outputs = new AvaloniaList<Output>(outputs);
        }
        else
        {
            CreateDefaults();
        }
    }

    public void CreateDefaults()
    {
        _outputs.Clear();
        foreach (var pair in Buttons)
        {
            _outputs.Add(new ControllerButton(Model,
                new Gh5NeckInput(pair.Key, Model, _microcontroller, combined: true), Colors.Green,
                Colors.Transparent, Array.Empty<byte>(), 5, pair.Value));
        }

        _outputs.Add(new ControllerAxis(Model,
            new Gh5NeckInput(Gh5NeckInputType.TapBar, Model, _microcontroller, combined: true),
            Colors.Transparent,
            Colors.Transparent, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, StandardAxisType.RightStickY));
    }

    public void AddTapBarFrets()
    {
        foreach (var pair in Taps)
        {
            _outputs.Add(new ControllerButton(Model,
                new Gh5NeckInput(pair.Key, Model, _microcontroller, combined: true), Colors.Transparent,
                Colors.Transparent, Array.Empty<byte>(), 5, pair.Value));
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGh5CombinedOutput(Sda, Scl, _outputs.ToList());
    }

    public override AvaloniaList<Output> Outputs => _outputs;

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
}
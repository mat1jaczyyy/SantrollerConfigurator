using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media;
using DynamicData;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public class GhwtCombinedOutput : CombinedOutput
{
    public int Pin { get; set; }
    private readonly Microcontroller _microcontroller;

    private static readonly Dictionary<GhWtInputType, StandardButtonType> Taps = new()
    {
        {GhWtInputType.TapGreen, StandardButtonType.A},
        {GhWtInputType.TapRed, StandardButtonType.B},
        {GhWtInputType.TapYellow, StandardButtonType.Y},
        {GhWtInputType.TapBlue, StandardButtonType.X},
        {GhWtInputType.TapOrange, StandardButtonType.Lb},
    };

    public GhwtCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? pin = null,
        IReadOnlyCollection<Output>? outputs = null) : base(model, null, "GHWT")
    {
        _microcontroller = microcontroller;
        if (pin.HasValue)
        {
            Pin = pin.Value;
        }
        Outputs.Clear();
        if (outputs != null)
        {
            Outputs.AddRange(outputs);
        }
        else
        {
            CreateDefaults();
        }
    }

    public void CreateDefaults()
    {
        Outputs.Clear();
        Outputs.Add(new ControllerAxis(Model,
            new GhWtTapInput(GhWtInputType.TapBar, Model, _microcontroller,
                combined: true),
            Colors.Transparent,
            Colors.Transparent, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0, StandardAxisType.LeftStickX));
    }

    public void AddTapBarFrets()
    {
        foreach (var pair in Taps)
        {
            Outputs.Add(new ControllerButton(Model,
                new GhWtTapInput(pair.Key, Model, _microcontroller,
                    combined: true), Colors.Transparent,
                Colors.Transparent, Array.Empty<byte>(), 5, pair.Value));
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGhwtCombinedOutput(Pin, Outputs.Items.ToList());
    }

    public override void UpdateBindings()
    {
    }
}
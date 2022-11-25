using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media;
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

    private readonly AvaloniaList<Output> _outputs = new();

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
        _outputs.Add(new ControllerAxis(Model,
            new GhWtTapInput(GhWtInputType.TapBar, Model, _microcontroller,
                combined: true),
            Colors.Transparent,
            Colors.Transparent, 0, short.MinValue, short.MaxValue, 0, StandardAxisType.LeftStickX));
    }

    public void AddTapBarFrets()
    {
        foreach (var pair in Taps)
        {
            _outputs.Add(new ControllerButton(Model,
                new GhWtTapInput(pair.Key, Model, _microcontroller,
                    combined: true), Colors.Transparent,
                Colors.Transparent, 0, 5, pair.Value));
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGhwtCombinedOutput(Pin, _outputs.ToList());
    }

    public override AvaloniaList<Output> Outputs => _outputs;
}
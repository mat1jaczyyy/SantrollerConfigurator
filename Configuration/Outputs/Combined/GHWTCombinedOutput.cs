using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public class GhwtCombinedOutput : CombinedOutput
{
    public bool MapTapBarToAxis { get; set; }
    public bool MapTapBarToFrets { get; set; }

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
        bool mapTapBarToFrets = false, bool mapTapBarToAxis = false) : base(model, null, "GHWT")
    {
        _microcontroller = microcontroller;
        MapTapBarToFrets = mapTapBarToAxis;
        MapTapBarToAxis = mapTapBarToAxis;
        if (pin.HasValue)
        {
            Pin = pin.Value;
        }
    }

    public override SerializedOutput GetJson()
    {
        return new SerializedGhwtCombinedOutput(Pin, MapTapBarToFrets, MapTapBarToAxis);
    }

    public override IReadOnlyList<Output> GetOutputs(IList<Output> bindings) => GetBindings(bindings);

    private IReadOnlyList<Output> GetBindings(IList<Output> bindings)
    {
        List<Output> outputs = new();
        var inputs = bindings.Select(s => s.Input?.InnermostInput()).Where(s => s is GhWtTapInput).Cast<GhWtTapInput>()
            .Select(s => s.Input).ToHashSet();
        if (MapTapBarToAxis && !inputs.Contains(GhWtInputType.TapBar))
        {
            outputs.Add(new ControllerAxis(Model, new GhWtTapInput(GhWtInputType.TapBar, _microcontroller),
                Colors.Transparent,
                Colors.Transparent, null, 1, 0, 0, StandardAxisType.RightStickY));
        }

        if (MapTapBarToFrets)
        {
            foreach (var pair in Taps)
            {
                if (inputs.Contains(pair.Key)) continue;
                outputs.Add(new ControllerButton(Model, new GhWtTapInput(pair.Key, _microcontroller), Colors.Green,
                    Colors.Transparent, null, 5, pair.Value));
            }
        }

        return outputs.AsReadOnly();
    }
}
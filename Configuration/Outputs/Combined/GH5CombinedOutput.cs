using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public class Gh5CombinedOutput : CombinedTwiOutput
{
    private readonly Microcontroller _microcontroller;
    public bool MapTapBarToAxis { get; set; }
    public bool MapTapBarToFrets { get; set; }
    public bool FretsEnabled { get; set; }

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
        bool fretsEnabled = true, bool mapTapBarToFrets = false, bool mapTapBarToAxis = false) : base(model,
        microcontroller,
        "gh5", 100000, "GH5", sda, scl)
    {
        _microcontroller = microcontroller;
        FretsEnabled = fretsEnabled;
        MapTapBarToAxis = mapTapBarToAxis;
        MapTapBarToFrets = mapTapBarToFrets;
    }

    public override SerializedOutput GetJson()
    {
        return new SerializedGh5CombinedOutput(Sda, Scl, FretsEnabled, MapTapBarToFrets,
            MapTapBarToAxis);
    }

    public override IReadOnlyList<Output> GetOutputs(IList<Output> bindings) => GetBindings(bindings);

    private IReadOnlyList<Output> GetBindings(IList<Output> bindings)
    {
        List<Output> outputs = new();
        var inputs = bindings.Select(s => s.Input?.InnermostInput()).Where(s => s is Gh5NeckInput).Cast<Gh5NeckInput>()
            .Select(s => s.Input).ToHashSet();

        if (FretsEnabled)
        {
            foreach (var pair in Buttons)
            {
                if (inputs.Contains(pair.Key)) continue;
                outputs.Add(new ControllerButton(Model, new Gh5NeckInput(pair.Key, _microcontroller), Colors.Green,
                    Colors.Transparent, null, 5, pair.Value));
            }
        }

        if (MapTapBarToAxis)
        {
            if (!inputs.Contains(Gh5NeckInputType.TapBar))
            {
                outputs.Add(new ControllerAxis(Model, new Gh5NeckInput(Gh5NeckInputType.TapBar, _microcontroller),
                    Colors.Transparent,
                    Colors.Transparent, null, 1, 0, 0, StandardAxisType.RightStickY));
            }
        }

        if (MapTapBarToFrets)
        {
            foreach (var pair in Taps)
            {
                if (inputs.Contains(pair.Key)) continue;
                outputs.Add(new ControllerButton(Model, new Gh5NeckInput(pair.Key, _microcontroller), Colors.Green,
                    Colors.Transparent, null, 5, pair.Value));
            }
        }

        return outputs.AsReadOnly();
    }
}
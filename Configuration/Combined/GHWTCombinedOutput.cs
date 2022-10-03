using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;

public class GHWTCombinedOutput : Output
{
    private readonly List<Output> BindingsTap;
    private readonly Output BindingTapBar;
    public bool TapEnabled { get; set; }
    public bool MapTapToFrets { get; set; }

    public GHWTCombinedOutput(ConfigViewModel model) : base(model, null, Colors.Transparent, Colors.Transparent, "GHWT")
    {
        BindingsTap = new()
        {
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapGreen, model.MicroController!), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.A),
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapRed, model.MicroController!), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.B),
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapYellow, model.MicroController!), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.Y),
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapBlue, model.MicroController!), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.X),
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapOrange, model.MicroController!), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.LB),
        };
        BindingTapBar = new ControllerAxis(model, new GhWtTapInput(GhWtInputType.TapBar, model.MicroController!),
            Colors.Transparent,
            Colors.Transparent, 1, 0, 0, StandardAxisType.RightStickY);
    }

    public override bool IsCombined => true;

    public override string Generate(bool xbox)
    {
        return "";
    }

    public override IReadOnlyList<Output> Outputs => GetBindings();

    private IReadOnlyList<Output> GetBindings()
    {
        List<Output> outputs = new();

        if (TapEnabled)
        {
            outputs.Add(BindingTapBar);
        }

        if (MapTapToFrets)
        {
            outputs.AddRange(BindingsTap);
        }

        return outputs.AsReadOnly();
    }
}
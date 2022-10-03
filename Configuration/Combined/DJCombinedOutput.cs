using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;

public class DJCombinedOutput : Output
{
    private readonly List<Output> Bindings;

    public DJCombinedOutput(ConfigViewModel model) : base(model, null, Colors.Transparent, Colors.Transparent, "DJ")
    {
        Bindings = new()
        {
            new ControllerButton(model, new DjInput(DjInputType.LeftAny), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.LB),
            new ControllerButton(model, new DjInput(DjInputType.LeftGreen), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.A),
            new ControllerButton(model, new DjInput(DjInputType.LeftRed), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.B),
            new ControllerButton(model, new DjInput(DjInputType.LeftBlue), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.X),
            new ControllerButton(model, new DjInput(DjInputType.RightAny), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.RB),
            new ControllerButton(model, new DjInput(DjInputType.RightGreen), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.A),
            new ControllerButton(model, new DjInput(DjInputType.RightRed), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.B),
            new ControllerButton(model, new DjInput(DjInputType.RightBlue), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.X),
            new ControllerAxis(model, new DjInput(DjInputType.LeftTurntable), Colors.Transparent, Colors.Transparent, 1,
                0, 0, StandardAxisType.LeftStickX),
            new ControllerAxis(model, new DjInput(DjInputType.RightTurnable), Colors.Transparent, Colors.Transparent, 1,
                0, 0, StandardAxisType.LeftStickY)
        };
    }

    public override bool IsCombined => true;

    public override string Generate(bool xbox)
    {
        return "";
    }

    public override IReadOnlyList<Output> Outputs => Bindings;
}
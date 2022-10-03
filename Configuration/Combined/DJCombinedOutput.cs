using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;

public class DJCombinedOutput : TwiOutput
{
    private readonly List<Output> _bindings;

    public DJCombinedOutput(ConfigViewModel model, Microcontroller.Microcontroller microcontroller) : base(model, microcontroller, DjInput.DjTwiType, DjInput.DjTwiFreq, "DJ")
    {
        _bindings = new()
        {
            new ControllerButton(model, new DjInput(DjInputType.LeftAny, microcontroller), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.LB),
            new ControllerButton(model, new DjInput(DjInputType.LeftGreen, microcontroller), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.A),
            new ControllerButton(model, new DjInput(DjInputType.LeftRed, microcontroller), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.B),
            new ControllerButton(model, new DjInput(DjInputType.LeftBlue, microcontroller), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.X),
            new ControllerButton(model, new DjInput(DjInputType.RightAny, microcontroller), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.RB),
            new ControllerButton(model, new DjInput(DjInputType.RightGreen, microcontroller), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.A),
            new ControllerButton(model, new DjInput(DjInputType.RightRed, microcontroller), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.B),
            new ControllerButton(model, new DjInput(DjInputType.RightBlue, microcontroller), Colors.Transparent, Colors.Transparent, 5,
                StandardButtonType.X),
            new ControllerAxis(model, new DjInput(DjInputType.LeftTurntable, microcontroller), Colors.Transparent, Colors.Transparent, 1,
                0, 0, StandardAxisType.LeftStickX),
            new ControllerAxis(model, new DjInput(DjInputType.RightTurnable, microcontroller), Colors.Transparent, Colors.Transparent, 1,
                0, 0, StandardAxisType.LeftStickY)
        };
    }

    public override bool IsCombined => true;

    public override string Generate(bool xbox)
    {
        return "";
    }

    public override IReadOnlyList<Output> Outputs => _bindings;
}
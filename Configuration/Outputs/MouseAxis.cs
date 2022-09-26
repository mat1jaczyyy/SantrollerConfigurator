using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class MouseAxis : OutputAxis
{
    
    public static Dictionary<MouseAxisType, string> mappings = new() {
        {MouseAxisType.X, "X"},
        {MouseAxisType.Y, "Y"},
        {MouseAxisType.ScrollX, "ScrollX"},
        {MouseAxisType.ScrollY, "ScrollY"},
    };
    public MouseAxis(ConfigViewModel model, IInput? input, Color ledOn, Color ledOff, float multiplier, int offset, int deadzone, bool trigger) : base(model, input, ledOn, ledOff, multiplier, offset, deadzone)
    {
    }
    public MouseAxisType Type { get; }
    public override string Name => Type.ToString();
    // TODO this
    public override bool Trigger => false;

    public override string GenerateOutput(bool xbox)
    {
        return "report->" + mappings[Type];
    }
}
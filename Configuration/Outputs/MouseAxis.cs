using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Json;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
public class MouseAxis : OutputAxis
{
    private static readonly Dictionary<MouseAxisType, string> Mappings = new() {
        {MouseAxisType.X, "X"},
        {MouseAxisType.Y, "Y"},
        {MouseAxisType.ScrollX, "ScrollX"},
        {MouseAxisType.ScrollY, "ScrollY"},
    };
    public MouseAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, float multiplier, int offset, int deadzone, MouseAxisType type) : base(model, input, ledOn, ledOff, multiplier, offset, deadzone, type.ToString())
    {
        Type = type;
    }
    public MouseAxisType Type { get; }
    public override bool Trigger => false;

    public override string GenerateOutput(bool xbox)
    {
        return "report->" + Mappings[Type];
    }
    public override bool IsCombined => false;
    public override JsonOutput GetJson()
    {
        return new JsonMouseAxis(Input?.GetJson(), Type, LedOn, LedOff, Multiplier, Offset, Deadzone);
    }
}
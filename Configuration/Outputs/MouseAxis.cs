using System;
using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class MouseAxis : OutputAxis
{
    private static readonly Dictionary<MouseAxisType, string> Mappings = new()
    {
        {MouseAxisType.X, "X"},
        {MouseAxisType.Y, "Y"},
        {MouseAxisType.ScrollX, "ScrollX"},
        {MouseAxisType.ScrollY, "ScrollY"},
    };

    public MouseAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, int? ledIndex, float multiplier,
        int offset, int deadZone, MouseAxisType type) : base(model, input, ledOn, ledOff, ledIndex, multiplier, offset,
        deadZone, type.ToString(), (_) => false)
    {
        Type = type;
    }

    public MouseAxisType Type { get; }

    protected override string GenerateOutput(bool xbox)
    {
        return "report->" + Mappings[Type];
    }

    public override bool IsCombined => false;

    protected override string MinCalibrationText()
    {
        switch (Type)
        {
            case MouseAxisType.X:
            case MouseAxisType.ScrollX:
                return "Move axis to the leftmost position";
            case MouseAxisType.Y:
            case MouseAxisType.ScrollY:
                return "Move axis to the lowest position";
            default:
                return "";
        }

    }
    
    protected override string MaxCalibrationText()
    {
        switch (Type)
        {
            case MouseAxisType.X:
            case MouseAxisType.ScrollX:
                return "Move axis to the rightmost position";
            case MouseAxisType.Y:
            case MouseAxisType.ScrollY:
                return "Move axis to the highest position";
            default:
                return "";
        }

    }

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override string? GetLocalisedName() => Name;

    public override SerializedOutput GetJson()
    {
        return new SerializedMouseAxis(Input?.GetJson(), Type, LedOn, LedOff, LedIndex, Multiplier, Offset, DeadZone);
    }
}
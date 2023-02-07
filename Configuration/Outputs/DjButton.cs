using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class DjButton : OutputButton
{
    public readonly DjInputType Type;

    public DjButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce,
        DjInputType type) : base(model, input, ledOn, ledOff, ledIndices, debounce, type.ToString())
    {
        Type = type;
    }

    public override string GenerateOutput(DeviceEmulationMode mode)
    {
        return GetReportField(Type);
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;

    public override bool IsStrum => false;

    public override bool Valid => true;

    public override string Generate(DeviceEmulationMode mode, List<int> debounceIndex, bool combined, string extra)
    {
        // Turntables also hit the standard buttons when you push each button
        switch (Type)
        {
            case DjInputType.LeftGreen:
            case DjInputType.RightGreen:
                extra = "report->a = true;";
                break;
            case DjInputType.LeftRed:
            case DjInputType.RightRed:
                extra = "report->b = true;";
                break;
            case DjInputType.LeftBlue:
            case DjInputType.RightBlue:
                extra = "report->x = true;";
                break;
            default:
                return "";
        }
        return base.Generate(mode, debounceIndex, combined, extra);
    }
    
    public override SerializedOutput Serialize()
    {
        return new SerializedDjButton(Input?.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}
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
        DjInputType type) : base(model, input, ledOn, ledOff, ledIndices, debounce, EnumToStringConverter.Convert(type))
    {
        Type = type;
    }

    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public override bool IsKeyboard => false;
    public override bool IsController => true;


    public override bool IsStrum => false;

    public override bool Valid => true;

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (mode is ConfigField.Xbox360Mask or ConfigField.XboxOneMask or ConfigField.Ps3Mask)
            return base.Generate(mode, debounceIndex, combined, extra);
        if (mode is not (ConfigField.Ps3 or ConfigField.Shared or ConfigField.XboxOne or ConfigField.Xbox360))
            return "";
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

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"DJ/{Name}.png";
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjButton(Input?.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}
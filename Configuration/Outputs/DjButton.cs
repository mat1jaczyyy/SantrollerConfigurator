using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class DjButton : OutputButton
{
    public readonly DjInputType Type;

    public DjButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce,
        DjInputType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, debounce,
        childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }

    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public override bool IsKeyboard => false;


    public override bool IsStrum => false;

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros)
    {
        if (mode is not (ConfigField.Ps3 or ConfigField.Shared or ConfigField.XboxOne or ConfigField.Xbox360))
            return "";
        if (mode is not ConfigField.Shared) {
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
        }

        return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override object GetOutputType()
    {
        return Type;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type,
            ChildOfCombined);
    }
}

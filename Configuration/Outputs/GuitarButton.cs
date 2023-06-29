using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class GuitarButton : OutputButton
{
    public readonly InstrumentButtonType Type;

    public GuitarButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce,
        InstrumentButtonType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, debounce,
        childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }

    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public override bool IsKeyboard => false;


    public override bool IsStrum => Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp;

    public override string GenerateOutput(ConfigField mode)
    {
        // PS3 and 360 just set the standard buttons, and rely on the solo flag
        // XB1 however has things broken out
        return Type switch
        {
            InstrumentButtonType.StrumUp => GetReportField(StandardButtonType.DpadUp),
            InstrumentButtonType.StrumDown => GetReportField(StandardButtonType.DpadDown),
            InstrumentButtonType.SoloGreen or InstrumentButtonType.Green when mode is not ConfigField.XboxOne =>
                GetReportField(StandardButtonType.A),
            InstrumentButtonType.SoloRed or InstrumentButtonType.Red when mode is not ConfigField.XboxOne =>
                GetReportField(StandardButtonType.B),
            InstrumentButtonType.SoloYellow or InstrumentButtonType.Yellow when mode is not ConfigField.XboxOne =>
                GetReportField(StandardButtonType.Y),
            InstrumentButtonType.SoloBlue or InstrumentButtonType.Blue when mode is not ConfigField.XboxOne =>
                GetReportField(StandardButtonType.X),
            InstrumentButtonType.SoloOrange or InstrumentButtonType.Orange when mode is not ConfigField.XboxOne =>
                GetReportField(StandardButtonType.LeftShoulder),
            InstrumentButtonType.Black1 => GetReportField(StandardButtonType.A),
            InstrumentButtonType.Black2 => GetReportField(StandardButtonType.B),
            InstrumentButtonType.White1 => GetReportField(StandardButtonType.X),
            InstrumentButtonType.Black3 => GetReportField(StandardButtonType.Y),
            InstrumentButtonType.White2 => GetReportField(StandardButtonType.LeftShoulder),
            InstrumentButtonType.White3 => GetReportField(StandardButtonType.RightShoulder),
            _ => GetReportField(Type)
        };
    }


    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override object GetOutputType()
    {
        return Type;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros)
    {
        if (mode is not (ConfigField.Shared or ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.Xbox360
            or ConfigField.XboxOne)) return "";
        // If combined debounce is on, then additionally generate extra logic to ignore this input if the opposite debounce flag is active
        if (combinedDebounce.Any() && Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp)
            combinedExtra = string.Join(" && ", combinedDebounce.Where(s => s != debounceIndex).Select(x => $"!debounce[{x}]"));
        // GHL Guitars map strum up and strum down to dpad up and down, and also the stick
        if (Model.DeviceType is DeviceControllerType.LiveGuitar &&
            Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp &&
            mode is ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.Xbox360 or ConfigField.XboxOne)
            return base.Generate(mode, debounceIndex,
                $"report->strumBar={(Type is InstrumentButtonType.StrumDown ? "0xFF" : "0x00")};", combinedExtra,
                combinedDebounce, macros);

        if (Model is not {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand})
            return base.Generate(mode, debounceIndex, "", combinedExtra, combinedDebounce, macros);

        //This stuff is only relevant for rock band guitars

        // Set solo flag
        if (Type is InstrumentButtonType.SoloBlue or InstrumentButtonType.SoloGreen
                or InstrumentButtonType.SoloOrange or InstrumentButtonType.SoloRed
                or InstrumentButtonType.SoloYellow && mode is not ConfigField.Shared)
            extra = "report->solo=true;";
        // For bluetooth, we shove in a XB1 style version too, so that that can be used at the other end.
        var ret = "";
        switch (mode)
        {
            case ConfigField.Ps3:
                // For bluetooth, we shove the xb1 bits into some unused bytes of the report
                ret += $@"if (bluetooth) {{
                    {base.Generate(ConfigField.XboxOne, debounceIndex, "", combinedExtra, combinedDebounce, macros)}
                }}";
                break;
            // XB1 also needs to set the normal face buttons, which can conveniently be done using the PS3 format
            // Also sets solo flag too
            case ConfigField.XboxOne:
                return ret + base.Generate(mode, debounceIndex, $"{GenerateOutput(ConfigField.Ps3)}=true;{extra}",
                    combinedExtra, combinedDebounce, macros);
        }


        return ret + base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedRbButton(Input!.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type,
            ChildOfCombined);
    }
}
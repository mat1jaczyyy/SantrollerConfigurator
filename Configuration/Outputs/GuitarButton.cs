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
        InstrumentButtonType type) : base(model, input, ledOn, ledOff, ledIndices, debounce)
    {
        Type = type;
        UpdateDetails();
    }

    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public override bool IsKeyboard => false;


    public override bool IsStrum => false;

    public override bool Valid => true;

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
            _ => GetReportField(Type)
        };
    }


    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"RockBand/{Type}.png";
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (mode is not (ConfigField.Shared or ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.Xbox360 or ConfigField.XboxOne)) return "";
        // GHL Guitars map strum up and strum down to dpad up and down, and also the stick
        if (Model.DeviceType is DeviceControllerType.LiveGuitar &&
            Type is InstrumentButtonType.StrumDown or InstrumentButtonType.StrumUp &&
            mode is ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.Xbox360 or ConfigField.XboxOne)
        {
            return base.Generate(mode, debounceIndex, combined,
                $"report->strumBar={(Type is InstrumentButtonType.StrumDown ? "0xFF" : "0x00")};");
        }

        if (Model is not {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand})
            return base.Generate(mode, debounceIndex, combined, "");
        
        //This stuff is only relevant for rock band guitars
        // For RF and bluetooth, we shove in a XB1 style version too, so that that can be used at the other end.
        var ret = "";
        switch (mode)
        {
            case ConfigField.Ps3:
                // For rf and bluetooth, we shove the xb1 bits into some unused bytes of the report
                ret += $@"if (rf_or_bluetooth) {{
                    {base.Generate(ConfigField.XboxOne, debounceIndex, combined, "")}
                }}";
                break;
            // XB1 also needs to set the normal face buttons, which can conveniently be done using the PS3 format
            case ConfigField.XboxOne:
                return ret + base.Generate(mode, debounceIndex, combined, $"{GenerateOutput(ConfigField.Ps3)}=true;");
        }


        // Set xb360 and ps3 use a solo flag for the solo frets
        if (Type is InstrumentButtonType.SoloBlue or InstrumentButtonType.SoloGreen
            or InstrumentButtonType.SoloOrange or InstrumentButtonType.SoloRed
            or InstrumentButtonType.SoloYellow)
        {
            extra = "report->solo=true;";
        }

        return ret + base.Generate(mode, debounceIndex, combined, extra);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedRBButton(Input!.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}
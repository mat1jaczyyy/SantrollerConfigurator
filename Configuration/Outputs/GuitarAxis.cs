using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class GuitarAxis : OutputAxis
{
    public GuitarAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff,
        byte[] ledIndices, int min, int max, int deadZone, GuitarAxisType type) : base(model, input, ledOn,
        ledOff, ledIndices, min, max, deadZone, "Guitar" + type, false)
    {
        Type = type;
    }

    public GuitarAxisType Type { get; }

    public override bool IsKeyboard => false;
    public override bool IsController => true;

    public override bool Valid => true;

    public override string LedOnLabel
    {
        get
        {
            return Type switch
            {
                GuitarAxisType.Tilt => "Highest Tilt LED Colour",
                GuitarAxisType.Whammy => "Whammy Pressed LED Colour",
                _ => ""
            };
        }
    }

    public override string LedOffLabel
    {
        get
        {
            return Type switch
            {
                GuitarAxisType.Tilt => "Lowest Tilt LED Colour",
                GuitarAxisType.Whammy => "Whammy Released LED Colour",
                _ => ""
            };
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGuitarAxis(Input!.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone);
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"{rhythmType}/{Name}.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (mode is not (ConfigField.Ps3 or ConfigField.XboxOne or ConfigField.Xbox360 or ConfigField.Ps3Mask
            or ConfigField.Xbox360Mask or ConfigField.XboxOneMask)) return "";
        if (Input == null) throw new IncompleteConfigurationException("Missing input!");
        var led = Input is FixedInput ? "" : CalculateLeds(mode);
        switch (mode)
        {
            // Xb1 is RB only, so no slider
            case ConfigField.XboxOneMask when Type == GuitarAxisType.Slider:
            case ConfigField.XboxOne when Type == GuitarAxisType.Slider:
                return "";
            case ConfigField.Ps3Mask when Type == GuitarAxisType.Tilt:
                return GetMaskField(GenerateOutput(mode), mode) + GetMaskField("tilt_pc", mode);
            case ConfigField.Ps3
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.GuitarHero} &&
                     Type == GuitarAxisType.Tilt:
                return $@"if (consoleType == PS3) {{
                         {GenerateOutput(mode)} = {GenerateAssignment(mode, true, false, false)};
                      }} else {{
                         report->tilt_pc = -{GenerateAssignment(mode, false, false, false)};
                      }} {led}";
            // PS3 RB expects tilt as a digital bit, so map that here
            // On pc, we use a standard axis because that works better in games like clone hero
            case ConfigField.Ps3
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Tilt:
                return $@"if (consoleType == PS3) {{
                         {GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, false)} == 0xFF;
                      }} else {{
                         report->tilt_pc = -{GenerateAssignment(mode, false, false, false)};
                      }} {led}";
            // Xbox 360 Pickup Selector is actually on one of the triggers.
            case ConfigField.Xbox360
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Pickup:
                return $"{GenerateOutput(mode)} = {GenerateAssignment(mode, false, true, false)}; {led}";
            case ConfigField.Xbox360Mask:
            case ConfigField.XboxOneMask:
            case ConfigField.Ps3Mask:
                return GetMaskField(GenerateOutput(mode), mode);
            default:
                return $"{GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, false)}; {led}";
        }
    }

    protected override string MinCalibrationText()
    {
        return Type switch
        {
            GuitarAxisType.Tilt => "Leave the guitar in a neutral position",
            GuitarAxisType.Whammy => "Release the whammy",
            _ => ""
        };
    }

    protected override string MaxCalibrationText()
    {
        return Type switch
        {
            GuitarAxisType.Tilt => "Tilt the guitar up",
            GuitarAxisType.Whammy => "Push the whammy all the way in",
            _ => ""
        };
    }

    protected override bool SupportsCalibration()
    {
        return true;
    }
}
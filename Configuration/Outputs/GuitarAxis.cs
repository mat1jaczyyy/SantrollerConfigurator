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
    public GuitarAxisType Type { get; }

    public GuitarAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff,
        byte[] ledIndices, int min, int max, int deadZone, GuitarAxisType type) : base(model, input, ledOn,
        ledOff, ledIndices, min, max, deadZone, "Guitar" + type, false)
    {
        Type = type;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGuitarAxis(Input!.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max, DeadZone);
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;
    public override bool Valid => true;

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (Input == null) throw new IncompleteConfigurationException("Missing input!");
        switch (mode)
        {
            case ConfigField.Shared or ConfigField.Consumer or ConfigField.Keyboard or ConfigField.Mouse:
            // Xb1 is RB only, so no slider
            case ConfigField.XboxOne when Type == GuitarAxisType.Slider:
                return "";
            default:
            {
                var led = Input is FixedInput ? "" : CalculateLeds(mode);

                switch (mode)
                {
                    // PS3 GH expects tilt on the accel byte, so force accelerometer values here
                    // On pc, we use a standard axis because that works better in games like clone hero
                    case ConfigField.Ps3 when Model is {DeviceType: DeviceControllerType.LiveGuitar} && Type == GuitarAxisType.Tilt:
                    case ConfigField.Ps3 when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.GuitarHero} && Type == GuitarAxisType.Tilt:
                        return $@"if (consoleType == PS3) {{
                         {GenerateOutput(mode)} = {GenerateAssignment(mode, true, false, false)};
                      }} else {{
                         report->tilt_pc = -{GenerateAssignment(mode, false, false, false)};
                      }} {led}";
                    // PS3 RB expects tilt as a digital bit, so map that here
                    // On pc, we use a standard axis because that works better in games like clone hero
                    case ConfigField.Ps3 when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} && Type == GuitarAxisType.Tilt:
                        return $@"if (consoleType == PS3) {{
                         {GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, false)} == 0xFF;
                      }} else {{
                         report->tilt_pc = -{GenerateAssignment(mode, false, false, false)};
                      }} {led}";
                    // Xbox 360 Pickup Selector is actually on one of the triggers.
                    case ConfigField.Xbox360 when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} && Type == GuitarAxisType.Pickup:
                        return $"{GenerateOutput(mode)} = {GenerateAssignment(mode, false, true, false)}; {led}";
                    default:
                        return $"{GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, false)}; {led}";
                }
            }
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

    protected override bool SupportsCalibration()
    {
        return true;
    }
}
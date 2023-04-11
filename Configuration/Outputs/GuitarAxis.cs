using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class GuitarAxis : OutputAxis
{
    public GuitarAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff,
        byte[] ledIndices, int min, int max, int deadZone, GuitarAxisType type) : base(model, input, ledOn,
        ledOff, ledIndices, min, max, deadZone, type == GuitarAxisType.Slider)
    {
        Type = type;
        UpdateDetails();
    }

    public GuitarAxisType Type { get; }

    public override bool IsKeyboard => false;

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

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
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
        return $"{rhythmType}/{Type}.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (mode == ConfigField.Shared) return base.Generate(mode, debounceIndex, combined, extra);
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.XboxOne or ConfigField.Xbox360)) return "";
        // The below is a mess... but essentially we have to handle converting the input to its respective output depending on console
        // We have to do some hyper specific stuff for digital to analog here too so its easiest to capture its value once
        var analogOn = 0;
        if (Input is DigitalToAnalog dta)
        {
            analogOn = dta.On;
            // Slider is really a uint8_t, so just cut off the extra bits
            if (Type == GuitarAxisType.Slider)
            {
                analogOn &= 0xFF;
            }
        }

        switch (mode)
        {
            case ConfigField.Xbox360 when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                // x360 slider is actually a int16_t BUT there is a mechanism to convert the uint8 value to its uint16_t version
                if (analogOn > 0x80)
                {
                    analogOn |= (analogOn - 1) << 8;
                }
                else
                {
                    analogOn |= analogOn << 8;
                }

                return $@"if ({Input.Generate(mode)}) {{
                                  {GenerateOutput(mode)} = {analogOn};
                              }}";
            case ConfigField.Xbox360 when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                // x360 slider is actually a int16_t BUT there is a mechanism to convert the uint8 value to its uint16_t version
                return $@"
                        {GenerateOutput(mode)} = {Input.Generate(mode)};
                        if ({GenerateOutput(mode)} > 0x80) {{
                            {GenerateOutput(mode)} |= ({GenerateOutput(mode)}-1) << 8;
                        }} else {{
                            {GenerateOutput(mode)} |= ({GenerateOutput(mode)}) << 8;
                        }}
                    ";
            case ConfigField.Ps3 or ConfigField.Ps4 when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                return $@"if ({Input.Generate(mode)}) {{
                                  {GenerateOutput(mode)} = {analogOn & 0xFF};
                              }}";
            case ConfigField.Ps3 or ConfigField.Ps4 when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                return $"{GenerateOutput(mode)} = {Input.Generate(mode)};";
            // Xb1 is RB only, so no slider
            case ConfigField.XboxOne when Type == GuitarAxisType.Slider:
                return "";

            // PS3 GH and GHL expects tilt on the tilt axis
            // On pc, we use a standard axis because that works better in games like clone hero
            case ConfigField.Ps3
                when Model is
                     {
                         DeviceType: DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar,
                         RhythmType: RhythmType.GuitarHero
                     } &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
                return $@"if ({Input.Generate(mode)}) {{
                                  if (consoleType == PS3 || consoleType == REAL_PS3) {{
                                     report->tilt = 0x280;
                                  }} else {{
                                     report->tilt_pc = 0;
                                  }}
                              }}";
            case ConfigField.Ps3
                when Model is
                     {
                         DeviceType: DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar,
                         RhythmType: RhythmType.GuitarHero
                     } &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:
                return $@"if (consoleType == PS3 || consoleType == REAL_PS3) {{
                         {GenerateOutput(mode)} = {GenerateAssignment(mode, true, false, false)};
                      }} else {{
                         report->tilt_pc = {GenerateAssignment(mode, false, false, false)};
                      }}";
            case ConfigField.Ps3
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
                // PS3 rb uses a digital bit, so just map the bit right across and skip the analog conversion
                return $@"if ({Input.Generate(mode)}) {{
                                  if (consoleType == PS3 || consoleType == REAL_PS3) {{
                                     report->tilt = true;
                                  }} else {{
                                     report->tilt_pc = 0;
                                  }}
                              }}";
            case ConfigField.Ps3
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:
                // PS3 RB expects tilt as a digital bit, so map that here
                // On pc, we use a standard axis because that works better in games like clone hero
                return $@"if (consoleType == PS3 || consoleType == REAL_PS3) {{
                         {GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, false)} == 0xFF;
                      }} else {{
                         report->tilt_pc = {GenerateAssignment(mode, false, false, false)};
                      }}";
            // Xbox 360 Pickup Selector is actually on one of the triggers.
            case ConfigField.Xbox360
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Pickup && Input is DigitalToAnalog:
                // Was int16_t (axis), needs to become uint8_t (trigger)
                var val = analogOn;
                val = (val >> 8) + 128;
                return $@"if ({Input.Generate(mode)}) {{
                                  {GenerateOutput(mode)} = {val};
                              }}";
            case ConfigField.Xbox360
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Pickup && Input is not DigitalToAnalog:
                return $"{GenerateOutput(mode)} = {GenerateAssignment(mode, false, true, false)};";
            default:
                return base.Generate(mode, debounceIndex, combined, extra);
        }
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class GuitarAxis : OutputAxis
{
    public static readonly Dictionary<PickupSelectorType, int> PickupSelectorRanges = new()
    {
        {PickupSelectorType.Chorus, 0x40},
        {PickupSelectorType.WahWah, 0x70},
        {PickupSelectorType.Flanger, 0xA0},
        {PickupSelectorType.Echo, 0xD0},
        {PickupSelectorType.None, 0xFF},
    };
    public static readonly Dictionary<PickupSelectorType, int> PickupSelectorRangesXB1 = new()
    {
        {PickupSelectorType.Chorus, 0x0},
        {PickupSelectorType.WahWah, 0x10},
        {PickupSelectorType.Flanger, 0x20},
        {PickupSelectorType.Echo, 0x30},
        {PickupSelectorType.None, 0x40},
    };
    public GuitarAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff,
        byte[] ledIndices, int min, int max, int deadZone, GuitarAxisType type, bool childOfCombined) : base(model,
        input, ledOn,
        ledOff, ledIndices, min, max, deadZone, type is GuitarAxisType.Slider or GuitarAxisType.Whammy, childOfCombined)
    {
        Type = type;
        UpdateDetails();
        this.WhenAnyValue(x => x.Value).Select(GetNamedAxisInfo).ToPropertyEx(this, x => x.NamedAxisInfo);
    }


    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public string NamedAxisInfo { get; } = "";

    public GuitarAxisType Type { get; }

    public bool HasNamedAxis => Type is GuitarAxisType.Slider or GuitarAxisType.Pickup;

    public override bool IsKeyboard => false;

    public override string LedOnLabel
    {
        get
        {
            return Type switch
            {
                GuitarAxisType.Tilt => "Highest Tilt LED Colour",
                GuitarAxisType.Whammy => "Whammy Pressed LED Colour",
                GuitarAxisType.Pickup => "Lowest Position LED Colour",
                GuitarAxisType.Slider => "Lowest Position LED Colour",
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
                GuitarAxisType.Pickup => "Lowest Position LED Colour",
                GuitarAxisType.Slider => "Lowest Position LED Colour",
                _ => ""
            };
        }
    }

    public static PickupSelectorType GetPickupSelectorValue(int val)
    {
        foreach (var (type, range) in PickupSelectorRanges)
        {
            if (val < range << 8)
            {
                return type;
            }
        }

        return PickupSelectorType.None;
    }

    private string GetNamedAxisInfo(int val)
    {
        if (Type is GuitarAxisType.Pickup)
        {
            val += short.MaxValue + 1;
            return EnumToStringConverter.Convert(GetPickupSelectorValue(val));
        }

        if (Type is not GuitarAxisType.Slider || !Gh5NeckInput.Gh5Mappings.ContainsKey(val))
            return ChildOfCombined ? "None" : "Current Frets: None";
        var info = Gh5NeckInput.Gh5Mappings[val];
        var ret = "";
        if (!ChildOfCombined)
        {
            ret = "Current Frets: ";
        }

        if (info.HasFlag(BarButton.Green)) ret += "Green ";
        if (info.HasFlag(BarButton.Red)) ret += "Red ";
        if (info.HasFlag(BarButton.Yellow)) ret += "Yellow ";
        if (info.HasFlag(BarButton.Blue)) ret += "Blue ";
        if (info.HasFlag(BarButton.Orange)) ret += "Orange";
        return ret.Trim();
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedGuitarAxis(Input!.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone, ChildOfCombined);
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override object GetOutputType()
    {
        return Type;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros)
    {
        if (mode == ConfigField.Shared)
            return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros);
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.XboxOne or ConfigField.Xbox360)) return "";
        // The below is a mess... but essentially we have to handle converting the input to its respective output depending on console
        // We have to do some hyper specific stuff for digital to analog here too so its easiest to capture its value once
        var analogOn = 0;
        if (Input is DigitalToAnalog dta)
        {
            analogOn = dta.On;
            // Slider is really a uint8_t, so just cut off the extra bits
            if (Type == GuitarAxisType.Slider) analogOn &= 0xFF;
        }

        switch (mode)
        {
            case ConfigField.Xbox360 when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                // x360 slider is actually a int16_t BUT there is a mechanism to convert the uint8 value to its uint16_t version
                if (analogOn > 0x80)
                    analogOn |= (analogOn - 1) << 8;
                else
                    analogOn |= analogOn << 8;

                return $@"if ({Input.Generate()}) {{
                                  {GenerateOutput(mode)} = {analogOn};
                          }}";
            case ConfigField.Xbox360 when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                // x360 slider is actually a int16_t BUT there is a mechanism to convert the uint8 value to its uint16_t version
                return $@"
                        {GenerateOutput(mode)} = {Input.Generate()};
                        if ({GenerateOutput(mode)} > 0x80) {{
                            {GenerateOutput(mode)} |= ({GenerateOutput(mode)}-1) << 8;
                        }} else {{
                            {GenerateOutput(mode)} |= ({GenerateOutput(mode)}) << 8;
                        }}
                    ";
            case ConfigField.Ps3 or ConfigField.Ps4 when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                return $@"if ({Input.Generate()}) {{
                                  {GenerateOutput(mode)} = {analogOn & 0xFF};
                              }}";
            case ConfigField.Ps3 or ConfigField.Ps4 when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                return $"{GenerateOutput(mode)} = {Input.Generate()} >> 8;";
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
                if (Model.UsingBluetooth())
                {
                    return $@"if ({Input.Generate()}) {{
                                  if (bluetooth) {{
                                     report->tilt_bluetooth = 255;
                                  }} else {{
                                     report->tilt = 0x180;
                                  }}
                              }}";
                }

                return $@"if ({Input.Generate()}) {{
                            report->tilt = 0x180;
                          }}";
            case ConfigField.Ps3
                when Model is
                     {
                         DeviceType: DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar,
                         RhythmType: RhythmType.GuitarHero
                     } &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:
                if (Model.UsingBluetooth())
                {
                    return $@"if (bluetooth) {{
                         report->tilt_bluetooth = {GenerateAssignment(mode, false, false, false)};
                      }} else {{
                         {GenerateOutput(mode)} = {GenerateAssignment(mode, true, false, false)};
                      }}";
                }

                return $@"if ({Input.Generate()}) {{
                            {GenerateOutput(mode)} = {GenerateAssignment(mode, true, false, false)};
                          }}";
            case ConfigField.Ps3
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
                // PS3 rb uses a digital bit, so just map the bit right across and skip the analog conversion
                if (Model.UsingBluetooth())
                {
                    return $@"if (bluetooth) {{
                         report->tilt_bluetooth = 255;
                      }} else {{
                         report->tilt = true;
                      }}";
                }

                return $@"if ({Input.Generate()}) {{
                            report->tilt = true;
                          }}";
            case ConfigField.Ps3
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:
                // PS3 RB expects tilt as a digital bit, so map that here
                // On pc, we use a standard axis because that works better in games like clone hero
                if (Model.UsingBluetooth())
                {
                    return $@"if (bluetooth) {{
                         report->tilt_bluetooth = {GenerateAssignment(mode, false, false, false)};
                      }} else {{
                         {GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, false)} == 0xFF;
                      }}";
                }

                return $@"if ({Input.Generate()}) {{
                            {GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, false)} == 0xFF;
                          }}";
            // Xbox 360 Pickup Selector is actually on one of the triggers.
            case ConfigField.Xbox360
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Pickup && Input is DigitalToAnalog:
                // Was int16_t (axis), needs to become uint8_t (trigger)
                var val = analogOn;
                val = (val >> 8) + 128;
                return $@"if ({Input.Generate()}) {{
                                  {GenerateOutput(mode)} = {val};
                              }}";
            case ConfigField.Xbox360
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Pickup && Input is not DigitalToAnalog:
                return $"{GenerateOutput(mode)} = {GenerateAssignment(mode, false, true, false)};";
            // Xbox One pickup selector ranges from 0 - 64, so we need to map it correctly.
            case ConfigField.XboxOne
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Pickup && Input is DigitalToAnalog:
                return $@"if ({Input.Generate()}) {{
                                  {GenerateOutput(mode)} = {PickupSelectorRangesXB1[GetPickupSelectorValue(analogOn)]};
                              }}";
            case ConfigField.XboxOne
                when Model is {DeviceType: DeviceControllerType.Guitar, RhythmType: RhythmType.RockBand} &&
                     Type == GuitarAxisType.Pickup && Input is not DigitalToAnalog:
                return $"{GenerateOutput(mode)} = (((({GenerateAssignment(mode, false, true, false)}) >> 10) + 1) & 0xF0);";
            default:
                if (Input is DigitalToAnalog)
                    return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros);
                return
                    $"{GenerateOutput(mode)} = {GenerateAssignment(mode, false, false, Type is GuitarAxisType.Whammy)};";
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
            GuitarAxisType.Tilt => "Tilt the guitar down",
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
        return Type is not GuitarAxisType.Slider;
    }
}
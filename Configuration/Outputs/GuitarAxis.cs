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

    public static readonly Dictionary<PickupSelectorType, int> PickupSelectorRangesXb1 = new()
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
                GuitarAxisType.Tilt => Resources.LEDColourActiveTilt,
                GuitarAxisType.Whammy => Resources.LEDColourActiveWhammy,
                GuitarAxisType.Pickup => Resources.LEDColourActivePickup,
                GuitarAxisType.Slider => Resources.LEDColourActiveSlider,
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
                GuitarAxisType.Tilt => Resources.LEDColourInctiveTilt,
                GuitarAxisType.Whammy => Resources.LEDColourInctiveWhammy,
                GuitarAxisType.Pickup => Resources.LEDColourInctivePickup,
                GuitarAxisType.Slider => Resources.LEDColourInctiveSlider,
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

        var ret = "";
        if (!ChildOfCombined)
        {
            ret = Resources.TapBarCurrentFrets;
        }

        if (Type is not GuitarAxisType.Slider || !Gh5NeckInput.Gh5Mappings.ContainsKey(val))
            return ret + Resources.TapBarCurrentFretsNone;
        var info = Gh5NeckInput.Gh5Mappings[val];
        if (info.HasFlag(BarButton.Green)) ret += $"{Resources.TapBarCurrentFretsGreen} ";
        if (info.HasFlag(BarButton.Red)) ret += $"{Resources.TapBarCurrentFretsRed} ";
        if (info.HasFlag(BarButton.Yellow)) ret += $"{Resources.TapBarCurrentFretsYellow} ";
        if (info.HasFlag(BarButton.Blue)) ret += $"{Resources.TapBarCurrentFretsBlue} ";
        if (info.HasFlag(BarButton.Orange)) ret += $"{Resources.TapBarCurrentFretsOrange} ";
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

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros)
    {
        if (mode == ConfigField.Shared)
            return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros);
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 or ConfigField.Xbox360 or ConfigField.XboxOne
            or ConfigField.Universal)) return "";
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
            case ConfigField.XboxOne when Model.DeviceControllerType is DeviceControllerType.LiveGuitar:
                return "";
            case ConfigField.XboxOne when Type is GuitarAxisType.Tilt && Input is DigitalToAnalog:
                return $$"""
                         if ({{Input.Generate()}}) {
                             {{GenerateOutput(mode)}} = {{analogOn}};
                         }
                         """;
            case ConfigField.XboxOne when Type is GuitarAxisType.Tilt :
                // XB1 tilt is similar enough to ps3 that we can just use it
                return $"{GenerateOutput(mode)} = {GenerateAssignment(GenerateOutput(mode), ConfigField.Ps3, false, false, false)};";
            case ConfigField.Xbox360 when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                // x360 slider is actually a int16_t BUT there is a mechanism to convert the uint8 value to its uint16_t version
                if (analogOn > 0x80)
                    analogOn |= (analogOn - 1) << 8;
                else
                    analogOn |= analogOn << 8;

                return $$"""
                         if ({{Input.Generate()}}) {
                             {{GenerateOutput(mode)}} = {{analogOn}};
                         }
                         """;
            case ConfigField.Xbox360 when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                // x360 slider is actually a int16_t BUT there is a mechanism to convert the uint8 value to its uint16_t version
                return $$"""
                         {{GenerateOutput(mode)}} = {{Input.Generate()}};
                         if ({{GenerateOutput(mode)}} > 0x80) {
                             {{GenerateOutput(mode)}} |= ({{GenerateOutput(mode)}}-1) << 8;
                         } else {
                             {{GenerateOutput(mode)}} |= ({{GenerateOutput(mode)}}) << 8;
                         }
                         """;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 when Type == GuitarAxisType.Slider && Input is DigitalToAnalog:
                return $$"""
                         if ({{Input.Generate()}}) {
                                                           {{GenerateOutput(mode)}} = {{analogOn & 0xFF}};
                                                       }
                         """;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.Ps4 when Type == GuitarAxisType.Slider && Input is not DigitalToAnalog:
                return $"{GenerateOutput(mode)} = {Input.Generate()} >> 8;";
            // Xb1 is RB only, so no slider
            case ConfigField.XboxOne when Type == GuitarAxisType.Slider:
                return "";

            // PS3 GH expects tilt on the tilt axis
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is
                     {
                         DeviceControllerType: DeviceControllerType.GuitarHeroGuitar
                     } &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
            {
                return $$"""
                         if ({{Input.Generate()}}) {
                             report->tilt = 0x180;
                         }
                         """;
            }
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is
                     {
                         DeviceControllerType: DeviceControllerType.GuitarHeroGuitar
                     } &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:
                return $$"""
                         if ({{Input.Generate()}}) {
                             {{GenerateOutput(mode)}} = {{GenerateAssignment(GenerateOutput(mode), mode, true, false, false)}};
                         }
                         """;
            // PS3 GHL expects tilt on accelerometer AND right stick x
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is
                     {
                         DeviceControllerType: DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.LiveGuitar
                     } &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
            {
                return $$"""
                         if ({{Input.Generate()}}) {
                             report->tilt = 0x180;
                             report->tilt2 = 0xFF;
                         }
                         """;
            }
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is
                     {
                         DeviceControllerType: DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.LiveGuitar
                     } &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:

                // GHL expects right stick x to go to 0xFF or 0x00 to signify tilt being active
                return $$"""
                         if ({{Input.Generate()}}) {
                             {{GenerateOutput(mode)}} = {{GenerateAssignment(GenerateOutput(mode), mode, true, false, false)}};
                             uint8_t tilt_test = {{GenerateAssignment(GenerateOutput(mode), mode, false, false, false)}};
                             if (tilt_test > 0xF0) {
                                 report->tilt2 = 0xFF;
                             }
                             if (tilt_test < 0x10) {
                                 report->tilt2 = 0x00;
                             }
                         }
                         """;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Tilt && Input is DigitalToAnalog:
                // PS3 rb uses a digital bit, so just map the bit right across and skip the analog conversion
                return $$"""
                         if ({{Input.Generate()}}) {
                             report->tiltDigital = true;
                         }
                         """;
            case ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Tilt && Input is not DigitalToAnalog:
                // PS3 RB expects tilt as a digital bit, so map that here. Still map a ps3 variant of the tilt though
                return $$"""
                         if ({{Input.Generate()}}) {
                             report->tiltDigital = {{GenerateAssignment(GenerateOutput(mode), mode, false, false, false)}} == 0xFF;
                         }
                         """;
            // Xbox 360 Pickup Selector is actually on one of the triggers.
            case ConfigField.Xbox360
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Pickup && Input is DigitalToAnalog:
                // Was int16_t (axis), needs to become uint8_t (trigger)
                var val = analogOn;
                val = (val >> 8) + 128;
                return $$"""
                         if ({{Input.Generate()}}) {
                             {{GenerateOutput(mode)}} = {{val}};
                         }
                         """;
            case ConfigField.Xbox360
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Pickup && Input is not DigitalToAnalog:
                return $"{GenerateOutput(mode)} = {GenerateAssignment(GenerateOutput(mode), mode, false, true, false)};";
            // Xbox One pickup selector ranges from 0 - 64, so we need to map it correctly.
            case ConfigField.XboxOne
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Pickup && Input is DigitalToAnalog:
                return $$"""
                         if ({{Input.Generate()}}) {
                                                           {{GenerateOutput(mode)}} = {{PickupSelectorRangesXb1[GetPickupSelectorValue(analogOn)]}};
                                                       }
                         """;
            case ConfigField.XboxOne
                when Model is {DeviceControllerType: DeviceControllerType.RockBandGuitar} &&
                     Type == GuitarAxisType.Pickup && Input is not DigitalToAnalog:
                return
                    $"{GenerateOutput(mode)} = (((({GenerateAssignment(GenerateOutput(mode), mode, false, true, false)}) >> 10) + 1) & 0xF0);";
            default:
                if (Input is DigitalToAnalog)
                    return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros);
                return
                    $"{GenerateOutput(mode)} = {GenerateAssignment(GenerateOutput(mode), mode, false, false, Type is GuitarAxisType.Whammy)};";
        }
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return EnumToStringConverter.Convert(Type);
    }

    protected override string MinCalibrationText()
    {
        return Type switch
        {
            GuitarAxisType.Tilt => Resources.AxisMinCalibrationTilt,
            GuitarAxisType.Whammy => Resources.AxisMinCalibrationWhammy,
            _ => ""
        };
    }

    protected override string MaxCalibrationText()
    {
        return Type switch
        {
            GuitarAxisType.Tilt => Resources.AxisMaxCalibrationTilt,
            GuitarAxisType.Whammy => Resources.AxisMaxCalibrationWhammy,
            _ => ""
        };
    }

    protected override bool SupportsCalibration()
    {
        return Type is not GuitarAxisType.Slider;
    }
}
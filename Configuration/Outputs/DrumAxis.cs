using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class DrumAxis : OutputAxis
{
    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsXbox360 = new()
    {
        {DrumAxisType.Green, StandardButtonType.A},
        {DrumAxisType.Red, StandardButtonType.B},
        {DrumAxisType.Blue, StandardButtonType.X},
        {DrumAxisType.Yellow, StandardButtonType.Y},
        {DrumAxisType.GreenCymbal, StandardButtonType.A},
        {DrumAxisType.BlueCymbal, StandardButtonType.X},
        {DrumAxisType.YellowCymbal, StandardButtonType.Y},
        {DrumAxisType.Orange, StandardButtonType.RightShoulder},
        {DrumAxisType.Kick, StandardButtonType.LeftShoulder},
        {DrumAxisType.Kick2, StandardButtonType.LeftThumbClick}
    };

    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsXboxOne = new()
    {
        {DrumAxisType.Green, StandardButtonType.A},
        {DrumAxisType.Red, StandardButtonType.B},
        {DrumAxisType.Kick, StandardButtonType.LeftShoulder},
        {DrumAxisType.Kick2, StandardButtonType.RightShoulder}
    };

    private static readonly Dictionary<DrumAxisType, StandardButtonType> ButtonsPs3 = new()
    {
        {DrumAxisType.Green, StandardButtonType.A},
        {DrumAxisType.Red, StandardButtonType.B},
        {DrumAxisType.Blue, StandardButtonType.X},
        {DrumAxisType.Yellow, StandardButtonType.Y},
        {DrumAxisType.GreenCymbal, StandardButtonType.A},
        {DrumAxisType.BlueCymbal, StandardButtonType.X},
        {DrumAxisType.YellowCymbal, StandardButtonType.Y},
        {DrumAxisType.Kick, StandardButtonType.LeftShoulder},
        {DrumAxisType.Orange, StandardButtonType.RightShoulder},
        {DrumAxisType.Kick2, StandardButtonType.RightShoulder}
    };

    private static readonly Dictionary<DrumAxisType, string> AxisMappings = new()
    {
        {DrumAxisType.Green, "report->greenVelocity"},
        {DrumAxisType.Red, "report->redVelocity"},
        {DrumAxisType.Yellow, "report->yellowVelocity"},
        {DrumAxisType.Blue, "report->blueVelocity"},
        {DrumAxisType.Orange, "report->orangeVelocity"},
        {DrumAxisType.Kick, "report->kickVelocity"}
    };

    private readonly StandardButtonType BlueCymbalFlag = StandardButtonType.DpadDown;

    private readonly StandardButtonType Ps3CymbalFlag = StandardButtonType.RightThumbClick;
    private readonly StandardButtonType Ps3PadFlag = StandardButtonType.LeftThumbClick;
    private readonly StandardButtonType Xbox360CymbalFlag = StandardButtonType.LeftShoulder;
    private readonly StandardButtonType Xbox360PadFlag = StandardButtonType.RightThumbClick;

    private readonly StandardButtonType YellowCymbalFlag = StandardButtonType.DpadUp;
    private int _debounce;

    private int _threshold;

    public DrumAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max,
        int deadZone, int threshold, int debounce, DrumAxisType type) : base(model, input, ledOn, ledOff, ledIndices,
        min, max, deadZone,
        "Drum" + type, true)
    {
        Type = type;
        Threshold = threshold;
        Debounce = debounce;
    }

    public DrumAxisType Type { get; }

    public override bool Valid => true;

    public override bool IsCombined => false;

    public override string LedOnLabel => "Drum Hit LED Colour";
    public override string LedOffLabel => "Drum not Hit LED Colour";

    public override bool IsKeyboard => false;
    public override bool IsController => true;

    public int Threshold
    {
        get => _threshold;
        set => this.RaiseAndSetIfChanged(ref _threshold, value);
    }

    public int Debounce
    {
        get => _debounce;
        set => this.RaiseAndSetIfChanged(ref _debounce, value);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return Name;
    }


    public override string GenerateOutput(ConfigField mode)
    {
        return AxisMappings.ContainsKey(Type) ? AxisMappings[Type] : "";
    }


    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (mode is not (ConfigField.Ps3 or ConfigField.XboxOne or ConfigField.Xbox360 or ConfigField.Ps3Mask
            or ConfigField.Xbox360Mask or ConfigField.XboxOneMask)) return "";
        if (string.IsNullOrEmpty(GenerateOutput(mode))) return "";

        var ifStatement = string.Join(" && ", debounceIndex.Select(x => $"debounce[{x}]"));
        var decrement = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]--;");
        var reset = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]={Debounce + 1};");
        var padFlag = Ps3PadFlag;
        var cymbalFlag = Ps3CymbalFlag;
        var outputButtons = "";
        switch (mode)
        {
            case ConfigField.Xbox360:
                padFlag = Xbox360PadFlag;
                cymbalFlag = Xbox360CymbalFlag;
                if (ButtonsXbox360.ContainsKey(Type)) outputButtons += $"\n{GetReportField(ButtonsXbox360[Type])} = 1";
                break;
            case ConfigField.XboxOne:
                if (ButtonsXboxOne.ContainsKey(Type)) outputButtons += $"\n{GetReportField(ButtonsXboxOne[Type])} = 1";
                break;
            case ConfigField.Ps3:
                if (ButtonsPs3.ContainsKey(Type)) outputButtons += $"\n{GetReportField(ButtonsPs3[Type])} = 1";
                break;
            case ConfigField.Xbox360Mask:
                padFlag = Xbox360PadFlag;
                cymbalFlag = Xbox360CymbalFlag;
                if (ButtonsXbox360.ContainsKey(Type))
                    outputButtons += GetMaskField(GetReportField(ButtonsXbox360[Type]), mode);
                break;
            case ConfigField.XboxOneMask:
                if (ButtonsXboxOne.ContainsKey(Type))
                    outputButtons += GetMaskField(GetReportField(ButtonsXboxOne[Type]), mode);
                break;
            case ConfigField.Ps3Mask:
                if (ButtonsPs3.ContainsKey(Type)) outputButtons += GetMaskField(GetReportField(ButtonsPs3[Type]), mode);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        if (Model.RhythmType == RhythmType.RockBand && mode != ConfigField.XboxOne)
            switch (Type)
            {
                case DrumAxisType.YellowCymbal:
                    if (mode is ConfigField.Ps3Mask or ConfigField.Xbox360Mask)
                    {
                        outputButtons += GetMaskField(GetReportField(YellowCymbalFlag), mode);
                        outputButtons += GetMaskField(GetReportField(cymbalFlag), mode);
                    }
                    else
                    {
                        outputButtons += $"\n{GetReportField(YellowCymbalFlag)} = 1";
                        outputButtons += $"\n{GetReportField(cymbalFlag)} = 1";
                    }

                    break;
                case DrumAxisType.BlueCymbal:

                    if (mode is ConfigField.Ps3Mask or ConfigField.Xbox360Mask)
                    {
                        outputButtons += GetMaskField(GetReportField(BlueCymbalFlag), mode);
                        outputButtons += GetMaskField(GetReportField(cymbalFlag), mode);
                    }
                    else
                    {
                        outputButtons += $"\n{GetReportField(BlueCymbalFlag)} = 1";
                        outputButtons += $"\n{GetReportField(cymbalFlag)} = 1";
                    }

                    break;
                case DrumAxisType.GreenCymbal:

                    if (mode is ConfigField.Ps3Mask or ConfigField.Xbox360Mask)
                    {
                        outputButtons += GetMaskField(GetReportField(cymbalFlag), mode);
                    }
                    else
                    {
                        outputButtons += $"\n{GetReportField(cymbalFlag)} = 1";
                    }

                    break;
                case DrumAxisType.Green:
                case DrumAxisType.Red:
                case DrumAxisType.Yellow:
                case DrumAxisType.Blue:

                    if (mode is ConfigField.Ps3Mask or ConfigField.Xbox360Mask)
                    {
                        outputButtons += GetMaskField(GetReportField(padFlag), mode);
                    }
                    else
                    {
                        outputButtons += $"\n{GetReportField(padFlag)} = 1";
                    }

                    break;
            }

        if (mode is ConfigField.Ps3Mask or ConfigField.Xbox360Mask or ConfigField.XboxOneMask)
        {
            return outputButtons + GetMaskField(GenerateOutput(mode), mode);
        }

        var assignedVal = "val_real";
        var valType = "uint16_t";
        // Xbox one uses 4 bit velocities
        if (mode == ConfigField.XboxOne)
        {
            valType = "uint8_t";
            assignedVal = "val_real >> 12";
        }
        // Xbox 360 GH and PS3 use uint16_t velocities
        else if (Model.RhythmType == RhythmType.GuitarHero || mode != ConfigField.Xbox360)
        {
            assignedVal = "val_real >> 8";
        }
        // And then 360 RB use inverted int16_t values, though the first bit is specified based on the type
        else
        {
            valType = "int16_t";
            switch (Type)
            {
                // Stuff mapped to the y axis is inverted
                case DrumAxisType.GreenCymbal:
                case DrumAxisType.Green:
                case DrumAxisType.Yellow:
                case DrumAxisType.YellowCymbal:
                    assignedVal = "-(0x7fff - (val >> 1))";
                    break;
                case DrumAxisType.Red:
                case DrumAxisType.Blue:
                case DrumAxisType.BlueCymbal:
                    assignedVal = "(0x7fff - (val >> 1))";
                    break;
            }
        }

        var led = CalculateLeds(mode);
        // Drum axis' are weird. Translate the value to a uint16_t like any axis, do tests against threshold for hits
        // and then convert them to their expected output format, before writing to the output report.
        return $@"
{{
    uint16_t val_real = {GenerateAssignment(mode, false, false, false)};
    if (val_real) {{
        if (val_real > {Threshold}) {{
            {reset}
        }}
        {valType} val = {assignedVal};
        {GenerateOutput(mode)} = val;
        {led}
    }}
    if ({ifStatement}) {{
        {decrement} 
        {outputButtons}
    }}
}}";
    }

    protected override string MinCalibrationText()
    {
        return "Do nothing";
    }

    protected override string MaxCalibrationText()
    {
        return "Hit the drum";
    }


    public override void UpdateBindings()
    {
    }

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"{rhythmType}/{Name}.png";
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDrumAxis(Input?.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone, Threshold, Debounce);
    }
}
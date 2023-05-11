using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class DrumAxis : OutputAxis
{
    private const StandardButtonType BlueCymbalFlag = StandardButtonType.DpadDown;
    private const StandardButtonType YellowCymbalFlag = StandardButtonType.DpadUp;

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
        {DrumAxisType.GreenCymbal, "report->greenVelocity"},
        {DrumAxisType.YellowCymbal, "report->yellowVelocity"},
        {DrumAxisType.BlueCymbal, "report->blueVelocity"},
        {DrumAxisType.Kick, "report->kickVelocity"},
        {DrumAxisType.Kick2, "report->kickVelocity"}
    };

    private static readonly Dictionary<DrumAxisType, string> AxisMappingsXb1 = new()
    {
        {DrumAxisType.Green, "report->greenVelocity"},
        {DrumAxisType.Red, "report->redVelocity"},
        {DrumAxisType.Yellow, "report->yellowVelocity"},
        {DrumAxisType.Blue, "report->blueVelocity"},
        {DrumAxisType.Orange, "report->orangeVelocity"},
        {DrumAxisType.GreenCymbal, "report->greenCymbalVelocity"},
        {DrumAxisType.YellowCymbal, "report->yellowCymbalVelocity"},
        {DrumAxisType.BlueCymbal, "report->blueCymbalVelocity"},
        {DrumAxisType.Kick, "report->kickVelocity"},
        {DrumAxisType.Kick2, "report->kickVelocity"}
    };

    private bool _calibrating;

    public DrumAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max,
        int deadZone, int threshold, int debounce, DrumAxisType type, bool childOfCombined) : base(model, input, ledOn,
        ledOff, ledIndices,
        min, max, deadZone, true, childOfCombined)
    {
        Type = type;
        Threshold = threshold;
        Debounce = debounce;
        UpdateDetails();
        this
            .WhenAnyValue(x => x.Threshold)
            .Select(ComputeDrumMargin).ToPropertyEx(this, x => x.ComputedDrumMargin);
    }

    public DrumAxisType Type { get; }

    public override bool Valid => true;

    public override bool IsCombined => false;

    public override string LedOnLabel => "Drum Hit LED Colour";
    public override string LedOffLabel => "Drum not Hit LED Colour";

    public override bool IsKeyboard => false;

    [Reactive] public int Threshold { get; set; }

    [Reactive] public int Debounce { get; set; }
    [ObservableAsProperty] public Thickness ComputedDrumMargin { get; }


    public override string? CalibrationText => _calibrating ? "Hit the drum" : null;

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override object GetOutputType()
    {
        return Type;
    }


    private Thickness ComputeDrumMargin(int threshold)
    {
        var val = (float) threshold / ushort.MaxValue * ProgressWidth;
        return new Thickness(val - 5, 0, val + 5, 0);
    }


    public override string GenerateOutput(ConfigField mode)
    {
        if (mode == ConfigField.XboxOne) return AxisMappingsXb1.TryGetValue(Type, out var value) ? value : "";

        return AxisMappings.TryGetValue(Type, out var mapping) ? mapping : "";
    }

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }


    public override string Generate(ConfigField mode, List<int> debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce)
    {
        if (mode == ConfigField.Shared)
            return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce);
        if (mode is not (ConfigField.Ps3 or ConfigField.XboxOne or ConfigField.Xbox360)) return "";
        if (string.IsNullOrEmpty(GenerateOutput(mode))) return "";
        var debounce = Debounce + 1;
        if (!Model.IsAdvancedMode) debounce = Model.Debounce + 1;

        var ifStatement = string.Join(" && ", debounceIndex.Select(x => $"debounce[{x}]"));
        
        var reset = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]={debounce};");
        var outputButtons = "";
        switch (mode)
        {
            case ConfigField.Xbox360:
                if (ButtonsXbox360.TryGetValue(Type, out var value))
                    outputButtons += $"\n{GetReportField(value)} = true;";
                break;
            case ConfigField.XboxOne:
                if (ButtonsXboxOne.TryGetValue(Type, out var value1))
                    outputButtons += $"\n{GetReportField(value1)} = true;";
                break;
            case ConfigField.Ps3:
                if (ButtonsPs3.TryGetValue(Type, out var value2))
                    outputButtons += $"\n{GetReportField(value2)} = true;";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        if (Model.RhythmType == RhythmType.RockBand && Type is DrumAxisType.Kick or DrumAxisType.Kick2)
        {
            return $@"if ({ifStatement}) {{
                {outputButtons}
            }}";
        }

        if (Model.RhythmType == RhythmType.RockBand && mode != ConfigField.XboxOne)
            switch (Type)
            {
                case DrumAxisType.YellowCymbal:
                    outputButtons += $"\n{GetReportField(YellowCymbalFlag)} = true;";
                    outputButtons += $"\n{GetReportField("cymbalFlag")} = true;";

                    break;
                case DrumAxisType.BlueCymbal:
                    outputButtons += $"\n{GetReportField(BlueCymbalFlag)} = true;";
                    outputButtons += $"\n{GetReportField("cymbalFlag")} = true;";

                    break;
                case DrumAxisType.GreenCymbal:
                    outputButtons += $"\n{GetReportField("cymbalFlag")} = true;";
                    break;
                case DrumAxisType.Green:
                case DrumAxisType.Red:
                case DrumAxisType.Yellow:
                case DrumAxisType.Blue:

                    outputButtons += $"\n{GetReportField("padFlag")} = true;";

                    break;
            }

        // If someone specified a digital input, then we need to take the value they have specified and convert it to the target consoles expected output
        var dtaVal = 0;
        if (Input is DigitalToAnalog dta) dtaVal = dta.On;

        var assignedVal = "val_real";
        // Xbox one uses 4 bit velocities
        if (mode == ConfigField.XboxOne)
        {
            assignedVal = "val_real >> 12";
            dtaVal >>= 12;
        }
        // PS3 uses 8 bit velocities
        else if (mode == ConfigField.Ps3)
        {
            assignedVal = "val_real >> 8";
            dtaVal >>= 8;
        }
        // Xbox 360 GH use uint8_t velocities
        else if (Model.RhythmType == RhythmType.GuitarHero)
        {
            assignedVal = "val_real >> 8";
            dtaVal >>= 8;
        }
        // And then 360 RB use inverted int16_t values, though the first bit is specified based on the type
        else
        {
            switch (Type)
            {
                // Stuff mapped to the y axis is inverted
                case DrumAxisType.GreenCymbal:
                case DrumAxisType.Green:
                case DrumAxisType.Yellow:
                case DrumAxisType.YellowCymbal:
                    assignedVal = "-(0x7fff - (val >> 1))";
                    dtaVal = -(0x7fff - (dtaVal >> 1));
                    break;
                case DrumAxisType.Red:
                case DrumAxisType.Blue:
                case DrumAxisType.BlueCymbal:
                    assignedVal = "(0x7fff - (val >> 1))";
                    dtaVal = 0x7fff - (dtaVal >> 1);
                    break;
            }
        }


        var rfExtra = "";
        // If someone has mapped digital inputs to the drums, then we can shortcut a bunch of the tests, and just need to use the calculated value from above
        if (Input is DigitalToAnalog dta2)
        {
            // For bluetooth and RF, stuff the cymbal data into some unused bytes for rf reasons
            if (mode == ConfigField.Ps3 &&
                Type is DrumAxisType.BlueCymbal or DrumAxisType.GreenCymbal or DrumAxisType.YellowCymbal)
                rfExtra = $@"
                if (rf_or_bluetooth) {{
                    {GenerateOutput(ConfigField.XboxOne)} = {dta2.On >> 8};
                }}  
            ";

            return $@"
            {{
                if ({Input.Generate()}) {{
                    {reset}
                    {GenerateOutput(mode)} = {dtaVal};
                    {rfExtra}
                }}
                if ({ifStatement}) {{
                    {outputButtons}
                }}
            }}";
        }

        // For bluetooth and RF, stuff the cymbal data into some unused bytes for rf reasons
        if (mode == ConfigField.Ps3 &&
            Type is DrumAxisType.BlueCymbal or DrumAxisType.GreenCymbal or DrumAxisType.YellowCymbal)
            rfExtra = $@"
                if (rf_or_bluetooth) {{
                    {GenerateOutput(ConfigField.XboxOne)} = val_real >> 8;
                }}  
            ";
        var generated = Input.IsUint ? Input.Generate() : $"(({Input.Generate()}) + INT16_MAX)";
        // Drum axis' are weird. Translate the value to a uint16_t like any axis, do tests against threshold for hits
        // and then convert them to their expected output format, before writing to the output report.
        return $@"
        {{
            uint16_t val_real = {generated};
            if (val_real) {{
                if (val_real > {Threshold}) {{
                    {reset}
                }}
                {GenerateOutput(mode)} = {assignedVal};
                {rfExtra}
            }}
            if ({ifStatement}) {{
                {outputButtons}
            }}
        }}";
    }

    [RelayCommand]
    private void CalibrateDrums()
    {
        _calibrating = !_calibrating;
        this.RaisePropertyChanged(nameof(CalibrationText));
    }

    public override void ApplyCalibration(int rawValue)
    {
        if (!Input.IsUint) rawValue += short.MaxValue;
        if (_calibrating) Threshold = Math.Max(rawValue, Threshold);
    }

    [RelayCommand]
    public void ResetThreshold()
    {
        Threshold = 0;
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

    public override SerializedOutput Serialize()
    {
        return new SerializedDrumAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone, Threshold, Debounce, ChildOfCombined);
    }
}
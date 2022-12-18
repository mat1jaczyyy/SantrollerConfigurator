using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class DrumAxis : OutputAxis
{
    private int xboxCymbal = 5;
    private int xboxTom = 9;
    private int ps3Tom = 10;

    private int ps3Cymbal = 11;

    //For xbox, this is in buttons
    //For ps3, this is in hat
    private int yellowCymbal = 1;
    private int blueCymbal = 2;

    public static Dictionary<DrumAxisType, int> ButtonsXbox = new()
    {
        {DrumAxisType.Green, 1},
        {DrumAxisType.Red, 2},
        {DrumAxisType.Blue, 3},
        {DrumAxisType.Yellow, 4},
        {DrumAxisType.GreenCymbal, 1},
        {DrumAxisType.BlueCymbal, 3},
        {DrumAxisType.YellowCymbal, 4},
        {DrumAxisType.Kick, 5},
        {DrumAxisType.Orange, 6},
        {DrumAxisType.Kick2, 8},
    };

    public static Dictionary<DrumAxisType, int> ButtonsPs3 = new()
    {
        {DrumAxisType.Blue, 1},
        {DrumAxisType.Green, 2},
        {DrumAxisType.Red, 3},
        {DrumAxisType.Yellow, 4},
        {DrumAxisType.BlueCymbal, 1},
        {DrumAxisType.GreenCymbal, 2},
        {DrumAxisType.YellowCymbal, 4},
        {DrumAxisType.Kick, 5},
        {DrumAxisType.Orange, 6},
        {DrumAxisType.Kick2, 6},
    };

    //int16, yellow and green need a - sign in front, so you would want to shift >> 1 and then prepend the - sign
    private static Dictionary<DrumAxisType, string> AxisMappingsRbXbox = new()
    {
        {DrumAxisType.Red, "report->l_x"},
        {DrumAxisType.Yellow, "report->l_y"},
        {DrumAxisType.Blue, "report->r_x"},
        {DrumAxisType.Green, "report->r_y"},
    };

    private static Dictionary<DrumAxisType, string> AxisMappingsGhXbox = new()
    {
        {DrumAxisType.Green, "reportGhDrum->greenVelocity"},
        {DrumAxisType.Red, "reportGhDrum->redVelocity"},
        {DrumAxisType.Yellow, "reportGhDrum->yellowVelocity"},
        {DrumAxisType.Blue, "reportGhDrum->blueVelocity"},
        {DrumAxisType.Orange, "reportGhDrum->orangeVelocity"},
        {DrumAxisType.Kick, "reportGhDrum->kickVelocity"},
    };

    private static Dictionary<DrumAxisType, string> AxisMappingsPs3 = new Dictionary<DrumAxisType, string>()
    {
        {DrumAxisType.Yellow, "report->axis[4]"},
        {DrumAxisType.Red, "report->axis[5]"},
        {DrumAxisType.Green, "report->axis[6]"},
        {DrumAxisType.Blue, "report->axis[7]"},
        {DrumAxisType.Kick, "report->axis[8]"},
        {DrumAxisType.Orange, "report->axis[9]"},
        {DrumAxisType.Kick2, "report->axis[9]"},
    };

    public DrumAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max,
        int deadZone, int threshold, int debounce, DrumAxisType type) : base(model, input, ledOn, ledOff, ledIndices,
        min, max, deadZone,
        "Drum"+type, (s) => true)
    {
        Type = type;
        Threshold = threshold;
        Debounce = debounce;
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return Name;
    }

    public DrumAxisType Type { get; }

    public override bool Valid => true;


    public override string GenerateOutput(bool xbox, bool useReal)
    {
        if (xbox)
        {
            switch (Model.RhythmType)
            {
                case RhythmType.GuitarHero when AxisMappingsGhXbox.ContainsKey(Type):
                    return AxisMappingsGhXbox[Type];
                case RhythmType.RockBand when AxisMappingsRbXbox.ContainsKey(Type):
                    return AxisMappingsRbXbox[Type];
            }
        }
        else if (AxisMappingsPs3.ContainsKey(Type))
        {
            return AxisMappingsPs3[Type];
        }

        return "";
    }


    public override string Generate(bool xbox, bool shared, List<int> debounceIndex, bool combined, string extra)
    {
        if (shared || string.IsNullOrEmpty(GenerateOutput(xbox, false)))
        {
            return "";
        }

        var ifStatement = string.Join(" && ", debounceIndex.Select(x => $"debounce[{x}]"));
        var decrement = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]--;");
        var reset = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]={Debounce + 1};");
        var buttons = 0;
        var hats = 0;
        var tom = ps3Tom;
        var cymbal = ps3Cymbal;
        if (xbox)
        {
            tom = xboxTom;
            cymbal = xboxCymbal;
            if (!ButtonsXbox.ContainsKey(Type))
            {
                return "";
            }

            buttons |= ButtonsXbox[Type];
            switch (Type)
            {
                case DrumAxisType.YellowCymbal:
                    buttons |= 1 << yellowCymbal;
                    break;
                case DrumAxisType.BlueCymbal:
                    buttons |= 1 << blueCymbal;
                    break;
            }
        }
        else
        {
            if (!ButtonsPs3.ContainsKey(Type))
            {
                return "";
            }

            buttons |= 1 << ButtonsPs3[Type];
            switch (Type)
            {
                case DrumAxisType.YellowCymbal:
                    hats |= 1 << yellowCymbal;
                    break;
                case DrumAxisType.BlueCymbal:
                    hats |= 1 << blueCymbal;
                    break;
            }
        }

        if (Model.RhythmType == RhythmType.RockBand)
        {
            switch (Type)
            {
                case DrumAxisType.YellowCymbal:
                case DrumAxisType.BlueCymbal:
                case DrumAxisType.GreenCymbal:
                    buttons |= cymbal;
                    break;
                case DrumAxisType.Green:
                case DrumAxisType.Red:
                case DrumAxisType.Yellow:
                case DrumAxisType.Blue:
                    buttons |= tom;
                    break;
            }
        }

        var outputButtons = buttons > 0 ? $" report->buttons |= {buttons};" : "";
        var outputHats = hats > 0 ? $" report->hat |= {hats};" : "";
        var assignedVal = "val_real";
        var valType = "uint16_t";
        if (Model.RhythmType == RhythmType.GuitarHero || !xbox)
        {
            assignedVal = $"val_real >> 8";
        }
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
                    assignedVal = $"-(0x7fff - (val >> 1))";
                    break;
                case DrumAxisType.Red:
                case DrumAxisType.Blue:
                case DrumAxisType.BlueCymbal:
                    assignedVal = $"(0x7fff - (val >> 1))";
                    break;
            }
        }
        var led = CalculateLeds(xbox);
        // Drum axis' are weird. Translate the value to a uint16_t like any axis, do tests against threshold for hits
        // and then convert them to their expected output format, before writing to the output report.
        return $@"
{{
    uint16_t val_real = {GenerateAssignment(xbox, false)};
    if (val_real) {{
        if (val_real > {Threshold}) {{
            {reset}
        }}
        {valType} val = {assignedVal};
        {GenerateOutput(xbox, false)} = val;
        {led}
    }}
    if ({ifStatement}) {{
        {decrement} 
        {outputButtons}
        {outputHats}
    }}
}}";
    }

    public override bool IsCombined => false;

    protected override string MinCalibrationText()
    {
        return "Do nothing";
    }

    protected override string MaxCalibrationText()
    {
        return "Hit the drum";
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;
    public override void UpdateBindings()
    {
    }

    private int _threshold;
    private int _debounce;

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

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDrumAxis(Input?.Serialise(), Type, LedOn, LedOff, LedIndices, Min, Max,
            DeadZone, Threshold, Debounce);
    }
}
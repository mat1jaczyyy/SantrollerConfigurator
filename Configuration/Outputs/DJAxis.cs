using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public partial class DjAxis : OutputAxis
{
    public DjAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max,
        int deadZone, DjAxisType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, min, max,
        deadZone,
        false, childOfCombined)
    {
        Multiplier = 0;
        Type = type;
        UpdateDetails();
    }

    public DjAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int multiplier,
        DjAxisType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, 0, 0,
        0,
        false, childOfCombined)
    {
        if (type == DjAxisType.Crossfader)
        {
            Invert = multiplier == -1;
        }
        else
        {
            Multiplier = multiplier;
        }

        Type = type;
        UpdateDetails();
    }

    [Reactive] public int Multiplier { get; set; }

    [Reactive] public bool Invert { get; set; }

    protected override int Calculate(
        (bool enabled, int value, int min, int max, int deadZone, bool trigger, DeviceControllerType
            deviceControllerType) values)
    {
        return Type switch
        {
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity when Input.IsUint => (values.value -
                short.MaxValue) * Multiplier,
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity => values.value * Multiplier,

            DjAxisType.EffectsKnob when Input.IsUint => (values.value - short.MaxValue) * (Invert ? -1 : 1),
            DjAxisType.EffectsKnob => values.value * (Invert ? -1 : 1),
            _ => base.Calculate(values)
        };
    }

    public DjAxisType Type { get; }

    public override bool IsKeyboard => false;

    public override string LedOnLabel
    {
        get
        {
            return Type switch
            {
                DjAxisType.Crossfader => Resources.LedColourActiveAxisX,
                DjAxisType.EffectsKnob => Resources.LedColourActiveAxisX,
                DjAxisType.LeftTableVelocity => Resources.LedColourActiveDjVelocity,
                DjAxisType.RightTableVelocity => Resources.LedColourActiveDjVelocity,
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
                DjAxisType.Crossfader => Resources.LedColourInactiveAxisX,
                DjAxisType.EffectsKnob => Resources.LedColourInactiveAxisX,
                DjAxisType.LeftTableVelocity => Resources.LedColourInactiveDjVelocity,
                DjAxisType.RightTableVelocity => Resources.LedColourInactiveDjVelocity,
                _ => ""
            };
        }
    }

    public bool IsVelocity => Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity;

    public bool SupportsMinMax => !IsDigitalToAnalog && !IsVelocity && !IsEffectsKnob;

    public bool IsFader => Type is DjAxisType.Crossfader;
    public bool IsEffectsKnob => Type is DjAxisType.EffectsKnob;

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    public override string GetName(DeviceControllerType deviceControllerType, LegendType legendType,
        bool swapSwitchFaceButtons)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override Enum GetOutputType()
    {
        return Type;
    }

    public override SerializedOutput Serialize()
    {
        if (IsVelocity)
        {
            return new SerializedDjAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Multiplier,
                ChildOfCombined);
        }

        if (IsEffectsKnob)
        {
            return new SerializedDjAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Invert ? -1 : 1,
                ChildOfCombined);
        }

        return new SerializedDjAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max, DeadZone,
            ChildOfCombined);
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros)
    {
        if (mode == ConfigField.Shared)
            return base.Generate(mode, debounceIndex, extra, combinedExtra, combinedDebounce, macros);
        if (mode is not (ConfigField.Ps3 or ConfigField.Ps3WithoutCapture or ConfigField.XboxOne or ConfigField.Xbox360 or ConfigField.Universal)) return "";

        // The crossfader and effects knob on ps3 controllers are shoved into the accelerometer data
        var accelerometer = mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture && Type is DjAxisType.Crossfader or DjAxisType.EffectsKnob;
        // PS3 needs uint, xb360 needs int
        // So convert to the right method for that console, and then shift for ps3
        var generated = $"({Input.Generate()})";
        var generatedPs3 = generated;
        var generatedTable = $"({generated} * {-Multiplier << 8})";
        var generatedTablePs3 = $"({generated} * {Multiplier})";
        
        if (InputIsUint)
        {
            generated = $"({generated} - INT16_MAX)";
            generatedTable = generated;
        }
        else
        {
            generatedPs3 = $"({generated} + INT16_MAX)";
            generatedTablePs3 = $"({generated} + 128)";
        }

        var gen = Type switch
        {
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity when mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                => generatedTablePs3,
            DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity
                => generatedTable,
            DjAxisType.EffectsKnob when mode is ConfigField.Ps3 or ConfigField.Ps3WithoutCapture
                => $"(({generatedPs3} >> 6))",
            DjAxisType.EffectsKnob => generated,
            _ => GenerateAssignment(mode, accelerometer, false, false)
        };

        return $"{GenerateOutput(mode)} = {gen};";
    }

    protected override string MinCalibrationText()
    {
        return Type switch
        {
            DjAxisType.Crossfader => Resources.AxisCalibraitonMinDjCrossfader,
            DjAxisType.EffectsKnob => Resources.AxisCalibrationMinEffectsKnob,
            DjAxisType.LeftTableVelocity => Resources.AxisCalibrationMinDjVelocityLeft,
            DjAxisType.RightTableVelocity => Resources.AxisCalibrationMinDjVelocityRight,
            _ => ""
        };
    }

    protected override string MaxCalibrationText()
    {
        return Type switch
        {
            DjAxisType.Crossfader => Resources.AxisCalibrationMaxDjCrossfader,
            DjAxisType.EffectsKnob => Resources.AxisCalibrationMaxDjEffectsKnob,
            DjAxisType.LeftTableVelocity => Resources.AxisCalibrationMaxDjVelocityLeft,
            DjAxisType.RightTableVelocity => Resources.AxisCalibrationMaxDjVelocityRight,
            _ => ""
        };
    }


    protected override bool SupportsCalibration()
    {
        return Type is DjAxisType.Crossfader;
    }
}
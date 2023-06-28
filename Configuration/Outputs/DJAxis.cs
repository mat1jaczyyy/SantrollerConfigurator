using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class DjAxis : OutputAxis
{
    public DjAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max, int deadZone, DjAxisType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, min, max,
        deadZone,
        false, childOfCombined)
    {
        Multiplier = 0;
        Type = type;
        UpdateDetails();
    }
    
    public DjAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int multiplier, DjAxisType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, 0, 0,
        0,
        false, childOfCombined)
    {
        Multiplier = multiplier;
        Type = type;
        UpdateDetails();
    }

    public int Multiplier
    {
        get => Max;
        set
        {
            if (!IsVelocity) return;
            Max = value;
            this.RaisePropertyChanged();
        }
    }
 
    protected override int Calculate(
        (bool enabled, int value, int min, int max, int deadZone, bool trigger, DeviceControllerType deviceControllerType)
            values)
    {
        if (Type is not (DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity))
        {
            return base.Calculate(values);
        }

        return values.value * values.max;
    }

    public DjAxisType Type { get; }

    public override bool IsKeyboard => false;

    public override string LedOnLabel
    {
        get
        {
            return Type switch
            {
                DjAxisType.Crossfader => "Rightmost LED Colour",
                DjAxisType.EffectsKnob => "Rightmost LED Colour",
                DjAxisType.LeftTableVelocity => "Positive Spin Velocity Colour",
                DjAxisType.RightTableVelocity => "Positive Spin Velocity Colour",
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
                DjAxisType.Crossfader => "Leftmost LED Colour",
                DjAxisType.EffectsKnob => "Leftmost LED Colour",
                DjAxisType.LeftTableVelocity => "Negative Spin Velocity Colour",
                DjAxisType.RightTableVelocity => "Negative Spin Velocity Colour",
                _ => ""
            };
        }
    }

    public bool IsVelocity => Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity;

    public bool SupportsMinMax => !IsDigitalToAnalog && !IsVelocity && Type is not DjAxisType.EffectsKnob;

    public bool IsFader => Type is DjAxisType.Crossfader;

    public override bool ShouldFlip(ConfigField mode)
    {
        return false;
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override object GetOutputType()
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
        if (mode is not (ConfigField.Ps3 or ConfigField.XboxOne or ConfigField.Xbox360)) return "";

        // The crossfader and effects knob on ps3 controllers are shoved into the accelerometer data
        var accelerometer = mode == ConfigField.Ps3 && Type is DjAxisType.Crossfader or DjAxisType.EffectsKnob;
        var gen = GenerateAssignment(mode, accelerometer, false, false);
        // This NEEDS to be 128 for dj hero
        if (Type is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity)
        {
            if (mode is ConfigField.Ps3)
            {
                gen = $"(({Input.Generate()} * {Max}) + 128)";
            }
            else
            {
                gen = $"({Input.Generate()} * {Max})";
            }
        }

        return $"{GenerateOutput(mode)} = {gen};";
    }

    protected override string MinCalibrationText()
    {
        return Type switch
        {
            DjAxisType.Crossfader => "Move fader to the leftmost position",
            DjAxisType.EffectsKnob => "Turn knob until the bar is at the leftmost position",
            DjAxisType.LeftTableVelocity => "Spin the left table the fastest you can to the left",
            DjAxisType.RightTableVelocity => "Spin the right table the fastest you can to the left",
            _ => ""
        };
    }

    protected override string MaxCalibrationText()
    {
        return Type switch
        {
            DjAxisType.Crossfader => "Move fader to the rightmost position",
            DjAxisType.EffectsKnob => "Turn knob until the bar is at the rightmost position",
            DjAxisType.LeftTableVelocity => "Spin the left table the fastest you can to the right",
            DjAxisType.RightTableVelocity => "Spin the right table the fastest you can to the right",
            _ => ""
        };
    }


    protected override bool SupportsCalibration()
    {
        return Type is DjAxisType.Crossfader;
    }
}
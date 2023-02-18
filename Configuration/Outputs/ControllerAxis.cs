using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerAxis : OutputAxis
{
    private readonly ObservableAsPropertyHelper<bool> _valid;

    public ControllerAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max,
        int deadZone, StandardAxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max,
        deadZone,
        type.ToString(), IsTrigger(type))
    {
        Type = type;
        _valid = this.WhenAnyValue(s => s.Model.DeviceType, s => s.Model.RhythmType, s => s.Type)
            .Select(s => ControllerEnumConverter.GetAxisText(s.Item1, s.Item2, s.Item3) != null)
            .ToProperty(this, s => s.Valid);
    }

    public StandardAxisType Type { get; }

    public override bool IsCombined => false;

    public override string LedOnLabel
    {
        get
        {
            switch (Type)
            {
                case StandardAxisType.LeftStickX:
                case StandardAxisType.RightStickX:
                    return "Right LED Colour";
                case StandardAxisType.LeftStickY:
                case StandardAxisType.RightStickY:
                    return "Highest LED Colour";
                case StandardAxisType.LeftTrigger:
                case StandardAxisType.RightTrigger:
                    return "Pressed LED Color";
                default:
                    return "";
            }
        }
    }

    public override string LedOffLabel
    {
        get
        {
            switch (Type)
            {
                case StandardAxisType.LeftStickX:
                case StandardAxisType.RightStickX:
                    return "Left LED Colour";
                case StandardAxisType.LeftStickY:
                case StandardAxisType.RightStickY:
                    return "Lowest LED Colour";
                case StandardAxisType.LeftTrigger:
                case StandardAxisType.RightTrigger:
                    return "Released LED Color";
                default:
                    return "";
            }
        }
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool Valid => _valid.Value;

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetAxisText(deviceControllerType, rhythmType,
            Enum.Parse<StandardAxisType>(Name)) ?? Name;
    }

    private static bool IsTrigger(StandardAxisType type)
    {
        return type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        switch (type)
        {
            case DeviceControllerType.Gamepad:
            case DeviceControllerType.Wheel:
            case DeviceControllerType.ArcadeStick:
            case DeviceControllerType.FlightStick:
            case DeviceControllerType.DancePad:
            case DeviceControllerType.ArcadePad:
                return $"Others/Xbox360/360_{Name}.png";
            case DeviceControllerType.Guitar:
            case DeviceControllerType.Drum:
                return $"{rhythmType}/{Name}.png";
            case DeviceControllerType.LiveGuitar:
                return $"GuitarHero/{Name}.png";
            case DeviceControllerType.Turntable:
                return $"DJ/{Name}.png";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    protected override string MinCalibrationText()
    {
        switch (Type)
        {
            case StandardAxisType.LeftStickX:
            case StandardAxisType.RightStickX:
                return "Move axis to the leftmost position";
            case StandardAxisType.LeftStickY:
            case StandardAxisType.RightStickY:
                return "Move axis to the lowest position";
            case StandardAxisType.LeftTrigger:
            case StandardAxisType.RightTrigger:
                return "Release the trigger";
            default:
                return "";
        }
    }

    protected override string MaxCalibrationText()
    {
        switch (Type)
        {
            case StandardAxisType.LeftStickX:
            case StandardAxisType.RightStickX:
                return "Move axis to the rightmost position";
            case StandardAxisType.LeftStickY:
            case StandardAxisType.RightStickY:
                return "Move axis to the highest position";
            case StandardAxisType.LeftTrigger:
            case StandardAxisType.RightTrigger:
                return "Push the trigger all the way in";
            default:
                return "";
        }
    }

    protected override bool SupportsCalibration()
    {
        return Type is not (StandardAxisType.AccelerationX or StandardAxisType.AccelerationY
            or StandardAxisType.AccelerationZ);
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (mode is not (ConfigField.Ps3 or ConfigField.XboxOne or ConfigField.Xbox360)) return "";
        return base.Generate(mode, debounceIndex, combined, extra);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerAxis(Input?.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone);
    }

    public override void UpdateBindings()
    {
    }
}
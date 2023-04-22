using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerAxis : OutputAxis
{
    private readonly ObservableAsPropertyHelper<bool> _valid;

    public ControllerAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max,
        int deadZone, StandardAxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max,
        deadZone, IsTrigger(type))
    {
        Type = type;
        _valid = this.WhenAnyValue(s => s.Model.DeviceType, s => s.Model.RhythmType, s => s.Type)
            .Select(s => ControllerEnumConverter.GetAxisText(s.Item1, s.Item3).Any())
            .ToProperty(this, s => s.Valid);
        UpdateDetails();
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
    public override bool Valid => _valid.Value;

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetAxisText(deviceControllerType,
            Type) ?? Type.ToString();
    }

    private static bool IsTrigger(StandardAxisType type)
    {
        return type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return mode is not (ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.XboxOne or ConfigField.Xbox360) ? "" : GetReportField(Type);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        switch (type)
        {
            case DeviceControllerType.Gamepad:
            case DeviceControllerType.ArcadeStick:
            case DeviceControllerType.FlightStick:
            case DeviceControllerType.DancePad:
            case DeviceControllerType.ArcadePad:
            case DeviceControllerType.StageKit:
                return $"Others/Xbox360/360_{Type}.png";
            case DeviceControllerType.Guitar:
            case DeviceControllerType.Drum:
                return $"{rhythmType}/{Type}.png";
            case DeviceControllerType.LiveGuitar:
                return $"GuitarHero/{Type}.png";
            case DeviceControllerType.Turntable:
                return $"DJ/{Type}.png";
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
    public override bool ShouldFlip(ConfigField mode)
    {
        // Need to flip y axis on PS4
        return mode is ConfigField.Ps4 && Type is StandardAxisType.LeftStickY or StandardAxisType.RightStickY;
    }

    protected override bool SupportsCalibration()
    {
        return true;
    }
    public override SerializedOutput Serialize()
    {
        return new SerializedControllerAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone);
    }

    public override void UpdateBindings()
    {
    }
}
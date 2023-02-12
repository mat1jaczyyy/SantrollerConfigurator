using System;
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

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetAxisText(deviceControllerType, rhythmType,
            Enum.Parse<StandardAxisType>(Name)) ?? Name;
    }

    private static bool IsTrigger(StandardAxisType type)
    {
        return type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
    }

    public StandardAxisType Type { get; }

    public override string GenerateOutput(ConfigField mode)
    {
        return GetReportField(Type);
    }

    public override bool IsCombined => false;

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

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;

    private readonly ObservableAsPropertyHelper<bool> _valid;
    public override bool Valid => _valid.Value;

    protected override bool SupportsCalibration()
    {
        return Type is not (StandardAxisType.AccelerationX or StandardAxisType.AccelerationY
            or StandardAxisType.AccelerationZ);
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
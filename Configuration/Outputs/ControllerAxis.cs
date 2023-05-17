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

    public ControllerAxis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max,
        int deadZone, StandardAxisType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, min,
        max,
        deadZone, IsTrigger(type), childOfCombined)
    {
        Type = type;
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

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetAxisText(deviceControllerType,
            Type);
    }

    public override object GetOutputType()
    {
        return Type;
    }

    private static bool IsTrigger(StandardAxisType type)
    {
        return type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return mode is not (ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.XboxOne or ConfigField.Xbox360)
            ? ""
            : GetReportField(Type);
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
        // Need to flip y axis on PS3/4
        return mode is ConfigField.Ps4 or ConfigField.Ps3 && Type is StandardAxisType.LeftStickY or StandardAxisType.RightStickY;
    }

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerAxis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone, ChildOfCombined);
    }

    public override void UpdateBindings()
    {
    }
}
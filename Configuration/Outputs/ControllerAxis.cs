using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class ControllerAxis : OutputAxis
{
    private static readonly Dictionary<StandardAxisType, string> Mappings = new()
    {
        {StandardAxisType.LeftStickX, "l_x"},
        {StandardAxisType.LeftStickY, "l_y"},
        {StandardAxisType.RightStickX, "r_x"},
        {StandardAxisType.RightStickY, "r_y"},
        {StandardAxisType.LeftTrigger, "axis[4]"},
        {StandardAxisType.RightTrigger, "axis[5]"},
        {StandardAxisType.AccelerationX, "accel[0]"},
        {StandardAxisType.AccelerationY, "accel[1]"},
        {StandardAxisType.AccelerationZ, "accel[2]"},
    };

    private static readonly Dictionary<StandardAxisType, string> MappingsXbox = new()
    {
        {StandardAxisType.LeftStickX, "l_x"},
        {StandardAxisType.LeftStickY, "l_y"},
        {StandardAxisType.RightStickX, "r_x"},
        {StandardAxisType.RightStickY, "r_y"},
        {StandardAxisType.LeftTrigger, "lt"},
        {StandardAxisType.RightTrigger, "rt"},
    };


    public ControllerAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte? ledIndex,
        float multiplier, int offset,
        int deadZone, StandardAxisType type) : base(model, input, ledOn, ledOff, ledIndex, multiplier, offset, deadZone,
        type.ToString(), (s) => IsTrigger(s, type))
    {
        Type = type;
    }

    private static bool IsTrigger(DeviceControllerType s, StandardAxisType type)
    {
        return (s is DeviceControllerType.Guitar &&
                type is StandardAxisType.RightStickX or StandardAxisType.RightStickY) ||
               type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
    }

    public StandardAxisType Type { get; }


    protected override string GenerateOutput(bool xbox)
    {
        if (xbox)
        {
            return "report->" + MappingsXbox[Type];
        }

        return "report->" + Mappings[Type];
    }

    public override string? GetLocalisedName() =>
        ControllerEnumConverter.GetAxisText(Model.DeviceType, Model.RhythmType, Type);

    public override bool IsCombined => false;

    protected override string MinCalibrationText()
    {
        if (Model.DeviceType == DeviceControllerType.Guitar && Type is StandardAxisType.RightStickX)
        {
            return "Release the whammy";
        }

        if (Model.DeviceType == DeviceControllerType.Guitar && Type is StandardAxisType.RightStickY)
        {
            return "Leave the guitar in a neutral position";
        }

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
        if (Model.DeviceType == DeviceControllerType.Guitar && Type is StandardAxisType.RightStickX)
        {
            return "Push the whammy all the way in";
        }

        if (Model.DeviceType == DeviceControllerType.Guitar && Type is StandardAxisType.RightStickY)
        {
            return "Tilt the guitar up";
        }

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

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerAxis(Input?.GetJson(), Type, LedOn, LedOff, LedIndex, Multiplier, Offset,
            DeadZone);
    }
}
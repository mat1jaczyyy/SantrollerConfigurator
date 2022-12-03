using System;
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


    public ControllerAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max,
        int deadZone, StandardAxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max, deadZone,
        type.ToString(), (s) => IsTrigger(s, type))
    {
        Type = type;
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetAxisText(deviceControllerType, rhythmType, Enum.Parse<StandardAxisType>(Name)) ?? Name;
    }

    public static bool IsTrigger(DeviceControllerType s, StandardAxisType type)
    {
        return (s is DeviceControllerType.Guitar && type is StandardAxisType.RightStickX) ||
               type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
    }

    public StandardAxisType Type { get; }


    protected override string GenerateOutput(bool xbox)
    {
        if (xbox)
        {
            if (!MappingsXbox.ContainsKey(Type)) return "";
            return "report->" + MappingsXbox[Type];
        }

        return "report->" + Mappings[Type];
    }

    public override bool IsCombined => false;

    protected override string MinCalibrationText()
    {
        switch (Model.DeviceType)
        {
            case DeviceControllerType.Guitar when Type is StandardAxisType.RightStickX:
                return "Release the whammy";
            case DeviceControllerType.Guitar when Type is StandardAxisType.RightStickY:
                return "Leave the guitar in a neutral position";
            default:
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
    }

    protected override string MaxCalibrationText()
    {
        switch (Model.DeviceType)
        {
            case DeviceControllerType.Guitar when Type is StandardAxisType.RightStickX:
                return "Push the whammy all the way in";
            case DeviceControllerType.Guitar when Type is StandardAxisType.RightStickY:
                return "Tilt the guitar up";
            default:
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
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;

    protected override bool SupportsCalibration()
    {
        return Type is not (StandardAxisType.AccelerationX or StandardAxisType.AccelerationY
            or StandardAxisType.AccelerationZ);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerAxis(Input?.Serialise(), Type, LedOn, LedOff, LedIndices, Min, Max,
            DeadZone);
    }
}
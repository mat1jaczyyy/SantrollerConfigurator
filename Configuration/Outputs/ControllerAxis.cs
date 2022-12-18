using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

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
        {StandardAxisType.AccelerationZ, "accel[1]"},
        {StandardAxisType.AccelerationY, "accel[2]"},
        {StandardAxisType.Gyro, "accel[3]"},
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

    private static readonly Dictionary<StandardAxisType, StandardAxisType> TurntableMap = new()
    {
        {StandardAxisType.LeftStickX, StandardAxisType.RightStickX},
        {StandardAxisType.LeftStickY, StandardAxisType.RightStickY},
        {StandardAxisType.RightStickX, StandardAxisType.AccelerationX},
        {StandardAxisType.RightStickY, StandardAxisType.AccelerationZ}
    };


    public ControllerAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max,
        int deadZone, StandardAxisType type, bool dj = false) : base(model, input, ledOn, ledOff, ledIndices, min, max,
        deadZone,
        type.ToString(), (s) => IsTrigger(s, type), dj)
    {
        Type = type;
        _valid = this.WhenAnyValue(s => s.Model.DeviceType, s => s.Model.RhythmType, s => s.Type)
            .Select(s => ControllerEnumConverter.GetAxisText(s.Item1, s.Item2, s.Item3) != null)
            .ToProperty(this, s => s.Valid);
    }

    public static string GetMapping(StandardAxisType type, bool xbox)
    {
        return "report->" + (xbox ? MappingsXbox[type] : Mappings[type]);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetAxisText(deviceControllerType, rhythmType,
            Enum.Parse<StandardAxisType>(Name)) ?? Name;
    }

    public static bool IsTrigger(DeviceControllerType s, StandardAxisType type)
    {
        return (s is DeviceControllerType.Guitar && type is StandardAxisType.RightStickX)
               || (s is DeviceControllerType.LiveGuitar && type is StandardAxisType.RightStickY) ||
               type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
    }

    public StandardAxisType Type { get; }

    public StandardAxisType GetRealAxis(bool xbox)
    {
        if (xbox) return Type;
        if (Model.DeviceType == DeviceControllerType.Turntable && TurntableMap.ContainsKey(Type))
        {
            return TurntableMap[Type];
        }

        return Type;
    }

    public override string GenerateOutput(bool xbox, bool useReal)
    {
        if (!xbox)
        {
            return "report->" + Mappings[useReal ? Type : GetRealAxis(xbox)];
        }

        if (!MappingsXbox.ContainsKey(Type)) return "";
        return "report->" + MappingsXbox[Type];
    }

    public override bool IsCombined => false;

    protected override string MinCalibrationText()
    {
        switch (Model.DeviceType)
        {
            case DeviceControllerType.LiveGuitar when Type is StandardAxisType.RightStickY:
                return "Release the whammy";
            case DeviceControllerType.LiveGuitar when Type is StandardAxisType.RightStickX:
                return "Leave the guitar in a neutral position";
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
            case DeviceControllerType.LiveGuitar when Type is StandardAxisType.RightStickX:
                return "Tilt the guitar up";
            case DeviceControllerType.LiveGuitar when Type is StandardAxisType.RightStickY:
                return "Push the whammy all the way in";
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

    private readonly ObservableAsPropertyHelper<bool> _valid;
    public override bool Valid => _valid.Value;

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

    public override void UpdateBindings()
    {
    }
}
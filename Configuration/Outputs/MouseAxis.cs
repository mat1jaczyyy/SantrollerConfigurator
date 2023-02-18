using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class MouseAxis : OutputAxis
{
    public MouseAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max, int deadZone, MouseAxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max,
        deadZone, EnumToStringConverter.Convert(type), false)
    {
        Type = type;
    }

    public override bool IsKeyboard => true;
    public override bool IsController => false;


    public override bool Valid => true;

    public MouseAxisType Type { get; }

    public override bool IsCombined => false;

    public override string LedOnLabel
    {
        get
        {
            switch (Type)
            {
                case MouseAxisType.X:
                case MouseAxisType.ScrollX:
                    return "Right Movement LED";
                case MouseAxisType.Y:
                case MouseAxisType.ScrollY:
                    return "Up Movement LED";
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
                case MouseAxisType.X:
                case MouseAxisType.ScrollX:
                    return "Left Movement LED";
                case MouseAxisType.Y:
                case MouseAxisType.ScrollY:
                    return "Down Movement LED";
                default:
                    return "";
            }
        }
    }

    public override void UpdateBindings()
    {
    }

    public override string GenerateOutput(ConfigField mode)
    {
        if (mode != ConfigField.Mouse) return "";
        return GetReportField(Type);
    }

    protected override string MinCalibrationText()
    {
        switch (Type)
        {
            case MouseAxisType.X:
            case MouseAxisType.ScrollX:
                return "Move axis to the leftmost position";
            case MouseAxisType.Y:
            case MouseAxisType.ScrollY:
                return "Move axis to the lowest position";
            default:
                return "";
        }
    }

    protected override string MaxCalibrationText()
    {
        switch (Type)
        {
            case MouseAxisType.X:
            case MouseAxisType.ScrollX:
                return "Move axis to the rightmost position";
            case MouseAxisType.Y:
            case MouseAxisType.ScrollY:
                return "Move axis to the highest position";
            default:
                return "";
        }
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return "Mouse.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return mode is not ConfigField.Mouse ? "" : base.Generate(mode, debounceIndex, combined, extra);
    }

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedMouseAxis(Input?.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone);
    }
}
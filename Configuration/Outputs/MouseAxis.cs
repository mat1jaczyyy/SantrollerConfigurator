using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class MouseAxis : OutputAxis
{
    public MouseAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max, int deadZone, MouseAxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max,
        deadZone, EnumToStringConverter.Convert(type), false)
    {
        Type = type;
    }

    public override bool IsKeyboard => true;
    public override bool IsController => false;
    public override bool IsMidi => false;

    public override bool Valid => true;
    public override void UpdateBindings()
    {
    }

    public MouseAxisType Type { get; }

    public override string GenerateOutput(DeviceEmulationMode mode)
    {
        if (mode != DeviceEmulationMode.Mouse) return "";
        return GetReportField(Type);
    }

    public override bool IsCombined => false;

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

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedMouseAxis(Input?.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max, DeadZone);
    }
}
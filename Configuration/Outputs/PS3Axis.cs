using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class PS3Axis : OutputAxis
{
    public PS3Axis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max,
        int deadZone, Ps3AxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max, deadZone,
        EnumToStringConverter.Convert(type), true)
    {
        Type = type;
    }

    public Ps3AxisType Type { get; }

    public override bool IsCombined => false;

    public override bool Valid => true;

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return Name;
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"PS3/{Name}.png";
    }


    public override string GenerateOutput(ConfigField mode)
    {
        return mode == ConfigField.Ps3 ? GetReportField(Type) : "";
    }

    protected override string MinCalibrationText()
    {
        return "Release the button";
    }

    protected override string MaxCalibrationText()
    {
        return "Press the button";
    }


    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return mode is not (ConfigField.Ps3 or ConfigField.Ps3Mask) ? "" : base.Generate(mode, debounceIndex, combined, extra);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedPS3Axis(Input?.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone);
    }

    public override void UpdateBindings()
    {
    }
}
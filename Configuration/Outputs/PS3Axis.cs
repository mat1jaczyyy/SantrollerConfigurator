using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class Ps3Axis : OutputAxis
{
    public Ps3Axis(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, int min,
        int max,
        int deadZone, Ps3AxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max, deadZone, true)
    {
        Type = type;
        UpdateDetails();
    }

    public Ps3AxisType Type { get; }

    public override bool IsCombined => false;

    public override bool Valid => true;

    public override bool IsKeyboard => false;
    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"PS3/{Type}.png";
    }


    public override string GenerateOutput(ConfigField mode)
    {
        return mode is ConfigField.Ps3 or ConfigField.Ps4 ? GetReportField(Type) : "";
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
        return new SerializedPS3Axis(Input.Serialise(), Type, LedOn, LedOff, LedIndices.ToArray(), Min, Max,
            DeadZone);
    }

    public override void UpdateBindings()
    {
    }
}
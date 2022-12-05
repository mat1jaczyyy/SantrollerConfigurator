using System;
using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class DrumAxis : OutputAxis
{


    public DrumAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max,
        int deadZone, DrumAxisType type) : base(model, input, ledOn, ledOff, ledIndices, min, max, deadZone,
        type.ToString(), (s) => true)
    {
        Type = type;
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return Name;
    }

    public DrumAxisType Type { get; }


    protected override string GenerateOutput(bool xbox)
    {
        return "";
    }

    public override bool IsCombined => false;

    protected override string MinCalibrationText()
    {
        return "Do nothing";
    }

    protected override string MaxCalibrationText()
    {
        return "Hit the drum";
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;

    protected override bool SupportsCalibration()
    {
        return true;
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDrumAxis(Input?.Serialise(), Type, LedOn, LedOff, LedIndices, Min, Max,
            DeadZone);
    }
}
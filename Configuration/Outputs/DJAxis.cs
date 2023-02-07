using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class DJAxis : OutputAxis
{
    public DjAxisType Type { get; }

    public DJAxis(ConfigViewModel model, DjAxisType type, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, int min, int max, int deadZone, string name, bool dj = false) : base(model, input, ledOn, ledOff, ledIndices, min, max, deadZone, name, false, dj)
    {
        Type = type;
    }

    public override SerializedOutput Serialize()
    {
        
        throw new System.NotImplementedException();
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;
    public override bool Valid => true;
    public override string GenerateOutput(DeviceEmulationMode mode)
    {
        return GetReportField(Type);
    }
    
    public override string Generate(DeviceEmulationMode mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (Input == null) throw new IncompleteConfigurationException("Missing input!");
        if (mode == DeviceEmulationMode.Shared) return "";
        var led = Input is FixedInput ? "" : CalculateLeds(mode);
        // The crossfader and effects knob on ps3 controllers are shoved into the accelerometer data
        var accelerometer = mode == DeviceEmulationMode.Ps3 && Type is DjAxisType.Crossfader or DjAxisType.EffectsKnob;
        return $"{GenerateOutput(mode)} = {GenerateAssignment(mode, accelerometer, false, false)}; {led}";
    }

    protected override string MinCalibrationText()
    {
        return Type switch
        {
            DjAxisType.Crossfader => "Move fader to the leftmost position",
            DjAxisType.EffectsKnob => "Turn knob until the bar is at the leftmost position",
            DjAxisType.LeftTableVelocity => "Spin the left table the fastest you can to the left",
            DjAxisType.RightTableVelocity => "Spin the right table the fastest you can to the left",
            _ => ""
        };
    }

    protected override string MaxCalibrationText()
    {
        return Type switch
        {
            DjAxisType.Crossfader => "Move fader to the rightmost position",
            DjAxisType.EffectsKnob => "Turn knob until the bar is at the rightmost position",
            DjAxisType.LeftTableVelocity => "Spin the left table the fastest you can to the right",
            DjAxisType.RightTableVelocity => "Spin the right table the fastest you can to the right",
            _ => ""
        };
    }

    protected override bool SupportsCalibration()
    {
        return Type is DjAxisType.Crossfader;
    }
}
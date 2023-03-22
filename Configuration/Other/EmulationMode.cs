using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Other;

public class EmulationMode : Output
{
    public EmulationMode(ConfigViewModel model, Input input, EmulationModeType type) : base(
        model, input, Colors.Black, Colors.Black, Array.Empty<byte>())
    {
        Type = type;
    }

    private EmulationModeType _emulationModeType;

    public EmulationModeType Type
    {
        get => _emulationModeType;
        set
        {
            this.RaiseAndSetIfChanged(ref _emulationModeType, value);
            UpdateDetails();
        }
    }

    public EmulationModeType[] EmulationModes { get; } = Enum.GetValues<EmulationModeType>();

    private string GetDefinition()
    {
        return Type switch
        {
            EmulationModeType.XboxOne => "XBOXONE",
            EmulationModeType.Wii => "WII_RB",
            EmulationModeType.Ps3 => "PS3",
            EmulationModeType.Ps4Or5 => "PS4",
            EmulationModeType.Switch => "SWITCH",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override bool IsCombined => false;
    public override bool IsStrum => false;

    public override bool IsKeyboard => false;
    public virtual bool IsController => false;

    public override bool Valid => true;
    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override SerializedOutput Serialize()
    {
        return new SerializedEmulationMode(Type, Input.Serialise());
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type) + " Console Mode Binding";
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        var image = Type switch
        {
            EmulationModeType.XboxOne => "XboxOne",
            EmulationModeType.Wii => "Wii",
            EmulationModeType.Ps3 => "PS3",
            EmulationModeType.Ps4Or5 => "PS4",
            EmulationModeType.Switch => "Switch",
            _ => throw new ArgumentOutOfRangeException()
        };
        return $"Combined/{image}.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        if (mode != ConfigField.Detection) return "";
        var ifStatement = string.Join(" && ", debounceIndex.Select(x => $"debounce[{x}]"));
        return $@"
            if ({ifStatement}) {{
                consoleType = {GetDefinition()};
                reset_usb();
            }}";
    }

    public override void UpdateBindings()
    {
    }
}
using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerButton : OutputButton
{
    private readonly ObservableAsPropertyHelper<bool> _valid;

    public ControllerButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce, StandardButtonType type) : base(model, input, ledOn, ledOff, ledIndices, debounce)
    {
        Type = type;
        _valid = this.WhenAnyValue(s => s.Model.DeviceType, s => s.Model.RhythmType, s => s.Type)
            .Select(s => ControllerEnumConverter.GetButtonText(s.Item1, s.Item2, s.Item3) != null)
            .ToProperty(this, s => s.Valid);
        UpdateDetails();
    }

    public StandardButtonType Type { get; }

    public override bool IsKeyboard => false;

    public override bool IsStrum => Type is StandardButtonType.DpadUp or StandardButtonType.DpadDown;

    public override bool IsCombined => false;
    public override bool Valid => _valid.Value;
    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetButtonText(deviceControllerType, rhythmType, Type) ?? Type.ToString();
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return mode is not (ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.Shared or ConfigField.XboxOne or ConfigField.Xbox360
            or ConfigField.Ps3Mask or ConfigField.Ps4Mask or ConfigField.Xbox360Mask or ConfigField.XboxOneMask)
            ? ""
            : GetReportField(Type);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        switch (type)
        {
            case DeviceControllerType.Gamepad:
            case DeviceControllerType.ArcadeStick:
            case DeviceControllerType.FlightStick:
            case DeviceControllerType.DancePad:
            case DeviceControllerType.ArcadePad:
                return $"Others/Xbox360/360_{Type}.png";
            case DeviceControllerType.Guitar:
            case DeviceControllerType.Drum:
                return $"{rhythmType}/{Type}.png";
            case DeviceControllerType.LiveGuitar:
                return $"GuitarHero/{Type}.png";
            case DeviceControllerType.Turntable:
                return $"DJ/{Type}.png";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}
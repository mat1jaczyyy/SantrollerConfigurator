using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerButton : OutputButton
{

    public ControllerButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce, StandardButtonType type, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices,
        debounce, childOfCombined)
    {
        Type = type;
        UpdateDetails();
    }

    public StandardButtonType Type { get; }

    public override bool IsKeyboard => false;

    public override bool IsStrum => Type is StandardButtonType.DpadUp or StandardButtonType.DpadDown;

    public override bool IsCombined => false;
    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetButtonText(deviceControllerType, Type);
    }

    public override object GetOutputType()
    {
        return Type;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        return mode is ConfigField.Ps3 or ConfigField.Ps4 or ConfigField.Shared or ConfigField.XboxOne
            or ConfigField.Xbox360 or ConfigField.Universal
            ? GetReportField(Type)
            : "";
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type,
            ChildOfCombined);
    }
}
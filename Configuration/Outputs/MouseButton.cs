using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class MouseButton : OutputButton
{
    public MouseButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce,
        MouseButtonType type) : base(model, input, ledOn, ledOff, ledIndices, debounce)
    {
        Type = type;
        UpdateDetails();
    }

    public override bool IsKeyboard => true;
    public override bool IsController => false;

    public MouseButtonType Type { get; }

    public override bool Valid => true;
    public override bool IsStrum => false;

    public override bool IsCombined => false;

    public override void UpdateBindings()
    {
    }

    public override string GenerateOutput(ConfigField mode)
    {
        if (mode != ConfigField.Mouse) return "";
        return GetReportField(Type);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return EnumToStringConverter.Convert(Type);
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return "Mouse.png";
    }

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return mode is not (ConfigField.Mouse or ConfigField.Shared or ConfigField.MouseMask)
            ? ""
            : base.Generate(mode, debounceIndex, combined, extra);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedMouseButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}
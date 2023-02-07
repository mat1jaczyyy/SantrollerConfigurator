using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerButton : OutputButton
{
    public ControllerButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce, StandardButtonType type) : base(model, input, ledOn, ledOff, ledIndices, debounce,
        type.ToString())
    {
        Type = type;
        _valid = this.WhenAnyValue(s => s.Model.DeviceType, s => s.Model.RhythmType, s => s.Type)
            .Select(s => ControllerEnumConverter.GetButtonText(s.Item1, s.Item2, s.Item3) != null)
            .ToProperty(this, s => s.Valid);
    }

    public override string GetName(DeviceControllerType deviceControllerType, RhythmType? rhythmType)
    {
        return ControllerEnumConverter.GetButtonText(deviceControllerType, rhythmType,
            Enum.Parse<StandardButtonType>(Name)) ?? Name;
    }

    public StandardButtonType Type { get; }

    public override string GenerateOutput(DeviceEmulationMode mode)
    {
        return GetReportField(Type);
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;
    public override bool IsStrum => Type is StandardButtonType.DpadUp or StandardButtonType.DpadDown;

    public override bool IsCombined => false;

    private readonly ObservableAsPropertyHelper<bool> _valid;
    public override bool Valid => _valid.Value;

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerButton(Input?.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}
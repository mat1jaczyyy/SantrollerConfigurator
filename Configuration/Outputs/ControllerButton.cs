using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class ControllerButton : OutputButton
{
    public static readonly List<StandardButtonType> Order = new()
    {
        StandardButtonType.X,
        StandardButtonType.A,
        StandardButtonType.B,
        StandardButtonType.Y,
        StandardButtonType.Lb,
        StandardButtonType.Rb,
        StandardButtonType.Lt,
        StandardButtonType.Rt,
        StandardButtonType.Select,
        StandardButtonType.Start,
        StandardButtonType.LeftStick,
        StandardButtonType.RightStick,
        StandardButtonType.Home,
        StandardButtonType.Capture
    };

    public static readonly List<StandardButtonType> OrderGh = new()
    {
        StandardButtonType.Y,
        StandardButtonType.A,
        StandardButtonType.B,
        StandardButtonType.X,
        StandardButtonType.Lb,
        StandardButtonType.Rb,
        StandardButtonType.Lt,
        StandardButtonType.Rt,
        StandardButtonType.Select,
        StandardButtonType.Start,
        StandardButtonType.LeftStick,
        StandardButtonType.RightStick,
        StandardButtonType.Home,
        StandardButtonType.Capture
    };
    
    public static readonly List<StandardButtonType> OrderSwitch = new()
    {
        StandardButtonType.Y,
        StandardButtonType.B,
        StandardButtonType.A,
        StandardButtonType.X,
        StandardButtonType.Lb,
        StandardButtonType.Rb,
        StandardButtonType.Lt,
        StandardButtonType.Rt,
        StandardButtonType.Select,
        StandardButtonType.Start,
        StandardButtonType.LeftStick,
        StandardButtonType.RightStick,
        StandardButtonType.Home,
        StandardButtonType.Capture
    };

    public static readonly List<StandardButtonType> XboxOrder = new()
    {
        StandardButtonType.Up,
        StandardButtonType.Down,
        StandardButtonType.Left,
        StandardButtonType.Right,
        StandardButtonType.Start,
        StandardButtonType.Select,
        StandardButtonType.LeftStick,
        StandardButtonType.RightStick,
        StandardButtonType.Lb,
        StandardButtonType.Rb,
        StandardButtonType.Home,
        StandardButtonType.Capture,
        StandardButtonType.A,
        StandardButtonType.B,
        StandardButtonType.X,
        StandardButtonType.Y
    };

    public static readonly List<StandardButtonType> HatOrder = new()
    {
        StandardButtonType.Up,
        StandardButtonType.Down,
        StandardButtonType.Left,
        StandardButtonType.Right,
    };

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

    public override string GenerateIndex(bool xbox)
    {
        if (xbox)
        {
            //On the xbox, LT and RT are analog only.
            return XboxOrder.Contains(Type) ? XboxOrder.IndexOf(Type).ToString() : "";
        }

        if (Model.DeviceType == DeviceControllerType.Guitar && Model.RhythmType == RhythmType.GuitarHero)
        {
            if (Type is StandardButtonType.X or StandardButtonType.Y)
            {
                return $"(consoleType == PS3 ? {OrderGh.IndexOf(Type).ToString()}) : {Order.IndexOf(Type).ToString()})";
            }
        }

        return HatOrder.Contains(Type) ? HatOrder.IndexOf(Type).ToString() : $"(consoleType == SWITCH ? {OrderSwitch.IndexOf(Type).ToString()} : {Order.IndexOf(Type).ToString()})";
    }

    public override string GenerateOutput(bool xbox)
    {
        if (!xbox && HatOrder.Contains(Type))
        {
            return "report->hat";
        }

        return "report->buttons";
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;
    public override bool IsStrum => Type is StandardButtonType.Up or StandardButtonType.Down;

    public override bool IsCombined => false;

    private readonly ObservableAsPropertyHelper<bool> _valid;
    public override bool Valid => _valid.Value;

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerButton(Input?.Serialise(), LedOn, LedOff, LedIndices, Debounce, Type);
    }
}
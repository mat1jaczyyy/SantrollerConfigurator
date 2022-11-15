using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class ControllerButton : OutputButton
{
    public static readonly List<StandardButtonType> Order = new()
    {
        StandardButtonType.Y,
        StandardButtonType.B,
        StandardButtonType.A,
        StandardButtonType.X,
        StandardButtonType.Lb,
        StandardButtonType.Rb,
        StandardButtonType.Lt,
        StandardButtonType.Rt,
        StandardButtonType.Start,
        StandardButtonType.Select,
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

    public ControllerButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte? ledIndex,
        byte debounce, StandardButtonType type) : base(model, input, ledOn, ledOff, ledIndex, debounce, type.ToString())
    {
        Type = type;
    }

    public StandardButtonType Type { get; }

    public override string? GetLocalisedName() =>
        ControllerEnumConverter.GetButtonText(Model.DeviceType, Model.RhythmType, Type);

    public override string GenerateIndex(bool xbox)
    {
        if (xbox)
        {
            //On the xbox, LT and RT are analog only.
            return XboxOrder.Contains(Type) ? XboxOrder.IndexOf(Type).ToString() : "";
        }

        return HatOrder.Contains(Type) ? HatOrder.IndexOf(Type).ToString() : Order.IndexOf(Type).ToString();
    }

    public override string GenerateOutput(bool xbox)
    {
        if (!xbox && HatOrder.Contains(Type))
        {
            return "report->hat";
        }

        return "report->buttons";
    }

    public override bool IsStrum => Type is StandardButtonType.Up or StandardButtonType.Down;

    public override bool IsCombined => false;

    public override SerializedOutput Serialize()
    {
        return new SerializedControllerButton(Input?.GetJson(), LedOn, LedOff, LedIndex, Debounce, Type);
    }
}
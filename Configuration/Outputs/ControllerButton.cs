using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Json;
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
        StandardButtonType.LB,
        StandardButtonType.RB,
        StandardButtonType.LT,
        StandardButtonType.RT,
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
        StandardButtonType.LB,
        StandardButtonType.RB,
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

    public ControllerButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, int debounce, StandardButtonType type) : base(model, input, ledOn, ledOff, debounce, type.ToString())
    {
        Type = type;
    }
    public StandardButtonType Type { get; }
    public override string GenerateIndex(bool xbox)
    {
        if (xbox)
        {
            return XboxOrder.IndexOf(Type).ToString();
        }

        if (HatOrder.Contains(Type))
        {
            return HatOrder.IndexOf(Type).ToString();
        }
        return Order.IndexOf(Type).ToString();
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
    public override JsonOutput GetJson()
    {
        return new JsonControllerButton(Input?.GetJson(), LedOn, LedOff, Debounce, Type);
    }
}
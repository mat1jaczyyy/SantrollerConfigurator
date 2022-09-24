using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Output;

public class ControllerButton : OutputButton
{
    public static List<StandardButtonType> order = new List<StandardButtonType>()
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

    public static List<StandardButtonType> xboxOrder = new List<StandardButtonType>()
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

    public static List<StandardButtonType> hatOrder = new List<StandardButtonType>()
    {
        StandardButtonType.Up,
        StandardButtonType.Down,
        StandardButtonType.Left,
        StandardButtonType.Right,
    };

    public ControllerButton(IInput? input, Color ledOn, Color ledOff, int debounce, StandardButtonType type) : base(input, ledOn, ledOff, debounce)
    {
        Type = type;
    }

    public override string Name => Type.ToString();
    //TODO: this
    public override string Image => Name;
    public StandardButtonType Type { get; }
    public override string GenerateIndex(bool xbox)
    {
        if (xbox)
        {
            return xboxOrder.IndexOf(Type).ToString();
        }

        if (hatOrder.Contains(Type))
        {
            return hatOrder.IndexOf(Type).ToString();
        }
        return order.IndexOf(Type).ToString();
    }

    public override string GenerateOutput(bool xbox)
    {
        if (!xbox && hatOrder.Contains(Type))
        {
            return "report->hat";
        }
        return "report->buttons";
    }

    public override bool IsStrum()
    {
        // TODO: probably want to have some way to check if its actually a guitar?
        return Type is StandardButtonType.Up or StandardButtonType.Down;
    }
}
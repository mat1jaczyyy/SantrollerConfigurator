using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class ControllerButton : OutputButton
{
    public static List<StandardButtonType> order = new()
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

    public static List<StandardButtonType> xboxOrder = new()
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

    public static List<StandardButtonType> hatOrder = new()
    {
        StandardButtonType.Up,
        StandardButtonType.Down,
        StandardButtonType.Left,
        StandardButtonType.Right,
    };

    public ControllerButton(ConfigViewModel model, IInput? input, Color ledOn, Color ledOff, int debounce, StandardButtonType type) : base(model, input, ledOn, ledOff, debounce)
    {
        Type = type;
    }

    public override string Name => Type.ToString();
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
        return Type is StandardButtonType.Up or StandardButtonType.Down;
    }
}
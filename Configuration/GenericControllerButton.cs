using System.Collections.Generic;
using Dahomey.Json.Attributes;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

[JsonDiscriminator(nameof(GenericControllerButton))]
public class GenericControllerButton : IOutputButton
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

    public StandardButtonType Type { get; }
    public OutputType OutputType => OutputType.Generic;

    public int Index(bool xbox)
    {
        if (xbox)
        {
            return xboxOrder.IndexOf(Type);
        }

        if (hatOrder.Contains(Type))
        {
            return hatOrder.IndexOf(Type);
        }
        return order.IndexOf(Type);
    }

    public string Generate(bool xbox)
    {
        if (!xbox && hatOrder.Contains(Type))
        {
            return "report->hat";
        }
        return "report->buttons";
    }

    public GenericControllerButton(StandardButtonType type)
    {
        Type = type;
    }
}
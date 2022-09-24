using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Output;

public class MouseButton : OutputButton
{
    // TODO need to have this all in the right order
    private static List<MouseButtonType> order = new()
    {
        MouseButtonType.Left,
        MouseButtonType.Right,
        MouseButtonType.Middle
    };

    public MouseButton(IInput? input, Color ledOn, Color ledOff, int debounce, MouseButtonType type) : base(input, ledOn, ledOff, debounce)
    {
        Type = type;
    }

    public override string Name => Type.ToString();
    //TODO: this
    public override string Image => Name;
    public MouseButtonType Type { get; }
    public override string GenerateIndex(bool xbox)
    {
        return order.IndexOf(Type).ToString();
    }

    public override string GenerateOutput(bool xbox)
    {
        return "report->buttons";
    }

    public override bool IsStrum()
    {
        return false;
    }
}
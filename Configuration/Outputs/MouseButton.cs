using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class MouseButton : OutputButton
{
    // TODO need to have this all in the right order
    private static List<MouseButtonType> order = new()
    {
        MouseButtonType.Left,
        MouseButtonType.Right,
        MouseButtonType.Middle
    };

    public MouseButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, int debounce, MouseButtonType type) : base(model, input, ledOn, ledOff, debounce, type.ToString())
    {
        Type = type;
    }

    public MouseButtonType Type { get; }
    public override string GenerateIndex(bool xbox)
    {
        return order.IndexOf(Type).ToString();
    }

    public override string GenerateOutput(bool xbox)
    {
        return "report->buttons";
    }

    public override bool IsStrum => false;

    public override bool IsCombined => false;
}
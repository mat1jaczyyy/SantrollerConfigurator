using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
public class MouseButton : OutputButton
{
    private static List<MouseButtonType> _order = new()
    {
        MouseButtonType.Left,
        MouseButtonType.Right,
        MouseButtonType.Middle
    };

    public MouseButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce, MouseButtonType type) : base(model, input, ledOn, ledOff, ledIndices, debounce, type.ToString())
    {
        Type = type;
        ControllerType = type.ToString();
    }

    public override bool IsKeyboard => true;
    public override bool IsController => false;
    public override bool IsMidi => false;
    public MouseButtonType Type { get; }

    public override string GenerateIndex(bool xbox)
    {
        return _order.IndexOf(Type).ToString();
    }

    public override string GenerateOutput(bool xbox)
    {
        return "report->buttons";
    }

    public override bool IsStrum => false;

    public override bool IsCombined => false;

    public override SerializedOutput Serialize()
    {
        return new SerializedMouseButton(Input?.Serialise(), LedOn, LedOff, LedIndices, Debounce, Type);
    }
}
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

    public MouseButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte ledIndex, byte debounce, MouseButtonType type) : base(model, input, ledOn, ledOff, ledIndex, debounce, type.ToString())
    {
        Type = type;
    }

    public MouseButtonType Type { get; }

    protected override string GenerateIndex(bool xbox)
    {
        return _order.IndexOf(Type).ToString();
    }

    protected override string GenerateOutput(bool xbox)
    {
        return "report->buttons";
    }

    public override bool IsStrum => false;

    public override bool IsCombined => false;
    public override string? GetLocalisedName() => Name;

    public override SerializedOutput Serialize()
    {
        return new SerializedMouseButton(Input?.Serialise(), LedOn, LedOff, LedIndex, Debounce, Type);
    }
}
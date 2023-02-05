using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;
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
    }

    public override bool IsKeyboard => true;
    public override bool IsController => false;
    public override bool IsMidi => false;
    public override void UpdateBindings()
    {
    }

    public MouseButtonType Type { get; }

    public override bool Valid => true;

    public override string GenerateIndex(DeviceEmulationMode mode)
    {
        return _order.IndexOf(Type).ToString();
    }

    public override string GenerateOutput(DeviceEmulationMode mode)
    {
        return "report->buttons";
    }

    public override bool IsStrum => false;

    public override bool IsCombined => false;

    public override SerializedOutput Serialize()
    {
        return new SerializedMouseButton(Input?.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}
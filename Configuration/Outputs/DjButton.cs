using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class DjButton : ControllerButton
{
    private static readonly Dictionary<DjInputType, StandardButtonType> Buttons = new()
    {
        {DjInputType.LeftAny, StandardButtonType.Lb},
        {DjInputType.LeftGreen, StandardButtonType.A},
        {DjInputType.LeftRed, StandardButtonType.B},
        {DjInputType.LeftBlue, StandardButtonType.X},
        {DjInputType.RightAny, StandardButtonType.Rb},
        {DjInputType.RightGreen, StandardButtonType.A},
        {DjInputType.RightRed, StandardButtonType.B},
        {DjInputType.RightBlue, StandardButtonType.X},
    };

    private readonly DjInputType _djButtonType;
    public DjButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce, DjInputType type) : base(model, input, ledOn, ledOff, ledIndices, debounce, Buttons[type])
    {
        _djButtonType = type;
    }

    public override string GenerateOutput(bool xbox)
    {
        return xbox ? base.GenerateOutput(xbox) : "report->accel[2]";
    }

    public override string GenerateIndex(bool xbox)
    {
        if (xbox) return base.GenerateIndex(xbox);
        return _djButtonType switch
        {
            DjInputType.RightGreen => "0",
            DjInputType.RightRed => "1",
            DjInputType.RightBlue => "2",
            DjInputType.LeftGreen => "4",
            DjInputType.LeftRed => "5",
            DjInputType.LeftBlue => "6",
            _ => ""
        };
    }

    public override string Generate(bool xbox, bool shared, List<int> debounceIndex, bool combined, string extra)
    {
        if (!xbox && _djButtonType is DjInputType.LeftAny or DjInputType.RightAny)
        {
            return "";
        }
        return base.Generate(xbox, shared, debounceIndex, combined, extra);
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjButton(Input?.Serialise(), LedOn, LedOff, LedIndices, Debounce, _djButtonType);
    }
}
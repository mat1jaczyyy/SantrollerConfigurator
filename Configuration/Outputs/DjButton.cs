using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class DjButton : OutputButton
{
    private static readonly Dictionary<DjInputType, string> Buttons = new()
    {
        {DjInputType.LeftGreen, "0"},
        {DjInputType.LeftRed, "1"},
        {DjInputType.LeftBlue, "2"},
        {DjInputType.RightGreen, "0"},
        {DjInputType.RightRed, "1"},
        {DjInputType.RightBlue, "2"},
    };

    private static readonly Dictionary<DjInputType, string> ButtonsPs3 = new()
    {
        {DjInputType.LeftGreen, "0"},
        {DjInputType.LeftRed, "1"},
        {DjInputType.LeftBlue, "2"},
        {DjInputType.RightGreen, "4"},
        {DjInputType.RightRed, "5"},
        {DjInputType.RightBlue, "6"},
    };

    private static readonly Dictionary<DjInputType, string> Axis = new()
    {
        {DjInputType.LeftGreen, "report->lt"},
        {DjInputType.LeftRed, "report->lt"},
        {DjInputType.LeftBlue, "report->lt"},
        {DjInputType.RightGreen, "report->rt"},
        {DjInputType.RightRed, "report->rt"},
        {DjInputType.RightBlue, "report->rt"},
    };

    public readonly DjInputType Type;

    public DjButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce,
        DjInputType type) : base(model, input, ledOn, ledOff, ledIndices, debounce, type.ToString())
    {
        Type = type;
    }

    public override string GenerateOutput(DeviceEmulationMode mode)
    {
        return mode == DeviceEmulationMode.Xbox360 ? Axis[Type] : "report->accel[2]";
    }

    public override string GenerateIndex(DeviceEmulationMode mode)
    {
        return mode == DeviceEmulationMode.Xbox360 ? Buttons[Type] : ButtonsPs3[Type];
    }

    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;

    public override bool IsStrum => false;

    public override bool Valid => true;

    public override SerializedOutput Serialize()
    {
        return new SerializedDjButton(Input?.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Type);
    }
}
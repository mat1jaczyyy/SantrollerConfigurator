using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;
public class GhwtCombinedOutput : Output
{
    private readonly List<Output> _bindingsTap;
    private readonly Output _bindingTapBar;
    public bool MapTapBarToAxis { get; set; }
    public bool MapTapBarToFrets { get; set; }
    
    public int Pin { get; set; }
    private readonly Microcontroller _microcontroller;

    public GhwtCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? pin = null, bool mapTapBarToFrets = false, bool mapTapBarToAxis = false) : base(model, null, Colors.Transparent, Colors.Transparent, "GHWT")
    {
        _microcontroller = microcontroller;
        this.MapTapBarToFrets = mapTapBarToAxis;
        this.MapTapBarToAxis = mapTapBarToAxis;
        if (pin.HasValue)
        {
            Pin = pin.Value;
        }
    }

    public override bool IsCombined => true;

    public override SerializedOutput GetJson()
    {
        return new SerializedGhwtCombinedOutput(LedOn, LedOff, Pin, MapTapBarToFrets, MapTapBarToAxis);
    }

    public override string Generate(bool xbox, int debounceIndex)
    {
        return "";
    }

    public override IReadOnlyList<Output> Outputs => GetBindings();

    private IReadOnlyList<Output> GetBindings()
    {
        List<Output> outputs = new();
        if (MapTapBarToAxis)
        {
            outputs.Add(new ControllerAxis(Model, new GhWtTapInput(GhWtInputType.TapBar, _microcontroller),
                Colors.Transparent,
                Colors.Transparent, 1, 0, 0, StandardAxisType.RightStickY));
        }

        if (MapTapBarToFrets)
        {
            outputs.Add(new ControllerButton(Model, new GhWtTapInput(GhWtInputType.TapGreen, _microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.A));
            outputs.Add(new ControllerButton(Model, new GhWtTapInput(GhWtInputType.TapRed, _microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.B));
            outputs.Add(new ControllerButton(Model, new GhWtTapInput(GhWtInputType.TapYellow, _microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.Y));
            outputs.Add(new ControllerButton(Model, new GhWtTapInput(GhWtInputType.TapBlue, _microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.X));
            outputs.Add(new ControllerButton(Model, new GhWtTapInput(GhWtInputType.TapOrange, _microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.Lb));
        }

        return outputs.AsReadOnly();
    }
}
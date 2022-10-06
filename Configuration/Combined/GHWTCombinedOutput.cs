using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;
public class GHWTCombinedOutput : Output
{
    private readonly List<Output> BindingsTap;
    private readonly Output BindingTapBar;
    public bool MapTapBarToAxis { get; set; }
    public bool MapTapBarToFrets { get; set; }
    
    public int Pin { get; set; }

    public GHWTCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? pin = null, bool mapTapBarToFrets = false, bool mapTapBarToAxis = false) : base(model, null, Colors.Transparent, Colors.Transparent, "GHWT")
    {
        this.MapTapBarToFrets = mapTapBarToAxis;
        this.MapTapBarToAxis = mapTapBarToAxis;
        if (pin.HasValue)
        {
            Pin = pin.Value;
        }

        BindingsTap = new()
        {
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapGreen, microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.A),
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapRed, microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.B),
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapYellow, microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.Y),
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapBlue, microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.X),
            new ControllerButton(model, new GhWtTapInput(GhWtInputType.TapOrange, microcontroller), Colors.Green,
                Colors.Transparent, 5, StandardButtonType.LB),
        };
        BindingTapBar = new ControllerAxis(model, new GhWtTapInput(GhWtInputType.TapBar, microcontroller),
            Colors.Transparent,
            Colors.Transparent, 1, 0, 0, StandardAxisType.RightStickY);
    }

    public override bool IsCombined => true;

    public override SerializedOutput GetJson()
    {
        return new SerializedGhwtCombinedOutput(LedOn, LedOff, Pin, MapTapBarToFrets, MapTapBarToAxis);
    }

    public override string Generate(bool xbox)
    {
        return "";
    }

    public override IReadOnlyList<Output> Outputs => GetBindings();

    private IReadOnlyList<Output> GetBindings()
    {
        List<Output> outputs = new();

        if (MapTapBarToAxis)
        {
            outputs.Add(BindingTapBar);
        }

        if (MapTapBarToFrets)
        {
            outputs.AddRange(BindingsTap);
        }

        return outputs.AsReadOnly();
    }
}
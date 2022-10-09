using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;
public class Gh5CombinedOutput : TwiOutput
{
    private readonly List<Output> _bindingsFret;
    private readonly List<Output> _bindingsTap;
    private readonly Output _bindingTapBar;
    public bool MapTapBarToAxis { get; set; }
    public bool MapTapBarToFrets { get; set; }
    
    public bool FretsEnabled { get; set; }

    public Gh5CombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda=null, int? scl=null, bool fretsEnabled = true, bool mapTapBarToFrets = false, bool mapTapBarToAxis = false) : base(model,
        microcontroller,
        "gh5", 100000, "GH5", sda, scl)
    {
        FretsEnabled = fretsEnabled;
        MapTapBarToAxis = mapTapBarToAxis;
        MapTapBarToFrets = mapTapBarToFrets;
        _bindingsFret = new()
        {
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.Green, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.A),
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.Red, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.B),
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.Yellow, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.Y),
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.Blue, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.X),
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.Orange, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.Lb),
        };

        _bindingsTap = new()
        {
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.TapGreen, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.A),
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.TapRed, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.B),
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.TapYellow, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.Y),
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.TapBlue, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.X),
            new ControllerButton(model, new Gh5NeckInput(Gh5NeckInputType.TapOrange, microcontroller), Colors.Green,
                Colors.Transparent, 5,
                StandardButtonType.Lb),
        };
        _bindingTapBar = new ControllerAxis(model, new Gh5NeckInput(Gh5NeckInputType.TapBar, microcontroller),
            Colors.Transparent,
            Colors.Transparent, 1, 0, 0, StandardAxisType.RightStickY);
    }

    public override bool IsCombined => true;

    public override SerializedOutput GetJson()
    {
        return new SerializedGh5CombinedOutput(LedOn, LedOff, Sda, Scl, FretsEnabled, MapTapBarToFrets, MapTapBarToAxis);
    }

    public override string Generate(bool xbox, int debounceIndex)
    {
        return "";
    }

    public override IReadOnlyList<Output> Outputs => GetBindings();

    private IReadOnlyList<Output> GetBindings()
    {
        List<Output> outputs = new();
        if (FretsEnabled)
        {
            outputs.AddRange(_bindingsFret);
        }

        if (MapTapBarToAxis)
        {
            outputs.Add(_bindingTapBar);
        }

        if (MapTapBarToFrets)
        {
            outputs.AddRange(_bindingsTap);
        }

        return outputs.AsReadOnly();
    }
}
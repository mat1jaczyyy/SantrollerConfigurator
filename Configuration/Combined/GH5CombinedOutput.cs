using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Json;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;
public class GH5CombinedOutput : TwiOutput
{
    private readonly List<Output> BindingsFret;
    private readonly List<Output> BindingsTap;
    private readonly Output BindingTapBar;
    public bool MapTapBarToAxis { get; set; }
    public bool MapTapBarToFrets { get; set; }
    
    public bool FretsEnabled { get; set; }

    public GH5CombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda=null, int? scl=null, bool fretsEnabled = false, bool mapTapBarToFrets = false, bool mapTapBarToAxis = false) : base(model,
        microcontroller,
        "gh5", 100000, "GH5", sda, scl)
    {
        FretsEnabled = fretsEnabled;
        MapTapBarToAxis = mapTapBarToAxis;
        MapTapBarToFrets = mapTapBarToFrets;
        BindingsFret = new()
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
                StandardButtonType.LB),
        };

        BindingsTap = new()
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
                StandardButtonType.LB),
        };
        BindingTapBar = new ControllerAxis(model, new Gh5NeckInput(Gh5NeckInputType.TapBar, microcontroller),
            Colors.Transparent,
            Colors.Transparent, 1, 0, 0, StandardAxisType.RightStickY);
    }

    public override bool IsCombined => true;

    public override JsonOutput GetJson()
    {
        return new JsonGh5CombinedOutput(LedOn, LedOff, Sda, Scl, FretsEnabled, MapTapBarToFrets, MapTapBarToAxis);
    }

    public override string Generate(bool xbox)
    {
        return "";
    }

    public override IReadOnlyList<Output> Outputs => GetBindings();

    private IReadOnlyList<Output> GetBindings()
    {
        List<Output> outputs = new();
        if (FretsEnabled)
        {
            outputs.AddRange(BindingsFret);
        }

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
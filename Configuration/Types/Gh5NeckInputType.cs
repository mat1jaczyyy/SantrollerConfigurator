using System;
using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

[Flags]
public enum BarButton
{
    Green = 1,
    Red = 2,
    Yellow = 4,
    Blue = 8,
    Orange = 16
}

public enum Gh5NeckInputType
{
    [Description("Green Fret")] Green,
    [Description("Red Fret")] Red,
    [Description("Yellow Fret")] Yellow,
    [Description("Blue Fret")] Blue,
    [Description("Orange Fret")] Orange,
    [Description("Slider Green Fret")] TapGreen,
    [Description("Slider Red Fret")] TapRed,
    [Description("Slider Yellow Fret")] TapYellow,
    [Description("Slider Blue Fret")] TapBlue,
    [Description("Slider Orange Fret")] TapOrange,
    [Description("Slider To Frets")] TapAll,
    [Description("Slider Axis")] TapBar
}
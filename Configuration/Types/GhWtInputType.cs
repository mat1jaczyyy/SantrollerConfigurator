using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum GhWtInputType
{
    [Description("Slider Green Fret")] TapGreen,
    [Description("Slider Red Fret")] TapRed,
    [Description("Slider Yellow Fret")] TapYellow,
    [Description("Slider Blue Fret")] TapBlue,
    [Description("Slider Orange Fret")] TapOrange,
    [Description("Slider To Frets")] TapAll,
    [Description("Slider Axis")] TapBar
}
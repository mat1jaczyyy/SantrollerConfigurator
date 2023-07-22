using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum WiiControllerType
{
    Nunchuk,
    [Description("Classic Controller")]
    ClassicController,
    [Description("Classic Controller Pro")]
    ClassicControllerPro,
    [Description("THQ UDraw Tablet")]
    UDraw,
    [Description("Drawsome Tablet")]
    Drawsome,
    [Description("Guitar Hero Guitar")]
    Guitar,
    [Description("Guitar Hero Drums")]
    Drum,
    [Description("DJ Hero Turntable")]
    Dj,
    [Description("Taiko Drums")]
    Taiko,
    [Description("Wii Motion Plus")]
    MotionPlus,
    [Description("Plugged in extension")]
    Selected,
    [Description("All Inputs")]
    All
}
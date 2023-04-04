using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum ConsoleType
{
    Universal=0,
    KeyboardMouse,
    Midi,
    StageKit,
    [Description("Xbox 360")]
    Xbox360,
    [Description("PS3")]
    Ps3,
    [Description("Wii Rock Band")]
    Wii,
    Switch,
    [Description("PS4")]
    Ps4,
    [Description("Xbox One / Series S / Series X")]
    XboxOne,
    Unknown=0xFF
}
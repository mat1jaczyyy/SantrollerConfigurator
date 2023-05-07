using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum EmulationModeType
{
    [Description("Xbox 360")] Xbox360,

    [Description("Xbox One / Series S / Series X")]
    XboxOne,
    [Description("Wii Rock Band")] Wii,

    [Description("PS3 (PS2 / Wii GH with optional modules)")]
    Ps3,
    [Description("PS4 / PS5")] Ps4Or5,
    [Description("Switch")] Switch
}
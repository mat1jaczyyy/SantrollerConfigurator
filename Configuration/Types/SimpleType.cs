using System.ComponentModel;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Types;

public enum SimpleType
{
    [Description("Automatic Wii Inputs")]
    WiiInputSimple,
    [Description("Automatic PS2 Inputs")]
    Ps2InputSimple,
    [Description("Automatic Guitar Hero World Tour Tap Bar Inputs")]
    WtNeckSimple,
    [Description("Automatic Guitar Hero 5 Neck Inputs")]
    Gh5NeckSimple,
    [Description("Automatic DJ Hero Turntable Inputs")]
    DjTurntableSimple
}
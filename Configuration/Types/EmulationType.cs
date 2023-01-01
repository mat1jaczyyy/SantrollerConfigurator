using System.ComponentModel;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Types;

public enum EmulationType
{
    Controller,
    [Description("Keyboard + Mouse")]
    KeyboardMouse,
    Midi
}
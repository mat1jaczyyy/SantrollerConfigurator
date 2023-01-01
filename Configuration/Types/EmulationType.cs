using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum EmulationType
{
    Controller,
    [Description("Keyboard + Mouse")]
    KeyboardMouse,
    Midi
}
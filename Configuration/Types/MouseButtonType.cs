using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum MouseButtonType
{
    [Description("Left Click")]
    Left,
    [Description("Right Click")]
    Right,
    [Description("Middle Click")]
    Middle
}
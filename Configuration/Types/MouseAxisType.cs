using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum MouseAxisType
{
    [Description("Mouse X Axis")]
    X,
    [Description("Mouse Y Axis")]
    Y,
    [Description("Mouse Scroll")]
    ScrollY,
    [Description("Mouse Horizontal Scroll")]
    ScrollX
}
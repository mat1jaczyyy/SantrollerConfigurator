using System.ComponentModel;

namespace GuitarConfiguratorSharp.NetCore.Configuration.PS2;

public enum Ps3AxisType
{
    [Description("Up Pressure")]
    UpButton,
    [Description("Right Pressure")]
    RightButton,
    [Description("Left Pressure")]
    LeftButton,
    [Description("Down Pressure")]
    DownButton,
    [Description("L2 Pressure")]
    L2,
    [Description("R2 Pressure")]
    R2,
    [Description("L1 Pressure")]
    L1,
    [Description("R1 Pressure")]
    R1,
    [Description("Triangle Pressure")]
    Triangle,
    [Description("Circle Pressure")]
    Circle,
    [Description("Cross Pressure")]
    Cross,
    [Description("Square Pressure")]
    Square,
}
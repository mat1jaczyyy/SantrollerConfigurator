using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum Ps3AxisType
{
    [Description("Dpad Up Pressure")] PressureDPadUp,
    [Description("Dpad Right Pressure")] PressureDPadRight,
    [Description("Dpad Left Pressure")] PressureDPadLeft,
    [Description("Dpad Down Pressure")] PressureDPadDown,
    [Description("L1 Pressure")] PressureL1,
    [Description("R1 Pressure")] PressureR1,
    [Description("Triangle Pressure")] PressureTriangle,
    [Description("Circle Pressure")] PressureCircle,
    [Description("Cross Pressure")] PressureCross,
    [Description("Square Pressure")] PressureSquare
}
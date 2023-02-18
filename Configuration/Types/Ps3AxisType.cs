using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum Ps3AxisType
{
    [Description("Dpad Up Pressure")] PressureDpadUp,
    [Description("Dpad Right Pressure")] PressureDpadRight,
    [Description("Dpad Left Pressure")] PressureDpadLeft,
    [Description("Dpad Down Pressure")] PressureDpadDown,
    [Description("L1 Pressure")] PressureL1,
    [Description("R1 Pressure")] PressureR1,
    [Description("Triangle Pressure")] PressureTriangle,
    [Description("Circle Pressure")] PressureCircle,
    [Description("Cross Pressure")] PressureCross,
    [Description("Square Pressure")] PressureSquare
}
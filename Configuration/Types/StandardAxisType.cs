using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum StandardAxisType
{
    [Description("Left Joystick X Axis")]
    LeftStickX,
    [Description("Left Joystick Y Axis")]
    LeftStickY,
    [Description("Right Joystick X Axis")]
    RightStickX,
    [Description("Right Joystick Y Axis")]
    RightStickY,
    [Description("Left Trigger Axis")]
    LeftTrigger,
    [Description("Right Trigger Axis")]
    RightTrigger
}
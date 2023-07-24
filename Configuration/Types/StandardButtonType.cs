using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum StandardButtonType
{
    [Description("A Button")]
    A,
    [Description("B Button")]
    B,
    [Description("X Button")]
    X,
    [Description("Y Button")]
    Y,
    [Description("Left Bumper")]
    LeftShoulder,
    [Description("Right Bumper")]
    RightShoulder,
    [Description("D-pad up")]
    DpadUp,
    [Description("D-pad down")]
    DpadDown,
    [Description("D-pad Left")]
    DpadLeft,
    [Description("D-pad Right")]
    DpadRight,
    [Description("Start Button")]
    Start,
    [Description("Select Button")]
    Back,
    [Description("Home Button")]
    Guide,
    [Description("Capture Button")]
    Capture,
    [Description("Left Stick Click")]
    LeftThumbClick,
    [Description("Right Stick Click")]
    RightThumbClick
}
using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum Ps2InputType
{
    [Description("GunCon - Horizontal Sync")]
    GunconHSync,

    [Description("GunCon - Vertical Sync")]
    GunconVSync,
    [Description("Mouse - X Axis")] MouseX,
    [Description("Mouse - Y Axis")] MouseY,
    [Description("NegCon - Twist Axis")] NegConTwist,

    [Description("NegCon - I Button Pressure")]
    NegConI,

    [Description("NegCon - II Button Pressure")]
    NegConIi,

    [Description("NegCon - L Button Pressure")]
    NegConL,
    [Description("JogCon - Wheel Axis")] JogConWheel,
    [Description("Guitar - Whammy Axis")] GuitarWhammy,

    [Description("Gamepad - Left Stick X")]
    LeftX,

    [Description("Gamepad - Left Stick Y")]
    LeftY,

    [Description("Gamepad - Right Stick X")]
    RightX,

    [Description("Gamepad - Right Stick Y")]
    RightY,

    [Description("DualShock 2 - L1 Pressure")]
    Dualshock2L1,

    [Description("DualShock 2 - R1 Pressure")]
    Dualshock2R1,

    [Description("DualShock 2 - D-Pad Right Pressure")]
    Dualshock2RightButton,

    [Description("DualShock 2 - D-Pad Left Pressure")]
    Dualshock2LeftButton,

    [Description("DualShock 2 - D-Pad Up Pressure")]
    Dualshock2UpButton,

    [Description("DualShock 2 - D-Pad Down Pressure")]
    Dualshock2DownButton,

    [Description("DualShock 2 - Triangle Pressure")]
    Dualshock2Triangle,

    [Description("DualShock 2 - Circle Pressure")]
    Dualshock2Circle,

    [Description("DualShock 2 - Cross Pressure")]
    Dualshock2Cross,

    [Description("DualShock 2 - Square Pressure")]
    Dualshock2Square,

    [Description("DualShock 2 - L2 Pressure")]
    Dualshock2L2,

    [Description("DualShock 2 - R2 Pressure")]
    Dualshock2R2,
    [Description("NegCon - A Button")] NegConA,
    [Description("NegCon - B Button")] NegConB,
    [Description("NegCon - Start Button")] NegConStart,
    [Description("NegCon - R Button")] NegConR,
    [Description("Guitar - Green Fret")] GuitarGreen,
    [Description("Guitar - Red Fret")] GuitarRed,
    [Description("Guitar - Yellow Fret")] GuitarYellow,
    [Description("Guitar - Blue Fret")] GuitarBlue,
    [Description("Guitar - Orange Fret")] GuitarOrange,
    [Description("Guitar - Strum Up")] GuitarStrumUp,
    [Description("Guitar - Strum Down")] GuitarStrumDown,

    [Description("Guitar - Select Button")]
    GuitarSelect,
    [Description("Guitar - Start Button")] GuitarStart,
    [Description("Guitar - Tilt")] GuitarTilt,

    [Description("Gamepad - Select Button")]
    Select,
    [Description("Gamepad - L3 Button")] L3,
    [Description("Gamepad - R3 Button")] R3,

    [Description("Gamepad - Start Button")]
    Start,
    [Description("Gamepad - D-Pad Up")] Up,
    [Description("Gamepad - D-Pad Right")] Right,
    [Description("Gamepad - D-Pad Down")] Down,
    [Description("Gamepad - D-Pad Left")] Left,
    [Description("Gamepad - L2 Button")] L2,
    [Description("Gamepad - R2 Button")] R2,
    [Description("Gamepad - L1 Button")] L1,
    [Description("Gamepad - R1 Button")] R1,

    [Description("Gamepad - Triangle Button")]
    Triangle,

    [Description("Gamepad - Circle Button")]
    Circle,

    [Description("Gamepad - Cross Button")]
    Cross,

    [Description("Gamepad - Square Button")]
    Square,
    [Description("All - Map Joystick To Dpad")]
    JoystickToDpad
}
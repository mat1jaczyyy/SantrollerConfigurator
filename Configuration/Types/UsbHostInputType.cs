using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum UsbHostInputType
{
    [Description("X Button")] X,
    [Description("A Button")] A,
    [Description("B Button")] B,
    [Description("Y Button")] Y,
    
    [Description("Left Bumper")] LeftShoulder,
    [Description("Right Bumper")] RightShoulder,
    
    [Description("Kick Pedal 1")] Kick1,
    [Description("Kick Pedal 2")] Kick2,
    
    [Description("Back Button")] Back,
    [Description("Start Button")] Start,
    
    [Description("Left Stick Click")] LeftStickClick,
    [Description("Right Stick Click")] RightStickClick,
    
    [Description("Home Button")] Guide,
    [Description("Capture Button")] Capture,
    
    [Description("Left Turntable Blue Fret")] LeftBlue,
    [Description("Left Turntable Red Fret")] LeftRed,
    [Description("Left Turntable Green Fret")] LeftGreen,
    [Description("Right Turntable Blue Fret")] RightBlue,
    [Description("Right Turntable Red Fret")] RightRed,
    [Description("Right Turntable Green Fret")] RightGreen,
    [Description("Solo Green Fret")] SoloGreen,
    [Description("Solo Red Fret")] SoloRed,
    [Description("Solo Yellow Fret")] SoloYellow,
    [Description("Solo Blue Fret")] SoloBlue,
    [Description("Solo Orange Fret")] SoloOrange,
    
    [Description("Green Fret")] Green,
    [Description("Red Fret")] Red,
    [Description("Yellow Fret")] Yellow,
    [Description("Blue Fret")] Blue,
    [Description("Orange Fret")] Orange,
    [Description("D-pad Up")] DpadUp,
    [Description("D-pad Down")] DpadDown,
    [Description("D-pad Left")] DpadLeft,
    [Description("D-pad Right")] DpadRight,
    
    [Description("Left Trigger Axis")] LeftTrigger,
    [Description("Right Trigger Axis")] RightTrigger,
    
    [Description("Left Joystick X Axis")] LeftStickX,
    [Description("Left Joystick Y Axis")] LeftStickY,
    [Description("Right Joystick X Axis")] RightStickX,
    [Description("Right Joystick Y Axis")] RightStickY,
    
    [Description("Dpad Up Pressure")] PressureDpadUp,
    [Description("Dpad Right Pressure")] PressureDpadRight,
    [Description("Dpad Left Pressure")] PressureDpadLeft,
    [Description("Dpad Down Pressure")] PressureDpadDown,
    [Description("L1 Pressure")] PressureL1,
    [Description("R1 Pressure")] PressureR1,
    [Description("Triangle Pressure")] PressureTriangle,
    [Description("Circle Pressure")] PressureCircle,
    [Description("Cross Pressure")] PressureCross,
    [Description("Square Pressure")] PressureSquare,
    RedVelocity,
    YellowVelocity,
    BlueVelocity,
    GreenVelocity,
    OrangeVelocity,
    BlueCymbalVelocity,
    YellowCymbalVelocity,
    GreenCymbalVelocity,
    KickVelocity,
    Whammy,
    Tilt,
    Pickup,
    Slider,
    [Description("Left Turntable Spin")] LeftTableVelocity,
    [Description("Right Turntable Spin")] RightTableVelocity,
    EffectsKnob,
    Crossfader,
    AccelX,
    AccelZ,
    AccelY,
    Gyro,
}
using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum WiiInputType
{
    [Description("Classic Controller - Left Stick X Axis")]
    ClassicLeftStickX,

    [Description("Classic Controller - Left Stick Y Axis")]
    ClassicLeftStickY,

    [Description("Classic Controller - Right Stick X Axis")]
    ClassicRightStickX,

    [Description("Classic Controller - Right Stick Y Axis")]
    ClassicRightStickY,

    [Description("Classic Controller - Left Trigger")]
    ClassicLeftTrigger,

    [Description("Classic Controller - Right Trigger")]
    ClassicRightTrigger,

    [Description("Drum - Green Pad Pressure")]
    DrumGreenPressure,

    [Description("Drum - Red Pad Pressure")]
    DrumRedPressure,

    [Description("Drum - Yellow Pad Pressure")]
    DrumYellowPressure,

    [Description("Drum - Blue Pad Pressure")]
    DrumBluePressure,

    [Description("Drum - Orange Pad Pressure")]
    DrumOrangePressure,

    [Description("Drum - Kick Pedal Pressure")]
    DrumKickPedalPressure,

    [Description("Guitar - Joystick X Axis")]
    GuitarJoystickX,

    [Description("Guitar - Joystick Y Axis")]
    GuitarJoystickY,
    [Description("Guitar - Whammy")] GuitarWhammy,
    [Description("Guitar - Slider Axis")] GuitarTapBar,

    [Description("Nunchuk - Joystick X Axis")]
    NunchukStickX,

    [Description("Nunchuk - Joystick Y Axis")]
    NunchukStickY,

    [Description("Nunchuk - Acceleration X Axis")]
    NunchukAccelerationX,

    [Description("Nunchuk - Acceleration Y Axis")]
    NunchukAccelerationY,

    [Description("Nunchuk - Acceleration Z Axis")]
    NunchukAccelerationZ,
    [Description("Nunchuk - Pitch")] NunchukRotationPitch,
    [Description("Nunchuk - Roll")] NunchukRotationRoll,
    [Description("Turntable - Left Spin")] DjTurntableLeft,

    [Description("Turntable - Right Spin")]
    DjTurntableRight,

    [Description("Turntable - Crossfade Slider")]
    DjCrossfadeSlider,

    [Description("Turntable - Effects Dial")]
    DjEffectDial,

    [Description("Turntable - Joystick X Axis")]
    DjStickX,

    [Description("Turntable - Joystick Y Axis")]
    DjStickY,

    [Description("UDraw - Pen X Position")]
    UDrawPenX,

    [Description("UDraw - Pen Y Position")]
    UDrawPenY,
    [Description("UDraw - Pen Pressure")] UDrawPenPressure,

    [Description("Drawsome - Pen X Position")]
    DrawsomePenX,

    [Description("Drawsome - Pen Y Position")]
    DrawsomePenY,

    [Description("Drawsome - Pen Pressure")]
    DrawsomePenPressure,
    [Description("Guitar - Green Fret")] GuitarGreen,
    [Description("Guitar - Red Fret")] GuitarRed,
    [Description("Guitar - Yellow Fret")] GuitarYellow,
    [Description("Guitar - Blue Fret")] GuitarBlue,
    [Description("Guitar - Orange Fret")] GuitarOrange,

    [Description("Guitar - Slider To Frets")]
    GuitarTapAll,

    [Description("Guitar - Slider Green Fret")]
    GuitarTapGreen,

    [Description("Guitar - Slider Red Fret")]
    GuitarTapRed,

    [Description("Guitar - Slider Yellow Fret")]
    GuitarTapYellow,

    [Description("Guitar - Slider Blue Fret")]
    GuitarTapBlue,

    [Description("Guitar - Slider Orange Fret")]
    GuitarTapOrange,
    [Description("Guitar - Minus Button")] GuitarMinus,
    [Description("Guitar - Plus Button")] GuitarPlus,

    [Description("Guitar - Strum Up Button")]
    GuitarStrumUp,

    [Description("Guitar - Strum Down Button")]
    GuitarStrumDown,
    [Description("Drum - Green Pad")] DrumGreen,
    [Description("Drum - Red Pad")] DrumRed,
    [Description("Drum - Yellow Pad")] DrumYellow,
    [Description("Drum - Blue Pad")] DrumBlue,
    [Description("Drum - Orange Pad")] DrumOrange,
    [Description("Drum - Minus Button")] DrumMinus,
    [Description("Drum - Plus Button")] DrumPlus,
    [Description("Drum - Kick Pedal")] DrumKickPedal,
    [Description("Nunchuk - C Button")] NunchukC,
    [Description("Nunchuk - Z Button")] NunchukZ,
    [Description("UDraw - Pen Click")] UDrawPenClick,
    [Description("UDraw - Pen Button 1")] UDrawPenButton1,
    [Description("UDraw - Pen Button 2")] UDrawPenButton2,
    [Description("Taiko - Left Rim Hit")] TaTaConLeftDrumRim,

    [Description("Taiko - Left Center Hit")]
    TaTaConLeftDrumCenter,
    [Description("Taiko - Right Rim Hit")] TaTaConRightDrumRim,

    [Description("Taiko - Right Center Hit")]
    TaTaConRightDrumCenter,

    [Description("Turntable - Euphoria Button")]
    DjHeroEuphoria,

    [Description("Turntable - Left Green Fret")]
    DjHeroLeftGreen,

    [Description("Turntable - Left Red Fret")]
    DjHeroLeftRed,

    [Description("Turntable - Left Blue Fret")]
    DjHeroLeftBlue,

    [Description("Turntable - Right Green Fret")]
    DjHeroRightGreen,

    [Description("Turntable - Right Red Fret")]
    DjHeroRightRed,

    [Description("Turntable - Right Blue Fret")]
    DjHeroRightBlue,

    [Description("Turntable - Minus Button")]
    DjHeroMinus,

    [Description("Turntable - Plus Button")]
    DjHeroPlus,

    [Description("Classic Controller - A Button")]
    ClassicA,

    [Description("Classic Controller - B Button")]
    ClassicB,

    [Description("Classic Controller - X Button")]
    ClassicX,

    [Description("Classic Controller - Y Button")]
    ClassicY,

    [Description("Classic Controller - D-Pad Up")]
    ClassicDPadUp,

    [Description("Classic Controller - D-Pad Down")]
    ClassicDPadDown,

    [Description("Classic Controller - D-Pad Left")]
    ClassicDPadLeft,

    [Description("Classic Controller - D-Pad Right")]
    ClassicDPadRight,

    [Description("Classic Controller - Zl Button")]
    ClassicZl,

    [Description("Classic Controller - Zr Button")]
    ClassicZr,

    [Description("Classic Controller - L Button")]
    ClassicLt,

    [Description("Classic Controller - R Button")]
    ClassicRt,

    [Description("Classic Controller - Plus Button")]
    ClassicPlus,

    [Description("Classic Controller - Minus Button")]
    ClassicMinus,

    [Description("Classic Controller - Home Button")]
    ClassicHome,
    
    [Description("All - Map Joystick To Dpad")]
    JoystickToDpad
}
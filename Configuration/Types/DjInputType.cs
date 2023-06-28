using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum DjInputType
{
    [Description("Left Turntable Spin")] LeftTurntable,
    [Description("Right Turntable Spin")] RightTurntable,

    [Description("Left Turntable Green Fret")]
    LeftGreen,

    [Description("Left Turntable Red Fret")]
    LeftRed,

    [Description("Left Turntable Blue Fret")]
    LeftBlue,

    [Description("Right Turntable Green Fret")]
    RightGreen,

    [Description("Right Turntable Red Fret")]
    RightRed,

    [Description("Right Turntable Blue Fret")]
    RightBlue
}
using System;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Neck;

[Flags]
public enum BarButton
{
    Green = 1,
    Red = 2,
    Yellow = 4,
    Blue = 8,
    Orange = 16
}

public enum Gh5NeckInputType 
{
    None,
    Green,
    Red,
    Yellow,
    Blue,
    Orange,
    TapGreen,
    TapRed,
    TapYellow,
    TapBlue,
    TapOrange,
    TapBar
}
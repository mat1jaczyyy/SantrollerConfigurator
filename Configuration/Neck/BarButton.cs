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
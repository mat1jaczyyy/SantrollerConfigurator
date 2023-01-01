using System;
using Avalonia.Media;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum LedType
{
    None,
    APA102_RGB,
    APA102_RBG,
    APA102_GRB,
    APA102_GBR,
    APA102_BRG,
    APA102_BGR,
}

public static class LedTypeMethods
{
    public static byte[] GetColors(this LedType type, Color color)
    {
        switch (type)
        {
            case LedType.APA102_RGB:
                return new[] {color.R, color.G, color.B};
            case LedType.APA102_RBG:
                return new[] {color.R, color.B, color.G};
            case LedType.APA102_GRB:
                return new[] {color.G, color.R, color.B};
            case LedType.APA102_GBR:
                return new[] {color.G, color.B, color.R};
            case LedType.APA102_BRG:
                return new[] {color.B, color.R, color.G};
            case LedType.APA102_BGR:
                return new[] {color.B, color.G, color.R};
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static int GetColorsAsInt(this LedType type, Color color)
    {
        var bytes = GetColors(type, color);
        return bytes[0] | bytes[1] << 8 | bytes[2] << 16;
    } 
}
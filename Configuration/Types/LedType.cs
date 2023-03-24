using System;
using System.Collections.Generic;
using System.Linq;
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
    APA102_BGR
}

public static class LedTypeMethods
{
    public static byte[] GetLedBytes(this LedType type, Color color)
    {
        return type switch
        {
            LedType.APA102_RGB => new[] {color.R, color.G, color.B},
            LedType.APA102_RBG => new[] {color.R, color.B, color.G},
            LedType.APA102_GRB => new[] {color.G, color.R, color.B},
            LedType.APA102_GBR => new[] {color.G, color.B, color.R},
            LedType.APA102_BRG => new[] {color.B, color.R, color.G},
            LedType.APA102_BGR => new[] {color.B, color.G, color.R},
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static IEnumerable<string> GetLedStrings(this LedType type, string r, string g, string b)
    {
        return type switch
        {
            LedType.APA102_RGB => new[] {r, g, b},
            LedType.APA102_RBG => new[] {r, b, g},
            LedType.APA102_GRB => new[] {g, r, b},
            LedType.APA102_GBR => new[] {g, b, r},
            LedType.APA102_BRG => new[] {b, r, g},
            LedType.APA102_BGR => new[] {b, g, r},
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static string GetLedAssignment(this LedType type, Color color, byte index)
    {
        var data = GetLedBytes(type, color);
        return string.Join("\n",
            data.Zip(new[] {'r', 'g', 'b'}).Select(pair => $"ledState[{index - 1}].{pair.Second} = {pair.First};"));
    }

    public static string GetLedAssignment(this LedType type, string r, string g, string b, byte index)
    {
        var data = GetLedStrings(type, r, g, b);
        return string.Join("\n",
            data.Zip(new[] {'r', 'g', 'b'}).Select(pair => $"ledState[{index - 1}].{pair.Second} = {pair.First};"));
    }

    public static string GetLedAssignment(this LedType type, Color on, Color off, string value, byte index)
    {
        return string.Join("",
            type.GetLedBytes(on).Zip(type.GetLedBytes(off), new[] {'r', 'g', 'b'}).Select(b =>
                $"ledState[{index - 1}].{b.Third} = (uint8_t)({b.First} + ((int16_t)({b.Second - b.First} * ({value})) >> 8));"));
    }
}
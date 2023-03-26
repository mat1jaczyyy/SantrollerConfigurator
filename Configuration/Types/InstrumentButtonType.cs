using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum InstrumentButtonType
{
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
    Black1,
    Black2,
    Black3,
    White1,
    White2,
    White3,
    [Description("Strum Up")]
    StrumUp,
    [Description("Strum Down")]
    StrumDown,
}

public static class InstrumentButtonTypeExtensions
{
    private static readonly InstrumentButtonType[] GuitarButtons =
    {
        InstrumentButtonType.Green,
        InstrumentButtonType.Red,
        InstrumentButtonType.Yellow,
        InstrumentButtonType.Blue,
        InstrumentButtonType.Orange,
        InstrumentButtonType.StrumDown,
        InstrumentButtonType.StrumUp
    };

    private static readonly InstrumentButtonType[] RbButtons =
    {
        InstrumentButtonType.Green,
        InstrumentButtonType.Red,
        InstrumentButtonType.Yellow,
        InstrumentButtonType.Blue,
        InstrumentButtonType.Orange,
        InstrumentButtonType.SoloGreen,
        InstrumentButtonType.SoloRed,
        InstrumentButtonType.SoloYellow,
        InstrumentButtonType.SoloBlue,
        InstrumentButtonType.SoloOrange,
        InstrumentButtonType.StrumDown,
        InstrumentButtonType.StrumUp
    };

    private static readonly InstrumentButtonType[] GhlButtons =
    {
        InstrumentButtonType.Black1,
        InstrumentButtonType.Black2,
        InstrumentButtonType.Black3,
        InstrumentButtonType.White1,
        InstrumentButtonType.White2,
        InstrumentButtonType.White3,
        InstrumentButtonType.StrumDown,
        InstrumentButtonType.StrumUp
    };

    public static IEnumerable<InstrumentButtonType> GetButtons(DeviceControllerType deviceControllerType,
        RhythmType type)
    {
        return deviceControllerType switch
        {
            DeviceControllerType.Guitar when type is RhythmType.GuitarHero => GuitarButtons,
            DeviceControllerType.Guitar when type is RhythmType.RockBand => RbButtons,
            DeviceControllerType.LiveGuitar => GhlButtons,
            _ => Enumerable.Empty<InstrumentButtonType>()
        };
    }
}
using System.Collections.Generic;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Types;


public enum DrumAxisType
{
    Green,
    Red,
    Yellow,
    Blue,
    Orange,
    GreenCymbal,
    YellowCymbal,
    BlueCymbal,
    Kick,
    Kick2
}

public static class DrumAxisTypeMethods
{

    public static IEnumerable<DrumAxisType> RbTypes()
    {
        return new[]
        {
            DrumAxisType.Green, DrumAxisType.Red, DrumAxisType.Yellow, DrumAxisType.Blue, DrumAxisType.GreenCymbal,
            DrumAxisType.YellowCymbal, DrumAxisType.BlueCymbal, DrumAxisType.Kick, DrumAxisType.Kick2
        };
    }

    public static IEnumerable<DrumAxisType> GhTypes()
    {
        return new[]
        {
            DrumAxisType.Green,
            DrumAxisType.Red,
            DrumAxisType.Yellow,
            DrumAxisType.Blue,
            DrumAxisType.Orange,
            DrumAxisType.Kick,
            DrumAxisType.Kick2
        };
    }

    public static IEnumerable<DrumAxisType> GetTypeFor(RhythmType type)
    {
        return type == RhythmType.GuitarHero ? GhTypes() : RbTypes();
    }
}
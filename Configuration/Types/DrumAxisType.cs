using System.Collections.Generic;
using System.Linq;

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

    public static IEnumerable<DrumAxisType> GetInvalidTypesFor(RhythmType type)
    {
        return type == RhythmType.GuitarHero ? RbTypes() : GhTypes();
    }

    public static IEnumerable<DrumAxisType> GetDifferenceFor(RhythmType rhythmType)
    {
        return GetInvalidTypesFor(rhythmType).Except(GetTypeFor(rhythmType));
    }
    public static IEnumerable<DrumAxisType> GetDifferenceInverseFor(RhythmType rhythmType)
    {
        return GetTypeFor(rhythmType).Except(GetInvalidTypesFor(rhythmType));
    }
}
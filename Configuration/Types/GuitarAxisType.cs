using System.Collections.Generic;
using System.Linq;

namespace GuitarConfigurator.NetCore.Configuration.Types;


public enum GuitarAxisType
{
    Whammy,
    Pickup,
    Slider,
    Tilt
}

public static class GuitarAxisTypeMethods
{

    public static IEnumerable<GuitarAxisType> RbTypes()
    {
        return new[]
        {
            GuitarAxisType.Pickup, GuitarAxisType.Tilt, GuitarAxisType.Whammy
        };
    }

    public static IEnumerable<GuitarAxisType> GhTypes()
    {
        return new[]
        {
            GuitarAxisType.Slider, GuitarAxisType.Tilt, GuitarAxisType.Whammy
        };
    }

    public static IEnumerable<GuitarAxisType> GetTypeFor(DeviceControllerType deviceControllerType, RhythmType type)
    {
        return (deviceControllerType == DeviceControllerType.LiveGuitar || type == RhythmType.GuitarHero) ? GhTypes() : RbTypes();
    }

    public static IEnumerable<GuitarAxisType> GetInvalidTypesFor(DeviceControllerType deviceControllerType,RhythmType type)
    {
        return (deviceControllerType == DeviceControllerType.LiveGuitar || type == RhythmType.GuitarHero) ? RbTypes() : GhTypes();
    }

    public static IEnumerable<GuitarAxisType> GetDifferenceFor(RhythmType rhythmType,
        DeviceControllerType deviceControllerType)
    {
        return GetInvalidTypesFor(deviceControllerType, rhythmType).Except(GetTypeFor(deviceControllerType, rhythmType));
    }
    public static IEnumerable<GuitarAxisType> GetDifferenceInverseFor(RhythmType rhythmType,
        DeviceControllerType deviceControllerType)
    {
        return GetTypeFor(deviceControllerType, rhythmType).Except(GetInvalidTypesFor(deviceControllerType, rhythmType));
    }
}
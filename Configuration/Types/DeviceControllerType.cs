using System;
using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum DeviceControllerType
{
    Gamepad = 1,
    DancePad,
    [Description("Guitar Hero Guitar")]
    GuitarHeroGuitar,
    [Description("Rock Band Guitar")]
    RockBandGuitar,
    [Description("Guitar Hero Drums")]
    GuitarHeroDrums,
    [Description("Rock Band Drums")]
    RockBandDrums,
    LiveGuitar,
    Turntable,
    [Description("Rock Band Stage Kit")] StageKit
}

public static class DeviceControllerRhythmTypeExtensions {
    public static bool Is5FretGuitar(this DeviceControllerType type)
    {
        return type is DeviceControllerType.GuitarHeroGuitar or DeviceControllerType.RockBandGuitar;
    }
    public static bool IsGuitar(this DeviceControllerType type)
    {
        return type.Is5FretGuitar() || type is DeviceControllerType.LiveGuitar;
    }
    public static bool IsDrum(this DeviceControllerType type)
    {
        return type is DeviceControllerType.GuitarHeroDrums or DeviceControllerType.RockBandDrums;
    }
    public static bool IsGh(this DeviceControllerType type)
    {
        return type is DeviceControllerType.GuitarHeroDrums or DeviceControllerType.GuitarHeroGuitar;
    }
    public static bool IsRb(this DeviceControllerType type)
    {
        return type is DeviceControllerType.RockBandDrums or DeviceControllerType.RockBandGuitar;
    }
}
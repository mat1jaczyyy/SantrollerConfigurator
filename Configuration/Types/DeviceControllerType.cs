using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum DeviceControllerType
{
    Gamepad = 1,
    ArcadeStick = 3,
    FlightStick = 4,
    DancePad = 5,
    ArcadePad = 6,
    Guitar = 7,
    LiveGuitar = 8,
    Drum = 9,
    Turntable = 10,
    [Description("Rock Band Stage Kit")] StageKit = 11
}
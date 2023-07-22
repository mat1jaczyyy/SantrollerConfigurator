using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum Ps2ControllerType : byte
{
    [Description("PS1 Controller")]
    Digital = 1,
    [Description("Dualshock 1")]
    Dualshock,
    [Description("Dualshock 2")]
    Dualshock2,
    [Description("Guitar Hero Guitar")]
    Guitar,
    [Description("FlightStick")]
    FlightStick,
    [Description("NegCon")]
    NegCon,
    [Description("GunCon")]
    GunCon,
    [Description("JogCon")]
    JogCon,
    [Description("Mouse")]
    Mouse,
    [Description("Plugged in ontroller")]
    Selected,
    [Description("All Inputs")]
    All
}
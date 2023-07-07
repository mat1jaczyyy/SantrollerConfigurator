using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum PickupSelectorType
{
    Chorus,
    [Description("Wah-wah")]
    WahWah,
    Flanger,
    Echo,
    None
}
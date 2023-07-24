using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum LegendType
{
    Xbox,
    [Description("PlayStation")]
    PlayStation,
    Switch
}
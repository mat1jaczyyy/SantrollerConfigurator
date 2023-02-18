using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum MouseMovementType
{
    [Description("Absolute Movement")] Absolute,
    [Description("Relative Movement")] Relative
}
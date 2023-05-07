using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum MultiplexerType
{
    [Description("4051 or other 8 channel multiplexer")]
    EightChannel,

    [Description("4067 or other 16 channel multiplexer")]
    SixteenChannel
}
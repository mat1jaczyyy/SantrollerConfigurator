using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum InputType
{
    AnalogPinInput,
    MultiplexerInput,
    DigitalPinInput,
    WiiInput,
    [Description("PS2 input")]
    Ps2Input,
    TurntableInput,
    WtNeckInput,
    [Description("GH5 neck input")]
    Gh5NeckInput,
    MacroInput,
    RfInput,
    UsbHostInput
}
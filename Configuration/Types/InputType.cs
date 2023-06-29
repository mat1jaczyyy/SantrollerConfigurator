using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum InputType
{
    AnalogPinInput,
    MultiplexerInput,
    DigitalPinInput,
    WiiInput,
    [Description("PS2 Input")] Ps2Input,
    TurntableInput,
    WtNeckInput,
    [Description("GH5 Neck Input")] Gh5NeckInput,
    MacroInput,
    [Description("USB Host Input")]
    UsbHostInput,
    BluetoothInput
}
using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum EmulationType
{
    Controller,
    [Description("Keyboard + Mouse")] KeyboardMouse,

    [Description("RF Controller Transmitter")]
    RfController,

    [Description("RF Keyboard + Mouse Transmitter")]
    RfKeyboardMouse,
    [Description("Bluetooth Controller")] Bluetooth,

    [Description("Bluetooth Keyboard + Mouse")]
    BluetoothKeyboardMouse
}
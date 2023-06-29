using System.ComponentModel;

namespace GuitarConfigurator.NetCore.Configuration.Types;

public enum EmulationType
{
    Controller,
    [Description("Keyboard + Mouse")] KeyboardMouse,

    [Description("Bluetooth Controller")] Bluetooth,

    [Description("Bluetooth Keyboard + Mouse")]
    BluetoothKeyboardMouse
}
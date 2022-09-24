using System;
using Avalonia.Input;
using Avalonia.Media;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Output;

public class KeyboardButton : OutputButton
{
    public KeyboardButton(IInput? input, Color ledOn, Color ledOff, int debounce, Key type) : base(input, ledOn, ledOff,
        debounce)
    {
        Key = type;
    }

    public override string Name => Key.ToString();

    //TODO: this
    public override string Image => Name;
    public Key Key;

    public override string GenerateIndex(bool xbox)
    {
        throw new NotImplementedException();
    }

    public override string GenerateOutput(bool xbox)
    {
        return "report->buttons";
    }

    // This gives us a function to go from key code to bit
    // https://github.com/qmk/qmk_firmware/blob/master/tmk_core/protocol/report.h
    // https://github.com/qmk/qmk_firmware/blob/master/tmk_core/protocol/report.c
    // https://github.com/qmk/qmk_firmware/blob/master/quantum/keycode.h
    public override bool IsStrum()
    {
        return false;
    }
}
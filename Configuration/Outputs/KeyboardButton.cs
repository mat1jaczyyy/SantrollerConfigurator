using System;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class KeyboardButton : OutputButton
{
    public KeyboardButton(ConfigViewModel model, IInput? input, Color ledOn, Color ledOff, int debounce, Key type) : base(model, input, ledOn, ledOff,
        debounce)
    {
        Key = type;
    }

    public override string Name => Key.ToString();
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
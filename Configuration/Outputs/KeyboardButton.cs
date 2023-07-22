using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public class KeyboardButton : OutputButton
{
    public static readonly Dictionary<Key, string> Keys = new()
    {
        {Key.LeftCtrl, "Left Control Key"},
        {Key.LeftAlt, "Left Alt Key"},
        {Key.LeftShift, "Left Shift Key"},
        {Key.LWin, "Left Windows Key"},
        {Key.RightCtrl, "Right Control Key"},
        {Key.RightAlt, "Right Alt Key"},
        {Key.RightShift, "Right Shift Key"},
        {Key.RWin, "Right Windows Key"},
        {Key.A, "A Key"},
        {Key.B, "B Key"},
        {Key.C, "C Key"},
        {Key.D, "D Key"},
        {Key.E, "E Key"},
        {Key.F, "F Key"},
        {Key.G, "G Key"},
        {Key.H, "H Key"},
        {Key.I, "I Key"},
        {Key.J, "J Key"},
        {Key.K, "K Key"},
        {Key.L, "L Key"},
        {Key.M, "M Key"},
        {Key.N, "N Key"},
        {Key.O, "O Key"},
        {Key.P, "P Key"},
        {Key.Q, "Q Key"},
        {Key.R, "R Key"},
        {Key.S, "S Key"},
        {Key.T, "T Key"},
        {Key.U, "U Key"},
        {Key.V, "V Key"},
        {Key.W, "W Key"},
        {Key.X, "X Key"},
        {Key.Y, "Y Key"},
        {Key.Z, "Z Key"},
        {Key.D0, "0 and ) Keys"},
        {Key.D1, "1 and ! Keys"},
        {Key.D2, "2 and @ Keys"},
        {Key.D3, "3 and # Keys"},
        {Key.D4, "4 and $ Keys"},
        {Key.D5, "5 and % Keys"},
        {Key.D6, "6 and % Keys"},
        {Key.D7, "7 and & Keys"},
        {Key.D8, "8 and * Keys"},
        {Key.D9, "9 and ( Keys"},
        {Key.Enter, "Enter Key"},
        {Key.Escape, "Escape Key"},
        {Key.Back, "Back Key"},
        {Key.Tab, "Tab Key"},
        {Key.Space, "Space Key"},
        {Key.OemMinus, "_ and - Keys"},
        {Key.OemPlus, "= and + Keys"},
        {Key.OemOpenBrackets, "{ and [ Keys"},
        {Key.OemCloseBrackets, "} and ] Keys"},
        {Key.OemPipe, "| and \\ Keys"},
        {Key.OemSemicolon, "; and : Keys"},
        {Key.OemQuotes, "' and \" Keys"},
        {Key.OemTilde, "~ and ` Keys"},
        {Key.OemComma, ", and < Keys"},
        {Key.OemPeriod, ". and > Keys"},
        {Key.OemQuestion, "/ and ? Keys"},
        {Key.CapsLock, "Caps Lock Key"},
        {Key.F1, "F1 Key"},
        {Key.F2, "F2 Key"},
        {Key.F3, "F3 Key"},
        {Key.F4, "F4 Key"},
        {Key.F5, "F5 Key"},
        {Key.F6, "F6 Key"},
        {Key.F7, "F7 Key"},
        {Key.F8, "F8 Key"},
        {Key.F9, "F9 Key"},
        {Key.F10, "F10 Key"},
        {Key.F11, "F11 Key"},
        {Key.F12, "F12 Key"},
        {Key.F13, "F13 Key"},
        {Key.F14, "F14 Key"},
        {Key.F15, "F15 Key"},
        {Key.F16, "F16 Key"},
        {Key.F17, "F17 Key"},
        {Key.F18, "F18 Key"},
        {Key.F19, "F19 Key"},
        {Key.F20, "F20 Key"},
        {Key.F21, "F21 Key"},
        {Key.F22, "F22 Key"},
        {Key.F23, "F23 Key"},
        {Key.F24, "F24 Key"},
        {Key.PrintScreen, "Print Screen Key"},
        {Key.Scroll, "Scroll Lock Key"},
        {Key.Pause, "Pause / Break Key"},
        {Key.Insert, "Insert Key"},
        {Key.Home, "Home Key"},
        {Key.PageUp, "Page Up Key"},
        {Key.PageDown, "Page Down Key"},
        {Key.Delete, "Delete Key"},
        {Key.End, "End Key"},
        {Key.Right, "Right Key"},
        {Key.Left, "Left Key"},
        {Key.Up, "Up Key"},
        {Key.Down, "Down Key"},
        {Key.NumLock, "Num Lock Key"},
        {Key.Divide, "NumPad / Key"},
        {Key.Multiply, "NumPad * Key"},
        {Key.Subtract, "NumPad - Key"},
        {Key.Add, "NumPad + Key"},
        {Key.NumPad0, "NumPad 0 Key"},
        {Key.NumPad1, "NumPad 1 Key"},
        {Key.NumPad2, "NumPad 2 Key"},
        {Key.NumPad3, "NumPad 3 Key"},
        {Key.NumPad4, "NumPad 4 Key"},
        {Key.NumPad5, "NumPad 5 Key"},
        {Key.NumPad6, "NumPad 6 Key"},
        {Key.NumPad7, "NumPad 7 Key"},
        {Key.NumPad8, "NumPad 8 Key"},
        {Key.NumPad9, "NumPad 9 Key"},
        {Key.Decimal, "NumPad . Key"},
        {Key.MediaNextTrack, "Next Track Key"},
        {Key.MediaPreviousTrack, "Previous Track Key"},
        {Key.MediaStop, "Stop Key"},
        {Key.MediaPlayPause, "Play / Pause Key"},
        {Key.VolumeMute, "Mute Key"},
        {Key.VolumeUp, "Volume Up Key"},
        {Key.VolumeDown, "Volume Down Key"}
    };

    public Key Key;

    public KeyboardButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce, Key type) : base(model, input, ledOn, ledOff, ledIndices,
        debounce, false)
    {
        Key = type;
        UpdateDetails();
    }

    public bool IsMediaKey => Key is Key.MediaStop or Key.MediaNextTrack or Key.MediaPlayPause or Key.VolumeDown
        or Key.VolumeMute or Key.VolumeUp;

    public override bool IsKeyboard => true;
    public virtual bool IsController => false;

    public override bool IsStrum => false;

    public override bool IsCombined => false;

    public override void UpdateBindings()
    {
    }

    public override string GetName(DeviceControllerType deviceControllerType)
    {
        return Keys.TryGetValue(Key, out var key) ? key : "";
    }

    public override object GetOutputType()
    {
        return Key;
    }

    public override string GenerateOutput(ConfigField mode)
    {
        switch (IsMediaKey)
        {
            case true when mode is not ConfigField.Consumer:
            case false when mode is not ConfigField.Keyboard:
                return "";
            default:
                return Key == Key.Delete ? GetReportField("Del") : GetReportField(Key);
        }
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedKeyboardButton(Input.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Key);
    }
}
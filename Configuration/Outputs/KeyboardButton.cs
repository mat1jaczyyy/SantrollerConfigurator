using System;
using System.Linq;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;
public class KeyboardButton : OutputButton
{
    private static readonly int SharedEpSize = 32;
    private static readonly int KeyboardReportBits = SharedEpSize - 2;

    enum HidKeyboardKeypadUsage
    {
        KcNo = 0x00,
        KcRollOver,
        KcPostFail,
        KcUndefined,
        KcA,
        KcB,
        KcC,
        KcD,
        KcE,
        KcF,
        KcG,
        KcH,
        KcI,
        KcJ,
        KcK,
        KcL,
        KcM, // 0x10
        KcN,
        KcO,
        KcP,
        KcQ,
        KcR,
        KcS,
        KcT,
        KcU,
        KcV,
        KcW,
        KcX,
        KcY,
        KcZ,
        Kc1,
        Kc2,
        Kc3, // 0x20
        Kc4,
        Kc5,
        Kc6,
        Kc7,
        Kc8,
        Kc9,
        Kc0,
        KcEnter,
        KcEscape,
        KcBackspace,
        KcTab,
        KcSpace,
        KcMinus,
        KcEqual,
        KcLeftBracket,
        KcRightBracket, // 0x30
        KcBackslash,
        KcNonusHash,
        KcSemicolon,
        KcQuote,
        KcGrave,
        KcComma,
        KcDot,
        KcSlash,
        KcCapsLock,
        KcF1,
        KcF2,
        KcF3,
        KcF4,
        KcF5,
        KcF6,
        KcF7, // 0x40
        KcF8,
        KcF9,
        KcF10,
        KcF11,
        KcF12,
        KcPrintScreen,
        KcScrollLock,
        KcPause,
        KcInsert,
        KcHome,
        KcPageUp,
        KcDelete,
        KcEnd,
        KcPageDown,
        KcRight,
        KcLeft, // 0x50
        KcDown,
        KcUp,
        KcNumLock,
        KcKpSlash,
        KcKpAsterisk,
        KcKpMinus,
        KcKpPlus,
        KcKpEnter,
        KcKp1,
        KcKp2,
        KcKp3,
        KcKp4,
        KcKp5,
        KcKp6,
        KcKp7,
        KcKp8, // 0x60
        KcKp9,
        KcKp0,
        KcKpDot,
        KcNonusBackslash,
        KcApplication,
        KcKbPower,
        KcKpEqual,
        KcF13,
        KcF14,
        KcF15,
        KcF16,
        KcF17,
        KcF18,
        KcF19,
        KcF20,
        KcF21, // 0x70
        KcF22,
        KcF23,
        KcF24,
        KcExecute,
        KcHelp,
        KcMenu,
        KcSelect,
        KcStop,
        KcAgain,
        KcUndo,
        KcCut,
        KcCopy,
        KcPaste,
        KcFind,
        KcKbMute,
        KcKbVolumeUp, // 0x80
        KcKbVolumeDown,
        KcLockingCapsLock,
        KcLockingNumLock,
        KcLockingScrollLock,
        KcKpComma,
        KcKpEqualAs400,
        KcInternational1,
        KcInternational2,
        KcInternational3,
        KcInternational4,
        KcInternational5,
        KcInternational6,
        KcInternational7,
        KcInternational8,
        KcInternational9,
        KcLanguage1, // 0x90
        KcLanguage2,
        KcLanguage3,
        KcLanguage4,
        KcLanguage5,
        KcLanguage6,
        KcLanguage7,
        KcLanguage8,
        KcLanguage9,
        KcAlternateErase,
        KcSystemRequest,
        KcCancel,
        KcClear,
        KcPrior,
        KcReturn,
        KcSeparator,
        KcOut, // 0xA0
        KcOper,
        KcClearAgain,
        KcCrsel,
        KcExsel,
        KcSystemPower = 0xA5,
        KcSystemSleep,
        KcSystemWake,

        /* Consumer Page (0x0C) */
        KcAudioMute,
        KcAudioVolUp,
        KcAudioVolDown,
        KcMediaNextTrack,
        KcMediaPrevTrack,
        KcMediaStop,
        KcMediaPlayPause,
        KcMediaSelect,
        KcMediaEject, // 0xB0
        KcMail,
        KcCalculator,
        KcMyComputer,
        KcWwwSearch,
        KcWwwHome,
        KcWwwBack,
        KcWwwForward,
        KcWwwStop,
        KcWwwRefresh,
        KcWwwFavorites,
        KcMediaFastForward,
        KcMediaRewind,
        KcBrightnessUp,
        KcBrightnessDown
    }
    

    public KeyboardButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices, byte debounce, Key type) : base(model, input, ledOn, ledOff, ledIndices,
        debounce, type.ToString())
    {
        Key = type;
    }

    public Key Key;

    public override string GenerateIndex(DeviceEmulationMode mode)
    {
        throw new NotImplementedException();
    }

    public override bool IsKeyboard => true;
    public override bool IsController => false;
    public override bool IsMidi => false;

    public override bool Valid => true;
    public override void UpdateBindings()
    {
    }

    public override string GenerateOutput(DeviceEmulationMode mode)
    {
        var code = 0;
        return $"report->keys[{code >> 3}] |= {1 << (code & 7)}";
    }

    // This gives us a function to go from key code to bit
    // https://github.com/qmk/qmk_firmware/blob/master/tmk_core/protocol/report.h
    // https://github.com/qmk/qmk_firmware/blob/master/tmk_core/protocol/report.c
    // https://github.com/qmk/qmk_firmware/blob/master/quantum/keycode.h

    public override bool IsStrum => false;

    public override bool IsCombined => false;

    public override SerializedOutput Serialize()
    {
        return new SerializedKeyboardButton(Input?.Serialise(), LedOn, LedOff, LedIndices.ToArray(), Debounce, Key);
    }
}
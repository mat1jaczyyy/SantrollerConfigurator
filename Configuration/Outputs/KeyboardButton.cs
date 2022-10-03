using System;
using Avalonia.Input;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public class KeyboardButton : OutputButton
{
    private static readonly int SharedEpSize = 32;
    private static readonly int KeyboardReportBits = SharedEpSize - 2;

    enum hid_keyboard_keypad_usage
    {
        KC_NO = 0x00,
        KC_ROLL_OVER,
        KC_POST_FAIL,
        KC_UNDEFINED,
        KC_A,
        KC_B,
        KC_C,
        KC_D,
        KC_E,
        KC_F,
        KC_G,
        KC_H,
        KC_I,
        KC_J,
        KC_K,
        KC_L,
        KC_M, // 0x10
        KC_N,
        KC_O,
        KC_P,
        KC_Q,
        KC_R,
        KC_S,
        KC_T,
        KC_U,
        KC_V,
        KC_W,
        KC_X,
        KC_Y,
        KC_Z,
        KC_1,
        KC_2,
        KC_3, // 0x20
        KC_4,
        KC_5,
        KC_6,
        KC_7,
        KC_8,
        KC_9,
        KC_0,
        KC_ENTER,
        KC_ESCAPE,
        KC_BACKSPACE,
        KC_TAB,
        KC_SPACE,
        KC_MINUS,
        KC_EQUAL,
        KC_LEFT_BRACKET,
        KC_RIGHT_BRACKET, // 0x30
        KC_BACKSLASH,
        KC_NONUS_HASH,
        KC_SEMICOLON,
        KC_QUOTE,
        KC_GRAVE,
        KC_COMMA,
        KC_DOT,
        KC_SLASH,
        KC_CAPS_LOCK,
        KC_F1,
        KC_F2,
        KC_F3,
        KC_F4,
        KC_F5,
        KC_F6,
        KC_F7, // 0x40
        KC_F8,
        KC_F9,
        KC_F10,
        KC_F11,
        KC_F12,
        KC_PRINT_SCREEN,
        KC_SCROLL_LOCK,
        KC_PAUSE,
        KC_INSERT,
        KC_HOME,
        KC_PAGE_UP,
        KC_DELETE,
        KC_END,
        KC_PAGE_DOWN,
        KC_RIGHT,
        KC_LEFT, // 0x50
        KC_DOWN,
        KC_UP,
        KC_NUM_LOCK,
        KC_KP_SLASH,
        KC_KP_ASTERISK,
        KC_KP_MINUS,
        KC_KP_PLUS,
        KC_KP_ENTER,
        KC_KP_1,
        KC_KP_2,
        KC_KP_3,
        KC_KP_4,
        KC_KP_5,
        KC_KP_6,
        KC_KP_7,
        KC_KP_8, // 0x60
        KC_KP_9,
        KC_KP_0,
        KC_KP_DOT,
        KC_NONUS_BACKSLASH,
        KC_APPLICATION,
        KC_KB_POWER,
        KC_KP_EQUAL,
        KC_F13,
        KC_F14,
        KC_F15,
        KC_F16,
        KC_F17,
        KC_F18,
        KC_F19,
        KC_F20,
        KC_F21, // 0x70
        KC_F22,
        KC_F23,
        KC_F24,
        KC_EXECUTE,
        KC_HELP,
        KC_MENU,
        KC_SELECT,
        KC_STOP,
        KC_AGAIN,
        KC_UNDO,
        KC_CUT,
        KC_COPY,
        KC_PASTE,
        KC_FIND,
        KC_KB_MUTE,
        KC_KB_VOLUME_UP, // 0x80
        KC_KB_VOLUME_DOWN,
        KC_LOCKING_CAPS_LOCK,
        KC_LOCKING_NUM_LOCK,
        KC_LOCKING_SCROLL_LOCK,
        KC_KP_COMMA,
        KC_KP_EQUAL_AS400,
        KC_INTERNATIONAL_1,
        KC_INTERNATIONAL_2,
        KC_INTERNATIONAL_3,
        KC_INTERNATIONAL_4,
        KC_INTERNATIONAL_5,
        KC_INTERNATIONAL_6,
        KC_INTERNATIONAL_7,
        KC_INTERNATIONAL_8,
        KC_INTERNATIONAL_9,
        KC_LANGUAGE_1, // 0x90
        KC_LANGUAGE_2,
        KC_LANGUAGE_3,
        KC_LANGUAGE_4,
        KC_LANGUAGE_5,
        KC_LANGUAGE_6,
        KC_LANGUAGE_7,
        KC_LANGUAGE_8,
        KC_LANGUAGE_9,
        KC_ALTERNATE_ERASE,
        KC_SYSTEM_REQUEST,
        KC_CANCEL,
        KC_CLEAR,
        KC_PRIOR,
        KC_RETURN,
        KC_SEPARATOR,
        KC_OUT, // 0xA0
        KC_OPER,
        KC_CLEAR_AGAIN,
        KC_CRSEL,
        KC_EXSEL,
        KC_SYSTEM_POWER = 0xA5,
        KC_SYSTEM_SLEEP,
        KC_SYSTEM_WAKE,

        /* Consumer Page (0x0C) */
        KC_AUDIO_MUTE,
        KC_AUDIO_VOL_UP,
        KC_AUDIO_VOL_DOWN,
        KC_MEDIA_NEXT_TRACK,
        KC_MEDIA_PREV_TRACK,
        KC_MEDIA_STOP,
        KC_MEDIA_PLAY_PAUSE,
        KC_MEDIA_SELECT,
        KC_MEDIA_EJECT, // 0xB0
        KC_MAIL,
        KC_CALCULATOR,
        KC_MY_COMPUTER,
        KC_WWW_SEARCH,
        KC_WWW_HOME,
        KC_WWW_BACK,
        KC_WWW_FORWARD,
        KC_WWW_STOP,
        KC_WWW_REFRESH,
        KC_WWW_FAVORITES,
        KC_MEDIA_FAST_FORWARD,
        KC_MEDIA_REWIND,
        KC_BRIGHTNESS_UP,
        KC_BRIGHTNESS_DOWN
    }
    

    public KeyboardButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, int debounce, Key type) : base(model, input, ledOn, ledOff,
        debounce, type.ToString())
    {
        Key = type;
    }

    public Key Key;

    public override string GenerateIndex(bool xbox)
    {
        throw new NotImplementedException();
    }

    public override string GenerateOutput(bool xbox)
    {
        int code = 0;
        return $"report->keys[{code >> 3}] |= {1 << (code & 7)}";
    }

    // This gives us a function to go from key code to bit
    // https://github.com/qmk/qmk_firmware/blob/master/tmk_core/protocol/report.h
    // https://github.com/qmk/qmk_firmware/blob/master/tmk_core/protocol/report.c
    // https://github.com/qmk/qmk_firmware/blob/master/quantum/keycode.h

    public override bool IsStrum => false;

    public override bool IsCombined => false;
}

using Avalonia.Media;
using GuitarConfiguratorSharp.Configuration;
using GuitarConfiguratorSharp.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class Ardwiino : ConfigurableUSBDevice
{

    private UInt32 cpuFreq;
    private Board board;
    private const int XBOX_BTN_COUNT = 16;
    private const int XBOX_AXIS_COUNT = 6;
    private const int XBOX_TRIGGER_COUNT = 2;

    private const ControllerAxis XBOX_WHAMMY = ControllerAxis.XBOX_R_X;
    private const ControllerAxis XBOX_TILT = ControllerAxis.XBOX_R_Y;
    private static readonly Version OLD_CPU_INFO_VERSION = new Version(8, 8, 4);

    private static readonly Version USB_CONTROL_REQUEST_API = new Version(4, 3, 7);

    public const ushort SERIAL_ARDWIINO_REVISION = 0x3122;

    // On 6.0.0 and above READ_CONFIG is 59
    // On 7.0.3 and above READ_CONFIG is 60
    // And with 8.0.7 and above READ_CONFIG is 62
    private const ushort CPU_INFO_COMMAND = 50;
    private const ushort JUMP_BOOTLOADER_COMMAND = 49;
    private const ushort JUMP_BOOTLOADER_COMMAND_UNO = 50;
    private const ushort READ_CONFIG_COMMAND = 62;
    private const ushort READ_CONFIG_PRE_8_0_7_COMMAND = 60;
    private const ushort READ_CONFIG_PRE_7_0_3_COMMAND = 59;
    private const byte REQUEST_HID_GET_REPORT = 0x01;
    private const byte REQUEST_HID_SET_REPORT = 0x09;

    public override bool MigrationSupported { get; }
    private DeviceConfiguration _config;

    public override DeviceConfiguration Configuration => this._config;

    public const byte NOT_USED = 0xFF;

    public Ardwiino(PlatformIO pio, Device.Net.IDevice device, string product, string serial, ushort version_number) : base(device, product, serial, version_number)
    {
        if (this.version < new Version(6, 0, 0))
        {
            var buffer = this.ReadData(6, REQUEST_HID_GET_REPORT);
            this.cpuFreq = uint.Parse(StructTools.RawDeserializeStr(buffer));
            buffer = this.ReadData(7, REQUEST_HID_GET_REPORT);
            string board = StructTools.RawDeserializeStr(buffer);
            this.board = Board.findBoard(board, this.cpuFreq);
            this.MigrationSupported = false;
            _config = new DeviceConfiguration(Board.findMicrocontroller(this.board));
            return;
        }
        this.MigrationSupported = true;
        // Version 6.0.0 started at config version 6, so we don't have to support anything earlier than that
        byte[] data = this.ReadData(CPU_INFO_COMMAND, 1);
        if (this.version < OLD_CPU_INFO_VERSION)
        {
            CpuInfoOld info = StructTools.RawDeserialize<CpuInfoOld>(data, 0);
            this.cpuFreq = info.cpu_freq;
            this.board = Board.findBoard(info.board, this.cpuFreq);
        }
        else
        {
            CpuInfo info = StructTools.RawDeserialize<CpuInfo>(data, 0);
            this.cpuFreq = info.cpu_freq;
            this.board = Board.findBoard(info.board, this.cpuFreq);
        }
        var read_config = READ_CONFIG_COMMAND;
        if (this.version < new Version(8, 0, 7))
        {
            read_config = READ_CONFIG_PRE_8_0_7_COMMAND;
        }
        else if (this.version < new Version(7, 0, 3))
        {
            read_config = READ_CONFIG_PRE_7_0_3_COMMAND;
        }
        data = new byte[Marshal.SizeOf(typeof(ArdwiinoConfiguration))];
        int sizeOfAll = Marshal.SizeOf(typeof(FullArdwiinoConfiguration));
        int offset = 0;
        int offsetId = 0;
        int maxSize = data.Length;
        uint version = 0;
        // Set the defaults for things that arent in every version
        ArdwiinoConfiguration config = new ArdwiinoConfiguration();
        config.neck = new NeckConfig();
        config.axisScale = new AxisScaleConfig();
        config.axisScale.axis = new AxisScale[XBOX_AXIS_COUNT];
        foreach (int axis in Enum.GetValues(typeof(ControllerAxis)))
        {
            config.axisScale.axis[axis].multiplier = 1;
            config.axisScale.axis[axis].offset = Int16.MinValue;
            config.axisScale.axis[axis].deadzone = Int16.MaxValue;
        }
        config.debounce.buttons = 5;
        config.debounce.strum = 20;
        config.debounce.combinedStrum = 0;
        config.rf.id = 0;
        config.rf.rfInEnabled = 0;
        while (offset < maxSize)
        {
            var data2 = ReadData((ushort)(read_config + offsetId), REQUEST_HID_GET_REPORT);
            Array.Copy(data2, 0, data, offset, data2.Length);
            offset += data2.Length;
            offsetId++;
            if (offset > sizeOfAll)
            {
                config.all = StructTools.RawDeserialize<FullArdwiinoConfiguration>(data, 0);
                version = config.all.main.version;
                if (version > 13)
                {
                    maxSize = Marshal.SizeOf(typeof(Configuration14));
                }
                else if (version > 12)
                {
                    maxSize = Marshal.SizeOf(typeof(Configuration13));
                }
                else if (version > 11)
                {
                    maxSize = Marshal.SizeOf(typeof(Configuration12));
                }
                else if (version > 10)
                {
                    maxSize = Marshal.SizeOf(typeof(Configuration11));
                }
                else if (version > 8)
                {
                    maxSize = Marshal.SizeOf(typeof(Configuration10));
                }
                else if (version == 8)
                {
                    maxSize = Marshal.SizeOf(typeof(Configuration8));
                }
                else
                {
                    maxSize = sizeOfAll;
                }
            }
        }
        // Patches to all
        if (version < 9)
        {
            // For versions below version 9, r_x is inverted from how we use it now
            config.all.pins.axis![((byte)ControllerAxis.XBOX_R_X)].inverted = (byte)(config.all.pins.axis[(int)ControllerAxis.XBOX_R_X].inverted == 0 ? 1 : 0);
        }
        else if (version < 14)
        {
            // version 14 changed mpu6050Orientation from accepting 0-5 to 0-2
            switch (config.all.axis.mpu6050Orientation)
            {
                case (byte)GyroOrientationOld.NEGATIVE_X:
                case (byte)GyroOrientationOld.POSITIVE_X:
                    config.all.axis.mpu6050Orientation = (byte)GyroOrientation.X;
                    break;
                case (byte)GyroOrientationOld.NEGATIVE_Y:
                case (byte)GyroOrientationOld.POSITIVE_Y:
                    config.all.axis.mpu6050Orientation = (byte)GyroOrientation.Y;
                    break;
                case (byte)GyroOrientationOld.NEGATIVE_Z:
                case (byte)GyroOrientationOld.POSITIVE_Z:
                    config.all.axis.mpu6050Orientation = (byte)GyroOrientation.Z;
                    break;
            }
        }
        // Read in the rest of the data, in the format that it is in
        if (version == 16)
        {
            config = StructTools.RawDeserialize<ArdwiinoConfiguration>(data, 0);
        }
        else if (version > 13)
        {
            var config_old = StructTools.RawDeserialize<Configuration14>(data, 0);
            config.axisScale = config_old.axisScale;
            config.pinsSP = config_old.pinsSP;
            config.rf = config_old.rf;
            config.debounce = config_old.debounce;
        }
        else if (version > 12)
        {
            var config_old = StructTools.RawDeserialize<Configuration13>(data, 0);
            config.axisScale = config_old.axisScale;
            config.pinsSP = config_old.pinsSP;
            config.rf = config_old.rf;
            config.debounce.buttons = config_old.debounce.buttons;
            config.debounce.strum = config_old.debounce.strum;
            config.debounce.combinedStrum = 0;
        }
        else if (version > 11)
        {
            var config_old = StructTools.RawDeserialize<Configuration12>(data, 0);
            foreach (int axis in Enum.GetValues(typeof(ControllerAxis)))
            {
                config.axisScale.axis[axis].multiplier = config_old.axisScale.axis[axis].multiplier;
                config.axisScale.axis[axis].offset = config_old.axisScale.axis[axis].offset;
                config.axisScale.axis[axis].deadzone = Int16.MaxValue;
            }
            config.pinsSP = config_old.pinsSP;
            config.rf = config_old.rf;
            config.debounce.buttons = 5;
            config.debounce.strum = 20;
            config.debounce.combinedStrum = 0;
        }
        else if (version > 10)
        {
            var config_old = StructTools.RawDeserialize<Configuration11>(data, 0);
            config.axisScale.axis[(int)ControllerAxis.XBOX_R_X].multiplier = config_old.whammy.multiplier;
            config.axisScale.axis[(int)ControllerAxis.XBOX_R_X].offset = (short)config_old.whammy.offset;
            config.pinsSP = config_old.pinsSP;
            config.rf = config_old.rf;
        }
        else if (version > 8)
        {
            var config_old = StructTools.RawDeserialize<Configuration10>(data, 0);
            config.pinsSP = config_old.pinsSP;
            config.rf = config_old.rf;
        }
        Microcontroller controller = Board.findMicrocontroller(board);
        List<Binding> bindings = new List<Binding>();
        Dictionary<int, Color> colors = new Dictionary<int, Color>();
        foreach (var led in config!.all!.leds!)
        {
            if (led.pin != 0)
            {
                colors[led.pin - 1] = Color.FromRgb(led.red, led.green, led.blue);
            }
        }
        LedType ledType = LedType.None;
        DeviceType deviceType = DeviceType.Guitar;
        EmulationType emulationType = EmulationType.Universal;
        InputControllerType inputControllerType = (InputControllerType)config.all.main.inputType;
        RhythmType rhythmType = RhythmType.GuitarHero;
        TiltType tiltType = (TiltType)config.all.main.tiltType;
        TiltOrientation tiltOrientation = (TiltOrientation)config.all.axis.mpu6050Orientation;
        if (config.all.main.fretLEDMode == 2)
        {
            ledType = LedType.APA102;
        }
        if ((config.all.main.subType >= (int)SubType.KEYBOARD_GAMEPAD && config.all.main.subType <= (int)SubType.KEYBOARD_ROCK_BAND_DRUMS) || config.all.main.subType == (int)SubType.MOUSE)
        {
            emulationType = EmulationType.Keyboard_Mouse;
        }
        if (config.all.main.subType >= (int)SubType.MIDI_GAMEPAD)
        {
            emulationType = EmulationType.Midi;
        }
        switch ((SubType)config.all.main.subType)
        {
            case SubType.XINPUT_GAMEPAD:
            case SubType.PS3_GAMEPAD:
            case SubType.SWITCH_GAMEPAD:
            case SubType.MIDI_GAMEPAD:
            case SubType.KEYBOARD_GAMEPAD:
                deviceType = DeviceType.Gamepad;
                break;
            case SubType.MIDI_LIVE_GUITAR:
            case SubType.XINPUT_LIVE_GUITAR:
            case SubType.KEYBOARD_LIVE_GUITAR:
                deviceType = DeviceType.Guitar;
                rhythmType = RhythmType.Live;
                break;
            case SubType.PS3_ROCK_BAND_DRUMS:
            case SubType.WII_ROCK_BAND_DRUMS:
            case SubType.MIDI_ROCK_BAND_DRUMS:
            case SubType.XINPUT_ROCK_BAND_DRUMS:
            case SubType.KEYBOARD_ROCK_BAND_DRUMS:
                deviceType = DeviceType.Drum;
                rhythmType = RhythmType.RockBand;
                break;
            case SubType.PS3_GUITAR_HERO_DRUMS:
            case SubType.MIDI_GUITAR_HERO_DRUMS:
            case SubType.XINPUT_GUITAR_HERO_DRUMS:
            case SubType.KEYBOARD_GUITAR_HERO_DRUMS:
                deviceType = DeviceType.Drum;
                rhythmType = RhythmType.GuitarHero;
                break;
            case SubType.PS3_ROCK_BAND_GUITAR:
            case SubType.WII_ROCK_BAND_GUITAR:
            case SubType.MIDI_ROCK_BAND_GUITAR:
            case SubType.XINPUT_ROCK_BAND_GUITAR:
            case SubType.KEYBOARD_ROCK_BAND_GUITAR:
                deviceType = DeviceType.Guitar;
                rhythmType = RhythmType.RockBand;
                break;
            case SubType.PS3_GUITAR_HERO_GUITAR:
            case SubType.MIDI_GUITAR_HERO_GUITAR:
            case SubType.XINPUT_GUITAR_HERO_GUITAR:
            case SubType.KEYBOARD_GUITAR_HERO_GUITAR:
                deviceType = DeviceType.Guitar;
                rhythmType = RhythmType.GuitarHero;
                break;
            default:
                deviceType = DeviceType.Gamepad;
                break;

        }
        // TODO: Handle necks
        if (config.all.main.inputType == (int)InputControllerType.Direct)
        {
            foreach (int axis in Enum.GetValues(typeof(ControllerAxis)))
            {
                var pin = config.all.pins.axis![axis];
                if (pin.pin == NOT_USED)
                {
                    continue;
                }
                var gen_axis = AXIS_TO_STANDARD[(ControllerAxis)axis];
                var scale = config.axisScale.axis[axis];
                var isTrigger = axis == (int)ControllerAxis.XBOX_LT || axis == (int)ControllerAxis.XBOX_RT;

                Color on = Color.FromRgb(0, 0, 0);
                if (colors.ContainsKey(axis + XBOX_BTN_COUNT))
                {
                    on = colors[axis + XBOX_BTN_COUNT];
                }
                Color off = Color.FromRgb(0, 0, 0);
                if (deviceType == DeviceType.Guitar && (ControllerAxis)axis == XBOX_WHAMMY)
                {
                    isTrigger = true;
                }
                if (deviceType == DeviceType.Guitar && (ControllerAxis)axis == XBOX_TILT)
                {
                    bindings.Add(new DigitalToAnalog(controller, 32767, new DirectDigital(controller, DevicePinMode.VCC, pin.pin, config.debounce.buttons, new GenericControllerButton(StandardButtonType.A), on, off), AnalogToDigitalType.Trigger, new GenericAxis(StandardAxisType.RightStickY), on, off, 1, 1, 1, true));
                }
                else
                {
                    bindings.Add(new DirectAnalog(controller, pin.pin, new GenericAxis(gen_axis), on, off, (scale.multiplier / 1024.0f) * (isTrigger ? 2 : 1) * (pin.inverted > 0 ? -1 : 1), ((isTrigger ? 0 : 32670) + scale.offset) >> 8, ((isTrigger ? 32768 : 0) + scale.deadzone) >> 8, isTrigger));
                }
            }
            foreach (int button in Enum.GetValues(typeof(ControllerButtons)))
            {
                var pin = config.all.pins.pins![button];
                if (pin == NOT_USED)
                {
                    continue;
                }
                Color on = Color.FromRgb(0, 0, 0);
                if (colors.ContainsKey(button))
                {
                    on = colors[button];
                }
                Color off = Color.FromRgb(0, 0, 0);
                var gen_button = BUTTON_TO_STANDARD[(ControllerButtons)button];
                var pinMode = DevicePinMode.VCC;
                if (config.all.main.fretLEDMode == 1 && deviceType == DeviceType.Guitar && FRETS.Contains(gen_button))
                {
                    pinMode = DevicePinMode.Floating;
                }
                var debounce = config.debounce.buttons;
                if (deviceType == DeviceType.Guitar && (gen_button == StandardButtonType.Up || gen_button == StandardButtonType.Down))
                {
                    debounce = config.debounce.strum;
                }
                OutputButton output = HAT.Contains(gen_button) ? new GenericControllerHat(gen_button) : new GenericControllerButton(gen_button);
                bindings.Add(new DirectDigital(controller, pinMode, pin, debounce, output, on, off));
            }
        }
        else if (config.all.main.inputType == (int)InputControllerType.Wii)
        {
            // TODO: once we have support for layouts, this will just load some wii layout
            // TODO: respecting "mapAccelToLeftJoy and other possible settings
        }
        else if (config.all.main.inputType == (int)InputControllerType.PS2)
        {
            // TODO: once we have support for layouts, this will just load some ps2 layout
        }
        if (config.all.main.mapLeftJoystickToDPad > 0)
        {
            var lx = bindings.FilterCast<Binding, Axis>().First(axis => axis.Type.Type == StandardAxisType.LeftStickX);
            var ly = bindings.FilterCast<Binding, Axis>().First(axis => axis.Type.Type == StandardAxisType.LeftStickY);
            var onlx = lx.LedOn;
            var offlx = lx.LedOff;
            var only = ly.LedOn;
            var offly = ly.LedOff;
            bindings.Add(new AnalogToDigital(controller, config.all.axis.joyThreshold, lx, AnalogToDigitalType.JoyLow, config.debounce.buttons, new GenericControllerHat(StandardButtonType.Left), onlx, offlx));
            bindings.Add(new AnalogToDigital(controller, config.all.axis.joyThreshold, lx, AnalogToDigitalType.JoyHigh, config.debounce.buttons, new GenericControllerHat(StandardButtonType.Right), onlx, offlx));
            bindings.Add(new AnalogToDigital(controller, config.all.axis.joyThreshold, ly, AnalogToDigitalType.JoyLow, config.debounce.buttons, new GenericControllerHat(StandardButtonType.Down), only, offly));
            bindings.Add(new AnalogToDigital(controller, config.all.axis.joyThreshold, ly, AnalogToDigitalType.JoyHigh, config.debounce.buttons, new GenericControllerHat(StandardButtonType.Up), only, offly));
        }
        _config = new DeviceConfiguration(controller, bindings, ledType, deviceType, emulationType, rhythmType, tiltType == TiltType.Digital_Mercury);
        _config.generate(pio);
    }

    public override String ToString()
    {
        return $"Ardwiino - {board.name} - {version}";
    }

    public override void bootloader()
    {
        WriteData(JUMP_BOOTLOADER_COMMAND, REQUEST_HID_SET_REPORT, new byte[0]);
    }

    public override void bootloaderUSB()
    {
        WriteData(JUMP_BOOTLOADER_COMMAND_UNO, REQUEST_HID_SET_REPORT, new byte[0]);
    }

    enum GyroOrientation
    {
        X, Y, Z
    };

    enum SubType
    {
        XINPUT_GAMEPAD = 1,
        XINPUT_WHEEL,
        XINPUT_ARCADE_STICK,
        XINPUT_FLIGHT_STICK,
        XINPUT_DANCE_PAD,
        XINPUT_LIVE_GUITAR = 9,
        XINPUT_ROCK_BAND_DRUMS = 12,
        XINPUT_GUITAR_HERO_DRUMS,
        XINPUT_ROCK_BAND_GUITAR,
        XINPUT_GUITAR_HERO_GUITAR,
        XINPUT_ARCADE_PAD = 19,
        KEYBOARD_GAMEPAD,
        KEYBOARD_GUITAR_HERO_GUITAR,
        KEYBOARD_ROCK_BAND_GUITAR,
        KEYBOARD_LIVE_GUITAR,
        KEYBOARD_GUITAR_HERO_DRUMS,
        KEYBOARD_ROCK_BAND_DRUMS,
        SWITCH_GAMEPAD,
        PS3_GUITAR_HERO_GUITAR,
        PS3_GUITAR_HERO_DRUMS,
        PS3_ROCK_BAND_GUITAR,
        PS3_ROCK_BAND_DRUMS,
        PS3_GAMEPAD,
        WII_ROCK_BAND_GUITAR,
        WII_ROCK_BAND_DRUMS,
        MOUSE,
        MIDI_GAMEPAD,
        MIDI_GUITAR_HERO_GUITAR,
        MIDI_ROCK_BAND_GUITAR,
        MIDI_LIVE_GUITAR,
        MIDI_GUITAR_HERO_DRUMS,
        MIDI_ROCK_BAND_DRUMS
    };

    enum ControllerButtons : int
    {
        XBOX_DPAD_UP,
        XBOX_DPAD_DOWN,
        XBOX_DPAD_LEFT,
        XBOX_DPAD_RIGHT,
        XBOX_START,
        XBOX_BACK,
        XBOX_LEFT_STICK,
        XBOX_RIGHT_STICK,

        XBOX_LB,
        XBOX_RB,
        XBOX_HOME,
        XBOX_UNUSED,
        XBOX_A,
        XBOX_B,
        XBOX_X,
        XBOX_Y,
    };

    enum ControllerAxis : int
    {
        XBOX_LT,
        XBOX_RT,
        XBOX_L_X,
        XBOX_L_Y,
        XBOX_R_X,
        XBOX_R_Y
    };

    private static Dictionary<ControllerAxis, StandardAxisType> AXIS_TO_STANDARD = new Dictionary<ControllerAxis, StandardAxisType>() {
        {ControllerAxis.XBOX_L_X,StandardAxisType.LeftStickX},
        {ControllerAxis.XBOX_L_Y,StandardAxisType.LeftStickY},
        {ControllerAxis.XBOX_R_X,StandardAxisType.RightStickX},
        {ControllerAxis.XBOX_R_Y,StandardAxisType.RightStickY},
        {ControllerAxis.XBOX_LT, StandardAxisType.LeftTrigger},
        {ControllerAxis.XBOX_RT,StandardAxisType.RightTrigger}
    };

    private static Dictionary<ControllerButtons, StandardButtonType> BUTTON_TO_STANDARD = new Dictionary<ControllerButtons, StandardButtonType>(){
            {ControllerButtons.XBOX_DPAD_UP,StandardButtonType.Up},
            {ControllerButtons.XBOX_DPAD_DOWN,StandardButtonType.Down},
            {ControllerButtons.XBOX_DPAD_LEFT,StandardButtonType.Left},
            {ControllerButtons.XBOX_DPAD_RIGHT,StandardButtonType.Right},
            {ControllerButtons.XBOX_START,StandardButtonType.Start},
            {ControllerButtons.XBOX_BACK,StandardButtonType.Select},
            {ControllerButtons.XBOX_LEFT_STICK,StandardButtonType.LeftStick},
            {ControllerButtons.XBOX_RIGHT_STICK,StandardButtonType.RightStick},
            {ControllerButtons.XBOX_LB,StandardButtonType.LB},
            {ControllerButtons.XBOX_RB,StandardButtonType.RB},
            {ControllerButtons.XBOX_HOME,StandardButtonType.Home},
            {ControllerButtons.XBOX_UNUSED,StandardButtonType.Capture},
            {ControllerButtons.XBOX_A,StandardButtonType.A},
            {ControllerButtons.XBOX_B,StandardButtonType.B},
            {ControllerButtons.XBOX_X,StandardButtonType.X},
            {ControllerButtons.XBOX_Y,StandardButtonType.Y}
    };

    private static IEnumerable<StandardButtonType> HAT = new List<StandardButtonType>() { StandardButtonType.Left, StandardButtonType.Right, StandardButtonType.Up, StandardButtonType.Down };

    List<StandardButtonType> FRETS = new List<StandardButtonType>() { StandardButtonType.A, StandardButtonType.B, StandardButtonType.X, StandardButtonType.Y, StandardButtonType.LB, StandardButtonType.RB };
    enum GyroOrientationOld : int
    {
        POSITIVE_Z,
        NEGATIVE_Z,
        POSITIVE_Y,
        NEGATIVE_Y,
        POSITIVE_X,
        NEGATIVE_X
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CpuInfoOld
    {
        public UInt32 cpu_freq;
        public byte multi;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string board;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CpuInfo
    {
        public UInt32 cpu_freq;
        public byte multi;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string board;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Led
    {
        public byte pin;
        public byte red;
        public byte green;
        public byte blue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Config
    {
        public byte inputType;
        public byte subType;
        public byte tiltType;
        public byte pollRate;
        public byte fretLEDMode;
        public byte mapLeftJoystickToDPad;
        public byte mapStartSelectToHome;
        public byte mapNunchukAccelToRightJoy;
        public UInt32 signature;
        public UInt32 version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AnalogPin
    {
        public byte pin;
        public byte inverted;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Pins
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_BTN_COUNT)]
        public byte[] pins;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_AXIS_COUNT)]
        public AnalogPin[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AnalogKey
    {
        public byte neg;
        public byte pos;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Keys
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_BTN_COUNT + XBOX_TRIGGER_COUNT)]
        public byte[] pins;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_AXIS_COUNT - XBOX_TRIGGER_COUNT)]
        public AnalogKey[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MainConfig
    {
        public byte inputType;
        public byte subType;
        public byte tiltType;
        public byte pollRate;
        public byte fretLEDMode;
        public byte mapLeftJoystickToDPad;
        public byte mapStartSelectToHome;
        public byte mapNunchukAccelToRightJoy;
        public UInt32 signature;
        public UInt32 version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AxisConfig
    {
        public byte triggerThreshold;
        public byte joyThreshold;
        public byte drumThreshold;

        public byte mpu6050Orientation;
        public Int16 tiltSensitivity;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MidiConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_AXIS_COUNT + XBOX_BTN_COUNT)]
        public byte[] type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_AXIS_COUNT + XBOX_BTN_COUNT)]
        public byte[] note;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_AXIS_COUNT + XBOX_BTN_COUNT)]
        public byte[] channel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RFConfig
    {
        public byte rfInEnabled;
        public UInt32 id;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AxisScale
    {
        public Int16 multiplier;
        public Int16 offset;
        public Int16 deadzone;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AxisScale12
    {
        public Int16 multiplier;
        public Int16 offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Version11AxisWhammyConfig
    {
        public byte multiplier;
        public UInt16 offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AxisScaleConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_AXIS_COUNT)]
        public AxisScale[] axis;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AxisScaleConfig12
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_AXIS_COUNT)]
        public AxisScale12[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DebounceConfig13
    {
        public byte buttons;
        public byte strum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DebounceConfig
    {
        public byte buttons;
        public byte strum;
        public byte combinedStrum;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ArdwiinoConfiguration
    {
        public FullArdwiinoConfiguration all;
        public RFConfig rf;
        public byte pinsSP;
        public AxisScaleConfig axisScale;
        public DebounceConfig debounce;
        public NeckConfig neck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration14
    {
        public FullArdwiinoConfiguration all;
        public RFConfig rf;
        public byte pinsSP;
        public AxisScaleConfig axisScale;
        public DebounceConfig debounce;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration13
    {
        public FullArdwiinoConfiguration all;
        public RFConfig rf;
        public byte pinsSP;
        public AxisScaleConfig axisScale;
        public DebounceConfig13 debounce;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration12
    {
        public FullArdwiinoConfiguration all;
        public RFConfig rf;
        public byte pinsSP;
        public AxisScaleConfig12 axisScale;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration11
    {
        public FullArdwiinoConfiguration all;
        public RFConfig rf;
        public byte pinsSP;

        public Version11AxisWhammyConfig whammy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration10
    {

        public FullArdwiinoConfiguration all;
        public RFConfig rf;
        public byte pinsSP;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration8
    {
        public FullArdwiinoConfiguration all;
        public RFConfig rf;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NeckConfig
    {
        bool wtNeck;
        bool gh5Neck;
        bool gh5NeckBar;
        bool wiiNeck;
        bool ps2Neck;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FullArdwiinoConfiguration
    {
        public MainConfig main;
        public Pins pins;
        public AxisConfig axis;
        public Keys keys;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XBOX_AXIS_COUNT + XBOX_BTN_COUNT)]
        public Led[] leds;
        public MidiConfig midi;
    }
}
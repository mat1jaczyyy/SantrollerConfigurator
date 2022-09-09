using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;
using GuitarConfiguratorSharp.NetCore.Utils;
using LibUsbDotNet;

namespace GuitarConfiguratorSharp.NetCore;

public class Ardwiino : ConfigurableUsbDevice
{

    private readonly UInt32 _cpuFreq;
    private const int XboxBtnCount = 16;
    private const int XboxAxisCount = 6;
    private const int XboxTriggerCount = 2;

    private const ControllerAxis XboxWhammy = ControllerAxis.XBOX_R_X;
    private const ControllerAxis XboxTilt = ControllerAxis.XBOX_R_Y;
    private static readonly Version OldCpuInfoVersion = new Version(8, 8, 4);

    private static readonly Version UsbControlRequestApi = new Version(4, 3, 7);

    public const ushort SERIAL_ARDWIINO_REVISION = 0x3122;
    // public static readonly FilterDeviceDefinition ArdwiinoDeviceFilter = new(label: "Ardwiino", classGuid: Santroller.ControllerGUID);

    // On 6.0.0 and above READ_CONFIG is 59
    // On 7.0.3 and above READ_CONFIG is 60
    // And with 8.0.7 and above READ_CONFIG is 62
    private const ushort CpuInfoCommand = 50;
    private const ushort JumpBootloaderCommand = 49;
    private const ushort JumpBootloaderCommandUno = 50;
    private const ushort ReadConfigCommand = 62;
    private const ushort ReadConfigPre807Command = 60;
    private const ushort ReadConfigPre703Command = 59;
    private const byte RequestHidGetReport = 0x01;
    private const byte RequestHidSetReport = 0x09;

    public override bool MigrationSupported { get; }
    private readonly DeviceConfiguration _config;

    public override DeviceConfiguration Configuration => this._config;

    private const byte NOT_USED = 0xFF;

    private readonly bool _failed = false;

    public Ardwiino(PlatformIo pio, string path, UsbDevice device, string product, string serial, ushort versionNumber) : base(device, path, product, serial, versionNumber)
    {
        try
        {
            if (this.version < new Version(6, 0, 0))
            {
                var buffer = this.ReadData(6, RequestHidGetReport);
                this._cpuFreq = uint.Parse(StructTools.RawDeserializeStr(buffer));
                buffer = this.ReadData(7, RequestHidGetReport);
                string board = StructTools.RawDeserializeStr(buffer);
                this.Board = Board.FindBoard(board, this._cpuFreq);
                this.MigrationSupported = false;
                _config = new DeviceConfiguration(Board.FindMicrocontroller(this.Board));
                return;
            }
            this.MigrationSupported = true;
            // Version 6.0.0 started at config version 6, so we don't have to support anything earlier than that
            byte[] data = this.ReadData(CpuInfoCommand, 1);
            if (this.version < OldCpuInfoVersion)
            {
                CpuInfoOld info = StructTools.RawDeserialize<CpuInfoOld>(data, 0);
                this._cpuFreq = info.cpu_freq;
                this.Board = Board.FindBoard(info.board, this._cpuFreq);
            }
            else
            {
                CpuInfo info = StructTools.RawDeserialize<CpuInfo>(data, 0);
                this._cpuFreq = info.cpu_freq;
                this.Board = Board.FindBoard(info.board, this._cpuFreq);
            }
            var readConfig = ReadConfigCommand;
            if (this.version < new Version(8, 0, 7))
            {
                readConfig = ReadConfigPre807Command;
            }
            else if (this.version < new Version(7, 0, 3))
            {
                readConfig = ReadConfigPre703Command;
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
            config.axisScale.axis = new AxisScale[XboxAxisCount];
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
                var data2 = ReadData((ushort)(readConfig + offsetId), RequestHidGetReport);
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
                var configOld = StructTools.RawDeserialize<Configuration14>(data, 0);
                config.axisScale = configOld.axisScale;
                config.pinsSP = configOld.pinsSP;
                config.rf = configOld.rf;
                config.debounce = configOld.debounce;
            }
            else if (version > 12)
            {
                var configOld = StructTools.RawDeserialize<Configuration13>(data, 0);
                config.axisScale = configOld.axisScale;
                config.pinsSP = configOld.pinsSP;
                config.rf = configOld.rf;
                config.debounce.buttons = configOld.debounce.buttons;
                config.debounce.strum = configOld.debounce.strum;
                config.debounce.combinedStrum = 0;
            }
            else if (version > 11)
            {
                var configOld = StructTools.RawDeserialize<Configuration12>(data, 0);
                foreach (int axis in Enum.GetValues(typeof(ControllerAxis)))
                {
                    config.axisScale.axis[axis].multiplier = configOld.axisScale.axis[axis].multiplier;
                    config.axisScale.axis[axis].offset = configOld.axisScale.axis[axis].offset;
                    config.axisScale.axis[axis].deadzone = Int16.MaxValue;
                }
                config.pinsSP = configOld.pinsSP;
                config.rf = configOld.rf;
                config.debounce.buttons = 5;
                config.debounce.strum = 20;
                config.debounce.combinedStrum = 0;
            }
            else if (version > 10)
            {
                var configOld = StructTools.RawDeserialize<Configuration11>(data, 0);
                config.axisScale.axis[(int)ControllerAxis.XBOX_R_X].multiplier = configOld.whammy.multiplier;
                config.axisScale.axis[(int)ControllerAxis.XBOX_R_X].offset = (short)configOld.whammy.offset;
                config.pinsSP = configOld.pinsSP;
                config.rf = configOld.rf;
            }
            else if (version > 8)
            {
                var configOld = StructTools.RawDeserialize<Configuration10>(data, 0);
                config.pinsSP = configOld.pinsSP;
                config.rf = configOld.rf;
            }
            Microcontroller controller = Board.FindMicrocontroller(Board);
            List<Binding> bindings = new List<Binding>();
            Dictionary<int, Color> colors = new Dictionary<int, Color>();
            foreach (var led in config.all!.leds!)
            {
                if (led.pin != 0)
                {
                    colors[led.pin - 1] = Color.FromRgb(led.red, led.green, led.blue);
                }
            }
            LedType ledType = LedType.None;
            DeviceControllerType deviceType = DeviceControllerType.Guitar;
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
                emulationType = EmulationType.KeyboardMouse;
            }
            if (config.all.main.subType >= (int)SubType.MIDI_GAMEPAD)
            {
                emulationType = EmulationType.Midi;
            }
            bool xinputOnWindows = false;
            switch ((SubType)config.all.main.subType)
            {
                case SubType.XINPUT_GAMEPAD:
                case SubType.XINPUT_LIVE_GUITAR:
                case SubType.XINPUT_ROCK_BAND_DRUMS:
                case SubType.XINPUT_GUITAR_HERO_DRUMS:
                case SubType.XINPUT_ROCK_BAND_GUITAR:
                case SubType.XINPUT_GUITAR_HERO_GUITAR:
                    xinputOnWindows = true;
                    break;
            }
            switch ((SubType)config.all.main.subType)
            {
                case SubType.XINPUT_GAMEPAD:
                case SubType.PS3_GAMEPAD:
                case SubType.SWITCH_GAMEPAD:
                case SubType.MIDI_GAMEPAD:
                case SubType.KEYBOARD_GAMEPAD:
                    deviceType = DeviceControllerType.Gamepad;
                    break;
                case SubType.MIDI_LIVE_GUITAR:
                case SubType.XINPUT_LIVE_GUITAR:
                case SubType.KEYBOARD_LIVE_GUITAR:
                    deviceType = DeviceControllerType.Guitar;
                    rhythmType = RhythmType.Live;
                    break;
                case SubType.PS3_ROCK_BAND_DRUMS:
                case SubType.WII_ROCK_BAND_DRUMS:
                case SubType.MIDI_ROCK_BAND_DRUMS:
                case SubType.XINPUT_ROCK_BAND_DRUMS:
                case SubType.KEYBOARD_ROCK_BAND_DRUMS:
                    deviceType = DeviceControllerType.Drum;
                    rhythmType = RhythmType.RockBand;
                    break;
                case SubType.PS3_GUITAR_HERO_DRUMS:
                case SubType.MIDI_GUITAR_HERO_DRUMS:
                case SubType.XINPUT_GUITAR_HERO_DRUMS:
                case SubType.KEYBOARD_GUITAR_HERO_DRUMS:
                    deviceType = DeviceControllerType.Drum;
                    rhythmType = RhythmType.GuitarHero;
                    break;
                case SubType.PS3_ROCK_BAND_GUITAR:
                case SubType.WII_ROCK_BAND_GUITAR:
                case SubType.MIDI_ROCK_BAND_GUITAR:
                case SubType.XINPUT_ROCK_BAND_GUITAR:
                case SubType.KEYBOARD_ROCK_BAND_GUITAR:
                    deviceType = DeviceControllerType.Guitar;
                    rhythmType = RhythmType.RockBand;
                    break;
                case SubType.PS3_GUITAR_HERO_GUITAR:
                case SubType.MIDI_GUITAR_HERO_GUITAR:
                case SubType.XINPUT_GUITAR_HERO_GUITAR:
                case SubType.KEYBOARD_GUITAR_HERO_GUITAR:
                    deviceType = DeviceControllerType.Guitar;
                    rhythmType = RhythmType.GuitarHero;
                    break;
                default:
                    deviceType = DeviceControllerType.Gamepad;
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
                    var genAxis = _axisToStandard[(ControllerAxis)axis];
                    var scale = config.axisScale.axis[axis];
                    var isTrigger = axis == (int)ControllerAxis.XBOX_LT || axis == (int)ControllerAxis.XBOX_RT;

                    Color on = Color.FromRgb(0, 0, 0);
                    if (colors.ContainsKey(axis + XboxBtnCount))
                    {
                        on = colors[axis + XboxBtnCount];
                    }
                    Color off = Color.FromRgb(0, 0, 0);
                    if (deviceType == DeviceControllerType.Guitar && (ControllerAxis)axis == XboxWhammy)
                    {
                        isTrigger = true;
                    }
                    if (deviceType == DeviceControllerType.Guitar && (ControllerAxis)axis == XboxTilt)
                    {
                        bindings.Add(new DigitalToAnalog(controller, 32767, new DirectDigital(controller, DevicePinMode.VCC, pin.pin, config.debounce.buttons, new GenericControllerButton(StandardButtonType.A), on, off), AnalogToDigitalType.Trigger, new GenericAxis(StandardAxisType.RightStickY), on, off, 1, 1, 1, true));
                    }
                    else
                    {
                        bindings.Add(new DirectAnalog(controller, pin.pin, new GenericAxis(genAxis), on, off, (scale.multiplier / 1024.0f) * (isTrigger ? 2 : 1) * (pin.inverted > 0 ? -1 : 1), ((isTrigger ? 0 : 32670) + scale.offset) >> 8, ((isTrigger ? 32768 : 0) + scale.deadzone) >> 8, isTrigger));
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
                    var genButton = _buttonToStandard[(ControllerButtons)button];
                    var pinMode = DevicePinMode.VCC;
                    if (config.all.main.fretLEDMode == 1 && deviceType == DeviceControllerType.Guitar && _frets.Contains(genButton))
                    {
                        pinMode = DevicePinMode.Floating;
                    }
                    var debounce = config.debounce.buttons;
                    if (deviceType == DeviceControllerType.Guitar && (genButton == StandardButtonType.Up || genButton == StandardButtonType.Down))
                    {
                        debounce = config.debounce.strum;
                    }
                    IOutputButton output = new GenericControllerButton(genButton);
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
            // Keyboard / Mouse does not have a joystick
            if (config.all.main.mapLeftJoystickToDPad > 0)
            {
                var lx = bindings.FilterCast<Binding, Axis>().FirstOrDefault(axis => axis?.Type.Type == StandardAxisType.LeftStickX, null);
                var ly = bindings.FilterCast<Binding, Axis>().FirstOrDefault(axis => axis?.Type.Type == StandardAxisType.LeftStickY, null);
                if (lx != null)
                {
                    var onlx = lx.LedOn;
                    var offlx = lx.LedOff;
                    bindings.Add(new AnalogToDigital(controller, config.all.axis.joyThreshold, lx, AnalogToDigitalType.JoyLow, config.debounce.buttons, new GenericControllerButton(StandardButtonType.Left), onlx, offlx));
                    bindings.Add(new AnalogToDigital(controller, config.all.axis.joyThreshold, lx, AnalogToDigitalType.JoyHigh, config.debounce.buttons, new GenericControllerButton(StandardButtonType.Right), onlx, offlx));

                }
                if (ly != null)
                {
                    var only = ly.LedOn;
                    var offly = ly.LedOff;
                    bindings.Add(new AnalogToDigital(controller, config.all.axis.joyThreshold, ly, AnalogToDigitalType.JoyLow, config.debounce.buttons, new GenericControllerButton(StandardButtonType.Down), only, offly));
                    bindings.Add(new AnalogToDigital(controller, config.all.axis.joyThreshold, ly, AnalogToDigitalType.JoyHigh, config.debounce.buttons, new GenericControllerButton(StandardButtonType.Up), only, offly));
                }
            }
            _config = new DeviceConfiguration(controller, bindings, ledType, deviceType, emulationType, rhythmType, tiltType == TiltType.DigitalMercury, xinputOnWindows);
            _config.Generate(pio);
        }
        catch (ArgumentException)
        {
            _failed = true;
            MigrationSupported = false;
            _config = new DeviceConfiguration(new Pico(Board.Rp2040Boards[0]));
        }
    }

    public override String ToString()
    {
        if (_failed)
        {
            return "An ardwiino device had issues reading, please unplug and replug it.";
        }
        return $"Ardwiino - {Board.Name} - {version}";
    }

    public override void Bootloader()
    {
        WriteData(JumpBootloaderCommand, RequestHidSetReport, Array.Empty<byte>());
    }

    public override void BootloaderUsb()
    {
        WriteData(JumpBootloaderCommandUno, RequestHidSetReport, Array.Empty<byte>());
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

    enum ControllerButtons
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

    enum ControllerAxis
    {
        XBOX_LT,
        XBOX_RT,
        XBOX_L_X,
        XBOX_L_Y,
        XBOX_R_X,
        XBOX_R_Y
    };

    private static readonly Dictionary<ControllerAxis, StandardAxisType> _axisToStandard = new Dictionary<ControllerAxis, StandardAxisType>() {
        {ControllerAxis.XBOX_L_X,StandardAxisType.LeftStickX},
        {ControllerAxis.XBOX_L_Y,StandardAxisType.LeftStickY},
        {ControllerAxis.XBOX_R_X,StandardAxisType.RightStickX},
        {ControllerAxis.XBOX_R_Y,StandardAxisType.RightStickY},
        {ControllerAxis.XBOX_LT, StandardAxisType.LeftTrigger},
        {ControllerAxis.XBOX_RT,StandardAxisType.RightTrigger}
    };

    private static readonly Dictionary<ControllerButtons, StandardButtonType> _buttonToStandard = new Dictionary<ControllerButtons, StandardButtonType>(){
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

    readonly List<StandardButtonType> _frets = new List<StandardButtonType>() { StandardButtonType.A, StandardButtonType.B, StandardButtonType.X, StandardButtonType.Y, StandardButtonType.LB, StandardButtonType.RB };
    enum GyroOrientationOld
    {
        POSITIVE_Z,
        NEGATIVE_Z,
        POSITIVE_Y,
        NEGATIVE_Y,
        POSITIVE_X,
        NEGATIVE_X
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CpuInfoOld
    {
        public readonly UInt32 cpu_freq;
        public readonly byte multi;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public readonly string board;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CpuInfo
    {
        public readonly UInt32 cpu_freq;
        public readonly byte multi;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public readonly string board;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Led
    {
        public readonly byte pin;
        public readonly byte red;
        public readonly byte green;
        public readonly byte blue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Config
    {
        public readonly byte inputType;
        public readonly byte subType;
        public readonly byte tiltType;
        public readonly byte pollRate;
        public readonly byte fretLEDMode;
        public readonly byte mapLeftJoystickToDPad;
        public readonly byte mapStartSelectToHome;
        public readonly byte mapNunchukAccelToRightJoy;
        public readonly UInt32 signature;
        public readonly UInt32 version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AnalogPin
    {
        public readonly byte pin;
        public byte inverted;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Pins
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxBtnCount)]
        public readonly byte[] pins;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount)]
        public readonly AnalogPin[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AnalogKey
    {
        public readonly byte neg;
        public readonly byte pos;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Keys
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxBtnCount + XboxTriggerCount)]
        public readonly byte[] pins;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount - XboxTriggerCount)]
        public readonly AnalogKey[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct MainConfig
    {
        public readonly byte inputType;
        public readonly byte subType;
        public readonly byte tiltType;
        public readonly byte pollRate;
        public readonly byte fretLEDMode;
        public readonly byte mapLeftJoystickToDPad;
        public readonly byte mapStartSelectToHome;
        public readonly byte mapNunchukAccelToRightJoy;
        public readonly UInt32 signature;
        public readonly UInt32 version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisConfig
    {
        public readonly byte triggerThreshold;
        public readonly byte joyThreshold;
        public readonly byte drumThreshold;

        public byte mpu6050Orientation;
        public readonly Int16 tiltSensitivity;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct MidiConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly byte[] type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly byte[] note;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly byte[] channel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct RfConfig
    {
        public byte rfInEnabled;
        public UInt32 id;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScale
    {
        public Int16 multiplier;
        public Int16 offset;
        public Int16 deadzone;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScale12
    {
        public readonly Int16 multiplier;
        public readonly Int16 offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Version11AxisWhammyConfig
    {
        public readonly byte multiplier;
        public readonly UInt16 offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScaleConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount)]
        public AxisScale[] axis;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScaleConfig12
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount)]
        public readonly AxisScale12[] axis;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DebounceConfig13
    {
        public readonly byte buttons;
        public readonly byte strum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DebounceConfig
    {
        public byte buttons;
        public byte strum;
        public byte combinedStrum;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ArdwiinoConfiguration
    {
        public FullArdwiinoConfiguration all;
        public RfConfig rf;
        public byte pinsSP;
        public AxisScaleConfig axisScale;
        public DebounceConfig debounce;
        public NeckConfig neck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration14
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;
        public readonly AxisScaleConfig axisScale;
        public readonly DebounceConfig debounce;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration13
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;
        public readonly AxisScaleConfig axisScale;
        public readonly DebounceConfig13 debounce;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration12
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;
        public readonly AxisScaleConfig12 axisScale;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration11
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;

        public readonly Version11AxisWhammyConfig whammy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration10
    {

        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
        public readonly byte pinsSP;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Configuration8
    {
        public readonly FullArdwiinoConfiguration all;
        public readonly RfConfig rf;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct NeckConfig
    {
        readonly bool wtNeck;
        readonly bool gh5Neck;
        readonly bool gh5NeckBar;
        readonly bool wiiNeck;
        readonly bool ps2Neck;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct FullArdwiinoConfiguration
    {
        public readonly MainConfig main;
        public readonly Pins pins;
        public AxisConfig axis;
        public readonly Keys keys;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly Led[] leds;
        public readonly MidiConfig midi;
    }
}
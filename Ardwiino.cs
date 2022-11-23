using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using LibUsbDotNet;

namespace GuitarConfiguratorSharp.NetCore;

public class Ardwiino : ConfigurableUsbDevice
{
    private uint _cpuFreq;
    private const int XboxBtnCount = 16;
    private const int XboxAxisCount = 6;
    private const int XboxTriggerCount = 2;

    private const ControllerAxisType XboxWhammy = ControllerAxisType.XboxRX;
    private const ControllerAxisType XboxTilt = ControllerAxisType.XboxRY;
    private static readonly Version OldCpuInfoVersion = new(8, 8, 4);

    private static readonly Version UsbControlRequestApi = new(4, 3, 7);

    public const ushort SerialArdwiinoRevision = 0x3122;
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

    private const byte NotUsed = 0xFF;

    private readonly bool _failed = false;

    public Ardwiino(PlatformIo pio, string path, UsbDevice device, string product, string serial, ushort versionNumber)
        : base(device, path, product, serial, versionNumber)
    {
        if (Version < new Version(6, 0, 0))
        {
            var buffer = ReadData(6, RequestHidGetReport);
            _cpuFreq = uint.Parse(StructTools.RawDeserializeStr(buffer));
            buffer = ReadData(7, RequestHidGetReport);
            var board = StructTools.RawDeserializeStr(buffer);
            Board = Board.FindBoard(board, _cpuFreq);
            MigrationSupported = false;
            return;
        }

        MigrationSupported = true;
        // Version 6.0.0 started at config version 6, so we don't have to support anything earlier than that
        var data = ReadData(CpuInfoCommand, 1);
        if (Version < OldCpuInfoVersion)
        {
            var info = StructTools.RawDeserialize<CpuInfoOld>(data, 0);
            _cpuFreq = info.cpu_freq;
            Board = Board.FindBoard(info.board, _cpuFreq);
        }
        else
        {
            var info = StructTools.RawDeserialize<CpuInfo>(data, 0);
            _cpuFreq = info.cpu_freq;
            Board = Board.FindBoard(info.board, _cpuFreq);
        }
    }

    public override string ToString()
    {
        if (_failed)
        {
            return "An ardwiino device had issues reading, please unplug and replug it.";
        }

        return $"Ardwiino - {Board.Name} - {Version}";
    }

    public override void Bootloader()
    {
        WriteData(JumpBootloaderCommand, RequestHidSetReport, Array.Empty<byte>());
    }

    public override void BootloaderUsb()
    {
        WriteData(JumpBootloaderCommandUno, RequestHidSetReport, Array.Empty<byte>());
    }

    public override async Task LoadConfiguration(ConfigViewModel model)
    {
        if (!MigrationSupported)
        {
            await model.SetDefaults(Board.FindMicrocontroller(Board));
            return;
        }

        try
        {
            var readConfig = ReadConfigCommand;
            if (Version < new Version(8, 0, 7))
            {
                readConfig = ReadConfigPre807Command;
            }
            else if (Version < new Version(7, 0, 3))
            {
                readConfig = ReadConfigPre703Command;
            }

            var data = new byte[Marshal.SizeOf(typeof(ArdwiinoConfiguration))];
            var sizeOfAll = Marshal.SizeOf(typeof(FullArdwiinoConfiguration));
            var offset = 0;
            var offsetId = 0;
            var maxSize = data.Length;
            uint version = 0;
            // Set the defaults for things that arent in every version
            var config = new ArdwiinoConfiguration();
            config.neck = new NeckConfig();
            config.axisScale = new AxisScaleConfig();
            config.axisScale.axis = new AxisScale[XboxAxisCount];
            foreach (int axis in Enum.GetValues(typeof(ControllerAxisType)))
            {
                config.axisScale.axis[axis].multiplier = 1;
                config.axisScale.axis[axis].offset = short.MinValue;
                config.axisScale.axis[axis].deadzone = short.MaxValue;
            }

            config.debounce.buttons = 5;
            config.debounce.strum = 20;
            config.debounce.combinedStrum = 0;
            config.rf.id = 0;
            config.rf.rfInEnabled = 0;
            while (offset < maxSize)
            {
                var data2 = ReadData((ushort) (readConfig + offsetId), RequestHidGetReport);
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
                config.all.pins.axis![((byte) ControllerAxisType.XboxRX)].inverted =
                    (byte) (config.all.pins.axis[(int) ControllerAxisType.XboxRX].inverted == 0 ? 1 : 0);
            }

            // Read in the rest of the data, in the format that it is in
            if (version == 16 || version == 17)
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
                foreach (int axis in Enum.GetValues(typeof(ControllerAxisType)))
                {
                    config.axisScale.axis[axis].multiplier = configOld.axisScale.axis[axis].multiplier;
                    config.axisScale.axis[axis].offset = configOld.axisScale.axis[axis].offset;
                    config.axisScale.axis[axis].deadzone = short.MaxValue;
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
                config.axisScale.axis[(int) ControllerAxisType.XboxRX].multiplier = configOld.whammy.multiplier;
                config.axisScale.axis[(int) ControllerAxisType.XboxRX].offset = (short) configOld.whammy.offset;
                config.pinsSP = configOld.pinsSP;
                config.rf = configOld.rf;
            }
            else if (version > 8)
            {
                var configOld = StructTools.RawDeserialize<Configuration10>(data, 0);
                config.pinsSP = configOld.pinsSP;
                config.rf = configOld.rf;
            }

            if (version < 17 && config.all.main.subType > (int) SubType.XinputArcadePad)
            {
                config.all.main.subType += SubType.XinputTurntable - SubType.XinputArcadePad;
                if (config.all.main.subType > (int) SubType.Ps3Gamepad)
                {
                    config.all.main.subType += 2;
                }

                if (config.all.main.subType > (int) SubType.WiiRockBandDrums)
                {
                    config.all.main.subType += 1;
                }
            }

            var controller = Board.FindMicrocontroller(Board);
            var bindings = new List<Output>();
            var colors = new Dictionary<int, Color>();
            var ledIndexes = new Dictionary<int, byte>();
            for (byte index = 0; index < config.all!.leds!.Length; index++)
            {
                var led = config.all!.leds![index];
                if (led.pin != 0)
                {
                    colors[led.pin - 1] = Color.FromRgb(led.red, led.green, led.blue);
                    ledIndexes[led.pin - 1] = index;
                }
            }

            var ledType = LedType.None;
            var deviceType = DeviceControllerType.Guitar;
            var emulationType = EmulationType.Controller;
            var inputControllerType = (InputControllerType) config.all.main.inputType;
            var rhythmType = RhythmType.GuitarHero;
            if (config.all.main.fretLEDMode == 2)
            {
                ledType = LedType.Apa102;
            }

            if ((config.all.main.subType >= (int) SubType.KeyboardGamepad &&
                 config.all.main.subType <= (int) SubType.KeyboardRockBandDrums) ||
                config.all.main.subType == (int) SubType.Mouse)
            {
                emulationType = EmulationType.KeyboardMouse;
            }

            if (config.all.main.subType >= (int) SubType.MidiGamepad)
            {
                emulationType = EmulationType.Midi;
            }

            var xinputOnWindows = (SubType)config.all.main.subType <= SubType.XinputTurntable;
            switch ((SubType) config.all.main.subType)
            {
                case SubType.XinputGamepad:
                case SubType.XinputLiveGuitar:
                case SubType.XinputRockBandDrums:
                case SubType.XinputGuitarHeroDrums:
                case SubType.XinputRockBandGuitar:
                case SubType.XinputGuitarHeroGuitar:
                case SubType.XinputTurntable:
                    xinputOnWindows = true;
                    break;
            }

            switch ((SubType) config.all.main.subType)
            {
                case SubType.XinputTurntable:
                case SubType.Ps3Turntable:
                    deviceType = DeviceControllerType.TurnTable;
                    break;
                case SubType.XinputGamepad:
                case SubType.Ps3Gamepad:
                case SubType.SwitchGamepad:
                case SubType.MidiGamepad:
                case SubType.KeyboardGamepad:
                    deviceType = DeviceControllerType.Gamepad;
                    break;
                case SubType.XinputArcadePad:
                    deviceType = DeviceControllerType.ArcadePad;
                    break;
                case SubType.XinputWheel:
                    deviceType = DeviceControllerType.Wheel;
                    break;
                case SubType.XinputArcadeStick:
                    deviceType = DeviceControllerType.ArcadeStick;
                    break;
                case SubType.XinputFlightStick:
                    deviceType = DeviceControllerType.FlightStick;
                    break;
                case SubType.XinputDancePad:
                    deviceType = DeviceControllerType.DancePad;
                    break;
                case SubType.WiiLiveGuitar:
                case SubType.Ps3LiveGuitar:
                case SubType.MidiLiveGuitar:
                case SubType.XinputLiveGuitar:
                case SubType.KeyboardLiveGuitar:
                    deviceType = DeviceControllerType.LiveGuitar;
                    rhythmType = RhythmType.GuitarHero;
                    break;
                case SubType.Ps3RockBandDrums:
                case SubType.WiiRockBandDrums:
                case SubType.MidiRockBandDrums:
                case SubType.XinputRockBandDrums:
                case SubType.KeyboardRockBandDrums:
                    deviceType = DeviceControllerType.Drum;
                    rhythmType = RhythmType.RockBand;
                    break;
                case SubType.Ps3GuitarHeroDrums:
                case SubType.MidiGuitarHeroDrums:
                case SubType.XinputGuitarHeroDrums:
                case SubType.KeyboardGuitarHeroDrums:
                    deviceType = DeviceControllerType.Drum;
                    rhythmType = RhythmType.GuitarHero;
                    break;
                case SubType.Ps3RockBandGuitar:
                case SubType.WiiRockBandGuitar:
                case SubType.MidiRockBandGuitar:
                case SubType.XinputRockBandGuitar:
                case SubType.KeyboardRockBandGuitar:
                    deviceType = DeviceControllerType.Guitar;
                    rhythmType = RhythmType.RockBand;
                    break;
                case SubType.Ps3GuitarHeroGuitar:
                case SubType.MidiGuitarHeroGuitar:
                case SubType.XinputGuitarHeroGuitar:
                case SubType.KeyboardGuitarHeroGuitar:
                    deviceType = DeviceControllerType.Guitar;
                    rhythmType = RhythmType.GuitarHero;
                    break;
                default:
                    deviceType = DeviceControllerType.Gamepad;
                    break;
            }

            var sda = 18;
            var scl = 19;
            var mosi = 3;
            var miso = 4;
            var sck = 6;
            var att = 0;
            var ack = 0;
            switch (controller)
            {
                case Micro:
                case Pico:
                    att = 10;
                    ack = 7;
                    break;
                case Uno:
                case Mega:
                    att = 10;
                    ack = 2;
                    break;
            }

            if (config.all.main.inputType == (int) InputControllerType.Direct)
            {
                if (deviceType == DeviceControllerType.Guitar)
                {
                    if (config.neck.gh5Neck != 0 || config.neck.gh5NeckBar != 0)
                    {
                        bindings.Add(new Gh5CombinedOutput(model, controller, sda, scl));
                    }

                    if (config.neck.wtNeck != 0)
                    {
                        bindings.Add(new GhwtCombinedOutput(model, controller, 9));
                    }
                }

                if (deviceType == DeviceControllerType.TurnTable)
                {
                    bindings.Add(new DjCombinedOutput(model, controller, sda, scl));
                }

                foreach (int axis in Enum.GetValues(typeof(ControllerAxisType)))
                {
                    var pin = config.all.pins.axis![axis];
                    if (pin.pin == NotUsed)
                    {
                        continue;
                    }

                    var genAxis = AxisToStandard[(ControllerAxisType) axis];
                    var scale = config.axisScale.axis[axis];
                    var isTrigger = axis == (int) ControllerAxisType.XboxLt ||
                                    axis == (int) ControllerAxisType.XboxRt;

                    var on = Color.FromRgb(0, 0, 0);
                    if (colors.ContainsKey(axis + XboxBtnCount))
                    {
                        on = colors[axis + XboxBtnCount];
                    }
                    
                    byte? ledIndex = ledIndexes.GetValueOrDefault(pin.pin);

                    var off = Color.FromRgb(0, 0, 0);
                    if (deviceType == DeviceControllerType.Guitar && (ControllerAxisType) axis == XboxWhammy)
                    {
                        isTrigger = true;
                    }

                    if (deviceType == DeviceControllerType.Guitar && (ControllerAxisType) axis == XboxTilt &&
                        config.all.main.tiltType == 2)
                    {
                        bindings.Add(new ControllerAxis(model,
                            new DigitalToAnalog(new DirectInput(pin.pin, DevicePinMode.PullUp, model, controller), 32767, model), on,
                            off, ledIndex, 1, 0,
                            0, StandardAxisType.RightStickY));
                    }
                    else
                    {
                        var axisMultiplier = (scale.multiplier / 1024.0f) * (isTrigger ? 2 : 1) *
                                             (pin.inverted > 0 ? -1 : 1);
                        var axisOffset = ((isTrigger ? 0 : 32670) + scale.offset) >> 8;
                        var axisDeadzone = ((isTrigger ? 32768 : 0) + scale.deadzone) >> 8;
                        bindings.Add(new ControllerAxis(model,
                            new DirectInput(pin.pin, DevicePinMode.Analog, model, controller), on, off,
                            ledIndex, axisMultiplier, axisOffset, axisDeadzone, genAxis));
                    }
                }

                foreach (int button in Enum.GetValues(typeof(ControllerButtons)))
                {
                    var pin = config.all.pins.pins![button];
                    if (pin == NotUsed)
                    {
                        continue;
                    }

                    var on = Color.FromRgb(0, 0, 0);
                    if (colors.ContainsKey(button))
                    {
                        on = colors[button];
                    }
                    byte? ledIndex = ledIndexes.GetValueOrDefault(pin);

                    var off = Color.FromRgb(0, 0, 0);
                    var genButton = ButtonToStandard[(ControllerButtons) button];
                    var pinMode = DevicePinMode.PullUp;
                    if (config.all.main.fretLEDMode == 1 && deviceType == DeviceControllerType.Guitar &&
                        _frets.Contains(genButton))
                    {
                        pinMode = DevicePinMode.Floating;
                    }

                    var debounce = config.debounce.buttons;
                    if (deviceType == DeviceControllerType.Guitar &&
                        (genButton == StandardButtonType.Up || genButton == StandardButtonType.Down))
                    {
                        debounce = config.debounce.strum;
                    }

                    if (deviceType == DeviceControllerType.TurnTable && genButton == StandardButtonType.LeftStick)
                    {
                        genButton = StandardButtonType.Y;
                    }

                    bindings.Add(new ControllerButton(model, new DirectInput(pin, pinMode, model, controller), on, off,
                        ledIndex, debounce, genButton));
                }
            }
            else if (config.all.main.tiltType == 2)
            {
                if (deviceType == DeviceControllerType.Guitar)
                {
                    var pin = config.all.pins.axis![(int) XboxTilt];
                    if (pin.pin != NotUsed)
                    {
                        var on = Color.FromRgb(0, 0, 0);
                        if (colors.ContainsKey((int) (XboxTilt + XboxBtnCount)))
                        {
                            on = colors[(int) (XboxTilt + XboxBtnCount)];
                        }

                        var off = Color.FromRgb(0, 0, 0);
                        byte? ledIndex = ledIndexes.GetValueOrDefault(pin.pin);
                        bindings.Add(new ControllerAxis(model,
                            new DigitalToAnalog(new DirectInput(pin.pin, DevicePinMode.PullUp, model, controller), 32767, model), on,
                            off, ledIndex, 1, 0,
                            0, StandardAxisType.RightStickY));
                    }
                }

                if (config.all.main.inputType == (int) InputControllerType.Wii)
                {
                    var wii = new WiiCombinedOutput(model, controller, sda, scl);
                    if (config.all.main.mapNunchukAccelToRightJoy != 0)
                    {
                        wii.AddNunchukAcceleration();
                    }
                    bindings.Add(wii);
                }
                else if (config.all.main.inputType == (int) InputControllerType.Ps2)
                {
                    bindings.Add(new Ps2CombinedOutput(model, controller, miso, mosi, sck, att, ack));
                }
            }

            // Keyboard / Mouse does not have a joystick
            if (config.all.main.mapLeftJoystickToDPad > 0)
            {
                ControllerAxis? lx = null;
                ControllerAxis? ly = null;
                var threshold = config.all.axis.joyThreshold << 8;
                foreach (var binding in bindings)
                {
                    if (binding is ControllerAxis axis)
                    {
                        if (axis.Type == StandardAxisType.LeftStickX)
                        {
                            lx = axis;
                        }
                        else if (axis.Type == StandardAxisType.LeftStickY)
                        {
                            ly = axis;
                        }
                    }
                }

                if (lx != null && lx.Input != null)
                {
                    var ledOn = lx.LedOn;
                    var ledOff = lx.LedOff;
                    bindings.Add(new ControllerButton(model,
                        new AnalogToDigital(lx.Input, AnalogToDigitalType.JoyLow, threshold, model), ledOn, ledOff,
                        null, config.debounce.buttons, StandardButtonType.Left));
                    bindings.Add(new ControllerButton(model,
                        new AnalogToDigital(lx.Input, AnalogToDigitalType.JoyHigh, threshold, model), ledOn, ledOff,
                        null, config.debounce.buttons, StandardButtonType.Right));
                }

                if (ly != null && ly.Input != null)
                {
                    var ledOn = ly.LedOn;
                    var ledOff = ly.LedOff;
                    bindings.Add(new ControllerButton(model,
                        new AnalogToDigital(ly.Input, AnalogToDigitalType.JoyLow, threshold, model), ledOn, ledOff,
                        null, config.debounce.buttons, StandardButtonType.Up));
                    bindings.Add(new ControllerButton(model,
                        new AnalogToDigital(ly.Input, AnalogToDigitalType.JoyHigh, threshold, model), ledOn, ledOff,
                        null, config.debounce.buttons, StandardButtonType.Down));
                }
            }

            model.MicroController = controller;
            model.LedType = ledType;
            model.DeviceType = deviceType;
            model.EmulationType = emulationType;
            model.RhythmType = rhythmType;
            model.XInputOnWindows = xinputOnWindows;
            model.Bindings.Clear();
            model.Bindings.AddRange(bindings);
            await model.Write();
        }
        catch (ArgumentException)
        {
            //TODO: do this right
            throw new IncompleteConfigurationException("Something went wrong");
        }
    }

    enum SubType
    {
        XinputGamepad = 1,
        XinputWheel,
        XinputArcadeStick,
        XinputFlightStick,
        XinputDancePad,
        XinputLiveGuitar = 9,
        XinputRockBandDrums = 12,
        XinputGuitarHeroDrums,
        XinputRockBandGuitar,
        XinputGuitarHeroGuitar,
        XinputArcadePad = 19,
        XinputTurntable = 23,
        KeyboardGamepad,
        KeyboardGuitarHeroGuitar,
        KeyboardRockBandGuitar,
        KeyboardLiveGuitar,
        KeyboardGuitarHeroDrums,
        KeyboardRockBandDrums,
        SwitchGamepad,
        Ps3GuitarHeroGuitar,
        Ps3GuitarHeroDrums,
        Ps3RockBandGuitar,
        Ps3RockBandDrums,
        Ps3Gamepad,
        Ps3Turntable,
        Ps3LiveGuitar,
        WiiRockBandGuitar,
        WiiRockBandDrums,
        WiiLiveGuitar,
        Mouse,
        MidiGamepad,
        MidiGuitarHeroGuitar,
        MidiRockBandGuitar,
        MidiLiveGuitar,
        MidiGuitarHeroDrums,
        MidiRockBandDrums
    }

    enum ControllerButtons
    {
        XboxDpadUp,
        XboxDpadDown,
        XboxDpadLeft,
        XboxDpadRight,
        XboxStart,
        XboxBack,
        XboxLeftStick,
        XboxRightStick,

        XboxLb,
        XboxRb,
        XboxHome,
        XboxUnused,
        XboxA,
        XboxB,
        XboxX,
        XboxY,
    }

    enum ControllerAxisType
    {
        XboxLt,
        XboxRt,
        XboxLX,
        XboxLY,
        XboxRX,
        XboxRY
    }

    private static readonly Dictionary<ControllerAxisType, StandardAxisType> AxisToStandard =
        new()
        {
            {ControllerAxisType.XboxLX, StandardAxisType.LeftStickX},
            {ControllerAxisType.XboxLY, StandardAxisType.LeftStickY},
            {ControllerAxisType.XboxRX, StandardAxisType.RightStickX},
            {ControllerAxisType.XboxRY, StandardAxisType.RightStickY},
            {ControllerAxisType.XboxLt, StandardAxisType.LeftTrigger},
            {ControllerAxisType.XboxRt, StandardAxisType.RightTrigger}
        };

    private static readonly Dictionary<ControllerButtons, StandardButtonType> ButtonToStandard =
        new()
        {
            {ControllerButtons.XboxDpadUp, StandardButtonType.Up},
            {ControllerButtons.XboxDpadDown, StandardButtonType.Down},
            {ControllerButtons.XboxDpadLeft, StandardButtonType.Left},
            {ControllerButtons.XboxDpadRight, StandardButtonType.Right},
            {ControllerButtons.XboxStart, StandardButtonType.Start},
            {ControllerButtons.XboxBack, StandardButtonType.Select},
            {ControllerButtons.XboxLeftStick, StandardButtonType.LeftStick},
            {ControllerButtons.XboxRightStick, StandardButtonType.RightStick},
            {ControllerButtons.XboxLb, StandardButtonType.Lb},
            {ControllerButtons.XboxRb, StandardButtonType.Rb},
            {ControllerButtons.XboxHome, StandardButtonType.Home},
            {ControllerButtons.XboxUnused, StandardButtonType.Capture},
            {ControllerButtons.XboxA, StandardButtonType.A},
            {ControllerButtons.XboxB, StandardButtonType.B},
            {ControllerButtons.XboxX, StandardButtonType.X},
            {ControllerButtons.XboxY, StandardButtonType.Y}
        };

    readonly List<StandardButtonType> _frets = new()
    {
        StandardButtonType.A, StandardButtonType.B, StandardButtonType.X, StandardButtonType.Y, StandardButtonType.Lb,
        StandardButtonType.Rb
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CpuInfoOld
    {
        public readonly uint cpu_freq;
        public readonly byte multi;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public readonly string board;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CpuInfo
    {
        public readonly uint cpu_freq;
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
        public readonly uint signature;
        public readonly uint version;
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
        public byte subType;
        public readonly byte tiltType;
        public readonly byte pollRate;
        public readonly byte fretLEDMode;
        public readonly byte mapLeftJoystickToDPad;
        public readonly byte mapStartSelectToHome;
        public readonly byte mapNunchukAccelToRightJoy;
        public readonly uint signature;
        public readonly uint version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisConfig
    {
        public readonly byte triggerThreshold;
        public readonly byte joyThreshold;
        public readonly byte drumThreshold;

        public byte mpu6050Orientation;
        public readonly short tiltSensitivity;
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
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScale
    {
        public short multiplier;
        public short offset;
        public short deadzone;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AxisScale12
    {
        public readonly short multiplier;
        public readonly short offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Version11AxisWhammyConfig
    {
        public readonly byte multiplier;
        public readonly ushort offset;
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
        public readonly byte wtNeck;
        public readonly byte gh5Neck;
        public readonly byte gh5NeckBar;
        public readonly byte wiiNeck;
        public readonly byte ps2Neck;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct FullArdwiinoConfiguration
    {
        public MainConfig main;
        public readonly Pins pins;
        public AxisConfig axis;
        public readonly Keys keys;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = XboxAxisCount + XboxBtnCount)]
        public readonly Led[] leds;

        public readonly MidiConfig midi;
    }

    public enum InputControllerType
    {
        None,
        Wii,
        Direct,
        Ps2
    }
}
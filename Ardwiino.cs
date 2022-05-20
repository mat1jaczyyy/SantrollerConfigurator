
using Device.Net;
using System;
using System.Text;
using System.Runtime.InteropServices;

public class Ardwiino : ConfigurableUSBDevice
{

    private UInt32 cpuFreq;
    private string board;
    private const int XBOX_BTN_COUNT = 16;
    private const int XBOX_AXIS_COUNT = 6;
    private static readonly Version OLD_CPU_INFO_VERSION = new Version(8, 8, 4);

    private static readonly Version USB_CONTROL_REQUEST_API = new Version(4, 3, 7);

    // On 6.0.0, READ_CONFIG is 59
    // On 7.0.3, READ_CONFIG is 60
    // And with 8.0.7 READ_CONFIG is 62 and stays there
    private const ushort CPU_INFO_COMMAND = 50;
    private const ushort READ_CONFIG_COMMAND = 62;
    private const ushort READ_CONFIG_PRE_8_0_7_COMMAND = 60;
    private const ushort READ_CONFIG_PRE_7_0_3_COMMAND = 59;
    private string boardName;
    bool unmigrateable = false;

    public Ardwiino(IDevice device, string product, string serial, ushort version_number) : base(device, product, serial, version_number)
    {
        if (this.version < new Version(6, 0, 0))
        {
            var buffer = this.ReadData(6);
            this.cpuFreq = uint.Parse(StructTools.RawDeserializeStr(buffer));
            buffer = this.ReadData(7);
            this.board = StructTools.RawDeserializeStr(buffer);
            this.boardName = Board.findBoard(this.board).name;
            unmigrateable = true;
            return;
        }
        // Version 6.0.0 started at config version 6, so we don't have to support anything earlier than that
        byte[] data = this.ReadData(CPU_INFO_COMMAND);
        if (this.version < OLD_CPU_INFO_VERSION)
        {
            CpuInfoOld info = StructTools.RawDeserialize<CpuInfoOld>(data, 0);
            this.cpuFreq = info.cpu_freq;
            this.board = info.board;
        }
        else
        {
            CpuInfo info = StructTools.RawDeserialize<CpuInfo>(data, 0);
            this.cpuFreq = info.cpu_freq;
            this.board = info.board;
        }
        this.boardName = Board.findBoard(this.board).name;
        var read_config = READ_CONFIG_COMMAND;
        if (this.version < new Version(8, 0, 7))
        {
            read_config = READ_CONFIG_PRE_8_0_7_COMMAND;
        }
        else if (this.version < new Version(7, 0, 3))
        {
            read_config = READ_CONFIG_PRE_7_0_3_COMMAND;
        }
        data = new byte[Marshal.SizeOf(typeof(Configuration))];
        int sizeOfAll = Marshal.SizeOf(typeof(ConfigurationAll));
        int offset = 0;
        int offsetId = 0;
        int maxSize = data.Length;
        uint version = 0;
        // Set the defaults for things that arent in every version
        Configuration config = new Configuration();
        config.axisScale.r_x.deadzone = Int16.MaxValue;
        config.axisScale.r_y.multiplier = 1;
        config.axisScale.r_y.offset = Int16.MinValue;
        config.axisScale.r_y.deadzone = Int16.MaxValue;
        config.axisScale.l_x.multiplier = 1;
        config.axisScale.l_x.offset = Int16.MinValue;
        config.axisScale.l_x.deadzone = Int16.MaxValue;
        config.axisScale.l_y.multiplier = 1;
        config.axisScale.l_y.offset = Int16.MinValue;
        config.axisScale.l_y.deadzone = Int16.MaxValue;
        config.axisScale.lt.multiplier = 1;
        config.axisScale.lt.offset = Int16.MinValue;
        config.axisScale.lt.deadzone = Int16.MaxValue;
        config.axisScale.rt.multiplier = 1;
        config.axisScale.rt.offset = Int16.MinValue;
        config.axisScale.rt.deadzone = Int16.MaxValue;
        config.debounce.buttons = 5;
        config.debounce.strum = 20;
        config.debounce.combinedStrum = 0;
        config.rf.id = 0;
        config.rf.rfInEnabled = 0;
        while (offset < maxSize)
        {
            var data2 = ReadData((ushort)(read_config + offsetId));
            Array.Copy(data2, 0, data, offset, data2.Length);
            offset += data2.Length;
            offsetId++;
            if (offset > sizeOfAll)
            {
                config.all = StructTools.RawDeserialize<ConfigurationAll>(data, 0);
                version = config.all.main.version;
                if (version > 12)
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
            config.all.pins.r_x.inverted = (byte)(config.all.pins.r_x.inverted == 0 ? 1 : 0);
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
        if (version > 13)
        {
            config = StructTools.RawDeserialize<Configuration>(data, 0);
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
            config.axisScale.l_x.multiplier = config_old.axisScale.l_x.multiplier;
            config.axisScale.l_x.offset = config_old.axisScale.l_x.offset;
            config.axisScale.l_x.deadzone = Int16.MaxValue;
            config.axisScale.l_y.multiplier = config_old.axisScale.l_y.multiplier;
            config.axisScale.l_y.offset = config_old.axisScale.l_y.offset;
            config.axisScale.l_y.deadzone = Int16.MaxValue;
            config.axisScale.r_x.multiplier = config_old.axisScale.r_x.multiplier;
            config.axisScale.r_x.offset = config_old.axisScale.r_x.offset;
            config.axisScale.r_x.deadzone = Int16.MaxValue;
            config.axisScale.r_y.multiplier = config_old.axisScale.r_y.multiplier;
            config.axisScale.r_y.offset = config_old.axisScale.r_y.offset;
            config.axisScale.r_y.deadzone = Int16.MaxValue;
            config.axisScale.lt.multiplier = config_old.axisScale.lt.multiplier;
            config.axisScale.lt.offset = config_old.axisScale.lt.offset;
            config.axisScale.lt.deadzone = Int16.MaxValue;
            config.axisScale.rt.multiplier = config_old.axisScale.rt.multiplier;
            config.axisScale.rt.offset = config_old.axisScale.rt.offset;
            config.axisScale.rt.deadzone = Int16.MaxValue;
            config.pinsSP = config_old.pinsSP;
            config.rf = config_old.rf;
            config.debounce.buttons = 5;
            config.debounce.strum = 20;
            config.debounce.combinedStrum = 0;
        }
        else if (version > 10)
        {
            var config_old = StructTools.RawDeserialize<Configuration11>(data, 0);
            config.axisScale.r_x.multiplier = config_old.whammy.multiplier;
            config.axisScale.r_x.offset = (short)config_old.whammy.offset;
            config.pinsSP = config_old.pinsSP;
            config.rf = config_old.rf;
        }
        else if (version > 8)
        {
            var config_old = StructTools.RawDeserialize<Configuration10>(data, 0);
            config.pinsSP = config_old.pinsSP;
            config.rf = config_old.rf;
        }
        else if (version == 8)
        {
            var config_old = StructTools.RawDeserialize<Configuration8>(data, 0);
            config.rf = config_old.rf;
        }


    }

    public override String ToString()
    {
        if (unmigrateable)
        {
            return $"Ardwiino - {board} - {cpuFreq} - {version} - config too old to import";
        }
        return $"Ardwiino - {board} - {cpuFreq} - {version}";
    }

    enum GyroOrientation
    {
        X, Y, Z
    };

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
        public byte up;
        public byte down;
        public byte left;
        public byte right;
        public byte start;
        public byte back;
        public byte left_stick;
        public byte right_stick;
        public byte LB;
        public byte RB;
        public byte home;
        public byte capture;
        public byte a;
        public byte b;
        public byte x;
        public byte y;
        public AnalogPin lt;
        public AnalogPin rt;
        public AnalogPin l_x;
        public AnalogPin l_y;


        public AnalogPin r_x;
        public AnalogPin r_y;
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
        public byte up;
        public byte down;
        public byte left;
        public byte right;
        public byte start;
        public byte back;
        public byte left_stick;
        public byte right_stick;
        public byte LB;
        public byte RB;
        public byte home;
        public byte capture;
        public byte a;
        public byte b;
        public byte x;
        public byte y;
        public byte lt;
        public byte rt;
        public AnalogKey l_x;
        public AnalogKey l_y;
        public AnalogKey r_x;
        public AnalogKey r_y;
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
        public AxisScale lt;
        public AxisScale rt;
        public AxisScale l_x;
        public AxisScale l_y;
        public AxisScale r_x;
        public AxisScale r_y;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AxisScaleConfig12
    {
        public AxisScale12 lt;
        public AxisScale12 rt;
        public AxisScale12 l_x;
        public AxisScale12 l_y;
        public AxisScale12 r_x;
        public AxisScale12 r_y;
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
    public struct Configuration
    {
        public ConfigurationAll all;
        public RFConfig rf;
        public byte pinsSP;
        public AxisScaleConfig axisScale;
        public DebounceConfig debounce;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration13
    {
        public ConfigurationAll all;
        public RFConfig rf;
        public byte pinsSP;
        public AxisScaleConfig axisScale;
        public DebounceConfig13 debounce;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration12
    {
        public ConfigurationAll all;
        public RFConfig rf;
        public byte pinsSP;
        public AxisScaleConfig12 axisScale;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration11
    {
        public ConfigurationAll all;
        public RFConfig rf;
        public byte pinsSP;

        public Version11AxisWhammyConfig whammy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration10
    {

        public ConfigurationAll all;
        public RFConfig rf;
        public byte pinsSP;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration8
    {
        public ConfigurationAll all;
        public RFConfig rf;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Configuration7
    {
        public ConfigurationAll all;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ConfigurationAll
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
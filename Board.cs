using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

namespace GuitarConfiguratorSharp.NetCore
{
    public struct Board
    {
        public string ArdwiinoName { get; }
        public string Name { get; }
        public string Environment { get; }

        public uint CpuFreq { get; }
        public List<uint> ProductIDs { get; }

        public bool HasUsbmcu { get; }

        public Board(string ardwiinoName, string name, uint cpuFreq, string environment, List<uint> productIDs, bool hasUsbmcu)
        {
            ArdwiinoName = ardwiinoName;
            Name = name;
            Environment = environment;
            ProductIDs = productIDs;
            CpuFreq = cpuFreq;
            HasUsbmcu = hasUsbmcu;
        }

        public static readonly Board Generic = new("generic", "Generic Serial Device", 0, "generic", new List<uint>(), false);
        public static readonly Board[] Atmega32U4Boards = {
        new("a-micro", "Arduino Micro in Bootloader Mode", 16000000, "a-micro", new List<uint>{0x0037, 0x0237}, false),
        new("a-micro", "Arduino Micro", 16000000, "a-micro", new List<uint>{0x8037, 0x8237}, false),
        new("micro", "Arduino Pro Micro 3.3V", 8000000, "sparkfun_promicro_8", new List<uint>{0x9204}, false),
        new("micro", "Arduino Pro Micro 5V", 16000000, "sparkfun_promicro_16", new List<uint>{0x9206}, false),
        new("leonardo", "Arduino Leonardo", 16000000, "leonardo", new List<uint>{0x8036, 0x800c}, false),
        new("leonardo", "Arduino Micro / Pro Micro / Leonardo in Bootloader Mode", 16000000, "leonardo", new List<uint>{0x0036}, false),
        new("micro", "Arduino Pro Micro in Bootloader Mode", 8000000, "micro", new List<uint>{0x9203, 0x9207}, false),
        new("micro", "Arduino Pro Micro in Bootloader Mode", 16000000, "micro", new List<uint>{0x9205}, false),
    };

        public static readonly Board[] Rp2040Boards = {
        new("pico", "Raspberry PI Pico", 0, "pico", new List<uint>(), false),
        new("adafruit_feather_rp2040", "Adafruit Feather RP2040", 0, "adafruit_feather_rp2040", new List<uint>(), false),
        new("adafruit_itsybitsy_rp2040", "Adafruit ItsyBitsy RP2040", 0, "adafruit_itsybitsy_rp2040", new List<uint>(), false),
        new("adafruit_feather_rp2040", "Adafruit KB2040", 0, "adafruit_feather_rp2040", new List<uint>(), false),
        new("adafruit_qtpy_rp2040", "Adafruit QT Py RP2040", 0, "adafruit_qtpy_rp2040", new List<uint>(), false),
        new("adafruit_trinkey_qt2040", "Adafruit Trinkey QT2040", 0, "adafruit_trinkey_qt2040", new List<uint>(), false),
        new("arduino_nano_rp2040_connect", "Arduino Nano RP2040 Connect", 0, "arduino_nano_rp2040_connect", new List<uint>(), false),
        new("melopero_shake_rp2040", "Melopero Shake RP2040", 0, "melopero_shake_rp2040", new List<uint>(), false),
        new("pimoroni_interstate75_rp2040", "Pimoroni Interstate 75", 0, "pimoroni_interstate75_rp2040", new List<uint>(), false),
        new("pimoroni_keybow2040", "Pimoroni Keybow 2040", 0, "pimoroni_keybow2040", new List<uint>(), false),
        new("pimoroni_pga2040", "Pimoroni PGA2040", 0, "pimoroni_pga2040", new List<uint>(), false),
        new("pimoroni_picolipo_4mb", "Pimoroni Pico LiPo (4MB)", 0, "pimoroni_picolipo_4mb", new List<uint>(), false),
        new("pimoroni_picolipo_16mb", "Pimoroni Pico LiPo (16MB)", 0, "pimoroni_picolipo_16mb", new List<uint>(), false),
        new("pimoroni_picosystem_rp2040", "Pimoroni PicoSystem", 0, "pimoroni_picosystem_rp2040", new List<uint>(), false),
        new("pimoroni_plasma2040", "Pimoroni Plasma 2040", 0, "pimoroni_plasma2040", new List<uint>(), false),
        new("pimoroni_tiny2040", "Pimoroni Tiny 2040", 0, "pimoroni_tiny2040", new List<uint>(), false),
        new("pybstick26_rp2040", "RP2040 PYBStick", 0, "pybstick26_rp2040", new List<uint>(), false),
        new("sparkfun_micromod_rp2040", "SparkFun MicroMod - RP2040", 0, "sparkfun_micromod_rp2040", new List<uint>(), false),
        new("sparkfun_promicro_rp2040", "SparkFun Pro Micro - RP2040", 0, "sparkfun_promicro_rp2040", new List<uint>(), false),
        new("sparkfun_thingplus_rp2040", "SparkFun Thing Plus - RP2040", 0, "sparkfun_thingplus_rp2040", new List<uint>(), false),
        new("vgaboard_rp2040", "Pimoroni Pico VGA Demo Base", 0, "vgaboard_rp2040", new List<uint>(), false),
        new("waveshare_rp2040_lcd_0.96", "Waveshare RP2040-LCD-0.96", 0, "waveshare_rp2040_lcd_0.96", new List<uint>(), false),
        new("waveshare_rp2040_plus_4mb", "Waveshare RP2040-Plus (4MB)", 0, "waveshare_rp2040_plus_4mb", new List<uint>(), false),
        new("waveshare_rp2040_plus_16mb", "Waveshare RP2040-Plus (16MB)", 0, "waveshare_rp2040_plus_16mb", new List<uint>(), false),
        new("waveshare_rp2040_zero", "Waveshare RP2040-Zero", 0, "waveshare_rp2040_zero", new List<uint>(), false)
    };

        public static readonly Board[] MiniBoards = {
        new("mini", "Arduino Pro Mini 5V", 16000000, "mini", new List<uint>(), false),
        new("mini", "Arduino Pro Mini 3.3V", 8000000, "mini", new List<uint>(), false),
        };
        public static readonly Board[] MegaBoards = {
        new("mega2560-atmega16u2", "Arduino Mega 2560", 0, "arduino_uno_mega_usb16", new List<uint>{0x2FEF}, true),
        new("mega2560-at90usb82", "Arduino Mega 2560", 0, "arduino_uno_mega_usb8", new List<uint>{0x2FF7}, true),
        new("mega2560", "Arduino Mega 2560", 0, "arduino_mega_2560", new List<uint>{0x0010, 0x0042}, true),
        new("megaadk-atmega16u2", "Arduino Mega ADK", 0, "arduino_uno_mega_usb16", new List<uint>{0x2FEF}, true),
        new("megaadk-at90usb82", "Arduino Mega ADK", 0, "arduino_uno_mega_usb8", new List<uint>{0x2FF7}, true),
        new("megaadk", "Arduino Mega ADK", 0, "arduino_mega_adk", new List<uint>{0x003f, 0x0044}, true),
    };
        public static readonly Board[] UnoBoards = {
        new("uno-atmega16u2", "Arduino Uno", 0, "arduino_uno_mega_usb16", new List<uint>{0x2FEF}, true),
        new("uno-at90usb82", "Arduino Uno", 0, "arduino_uno_mega_usb8", new List<uint>{0x2FF7}, true),
        new("uno", "Arduino Uno", 0, "arduino_uno", new List<uint>{0x0043, 0x0001, 0x0243}, true),
       };
        public static readonly Board UsbUpload = new("usb", "Arduino Uno / Mega in Firmware Update Mode", 0, "", new List<uint>{0x2883}, true);

        public static readonly Board[] Boards = UnoBoards
            .Concat(MiniBoards)
            .Concat(MegaBoards)
            .Concat(Atmega32U4Boards)
            .Concat(Rp2040Boards)
            .Concat(new[] { UsbUpload })
            .ToArray();

        public static Board FindBoard(string ardwiinoName, uint cpuFreq)
        {
            foreach (var board in Boards)
            {
                if (board.ArdwiinoName == ardwiinoName && (cpuFreq == 0 || board.CpuFreq == 0 || board.CpuFreq == cpuFreq))
                {
                    return board;
                }
            }
            return Generic;
        }
        public static Microcontroller FindMicrocontroller(Board board)
        {
            if (Atmega32U4Boards.Contains(board))
            {
                return new Micro(board);
            }

            if (UnoBoards.Contains(board) || board.ArdwiinoName == UsbUpload.ArdwiinoName)
            {
                return new Uno(board);
            }

            if (MegaBoards.Contains(board))
            {
                return new Mega(board);
            }

            if (Rp2040Boards.Contains(board))
            {
                return new Pico(board);
            }

            if (MiniBoards.Contains(board))
            {
                throw new NotSupportedException("TODO: support mini");
            }
            throw new NotSupportedException("Not sure how we got here");

        }

        public bool IsAvr()
        {
            return Atmega32U4Boards.Contains(this) || UnoBoards.Contains(this) || MegaBoards.Contains(this) || MiniBoards.Contains(this);
        }
    }
}
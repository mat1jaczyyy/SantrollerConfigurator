
using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.Configuration;

public struct Board
{
    public string ardwiinoName { get; }
    public string name { get; }
    public string environment { get; }

    public uint cpuFreq { get; }
    public List<uint> productIDs { get; }

    public Board(string ardwiinoName, string name, uint cpuFreq, string environment, List<uint> productIDs)
    {
        this.ardwiinoName = ardwiinoName;
        this.name = name;
        this.environment = environment;
        this.productIDs = productIDs;
        this.cpuFreq = cpuFreq;
    }

    public static readonly Board Generic = new Board("generic", "Generic Serial Device", 0, "generic", new List<uint> { });
    public static readonly Board[] ATMEGA32U4Boards = {
        new Board("a-micro", "Arduino Micro in Bootloader Mode", 16000000, "a-micro", new List<uint>{0x0037, 0x0237}),
        new Board("a-micro", "Arduino Micro", 16000000, "a-micro", new List<uint>{0x8037, 0x8237}),
        new Board("micro", "Arduino Pro Micro 3.3V", 8000000, "micro", new List<uint>{0x9204}),
        new Board("micro", "Arduino Pro Micro 5V", 16000000, "micro", new List<uint>{0x9206}),
        new Board("leonardo", "Arduino Leonardo", 16000000, "leonardo", new List<uint>{0x8036, 0x800c}),
        new Board("leonardo", "Arduino Micro / Pro Micro / Leonardo in Bootloader Mode", 16000000, "leonardo", new List<uint>{0x0036}),
        new Board("micro", "Arduino Pro Micro in Bootloader Mode", 8000000, "micro", new List<uint>{0x9203, 0x9207}),
        new Board("micro", "Arduino Pro Micro in Bootloader Mode", 16000000, "micro", new List<uint>{0x9205}),
    };

    public static readonly Board[] RP2040Boards = {
        new Board("pico", "Raspberry PI Pico", 0, "pico", new List<uint>{}),
        new Board("adafruit_feather_rp2040", "Adafruit Feather RP2040", 0, "adafruit_feather_rp2040", new List<uint>{}),
        new Board("adafruit_itsybitsy_rp2040", "Adafruit ItsyBitsy RP2040", 0, "adafruit_itsybitsy_rp2040", new List<uint>{}),
        new Board("adafruit_feather_rp2040", "Adafruit KB2040", 0, "adafruit_feather_rp2040", new List<uint>{}),
        new Board("adafruit_qtpy_rp2040", "Adafruit QT Py RP2040", 0, "adafruit_qtpy_rp2040", new List<uint>{}),
        new Board("adafruit_trinkey_qt2040", "Adafruit Trinkey QT2040", 0, "adafruit_trinkey_qt2040", new List<uint>{}),
        new Board("arduino_nano_rp2040_connect", "Arduino Nano RP2040 Connect", 0, "arduino_nano_rp2040_connect", new List<uint>{}),
        new Board("melopero_shake_rp2040", "Melopero Shake RP2040", 0, "melopero_shake_rp2040", new List<uint>{}),
        new Board("pimoroni_interstate75_rp2040", "Pimoroni Interstate 75", 0, "pimoroni_interstate75_rp2040", new List<uint>{}),
        new Board("pimoroni_keybow2040", "Pimoroni Keybow 2040", 0, "pimoroni_keybow2040", new List<uint>{}),
        new Board("pimoroni_pga2040", "Pimoroni PGA2040", 0, "pimoroni_pga2040", new List<uint>{}),
        new Board("pimoroni_picolipo_4mb", "Pimoroni Pico LiPo (4MB)", 0, "pimoroni_picolipo_4mb", new List<uint>{}),
        new Board("pimoroni_picolipo_16mb", "Pimoroni Pico LiPo (16MB)", 0, "pimoroni_picolipo_16mb", new List<uint>{}),
        new Board("pimoroni_picosystem_rp2040", "Pimoroni PicoSystem", 0, "pimoroni_picosystem_rp2040", new List<uint>{}),
        new Board("pimoroni_plasma2040", "Pimoroni Plasma 2040", 0, "pimoroni_plasma2040", new List<uint>{}),
        new Board("pimoroni_tiny2040", "Pimoroni Tiny 2040", 0, "pimoroni_tiny2040", new List<uint>{}),
        new Board("pybstick26_rp2040", "RP2040 PYBStick", 0, "pybstick26_rp2040", new List<uint>{}),
        new Board("sparkfun_micromod_rp2040", "SparkFun MicroMod - RP2040", 0, "sparkfun_micromod_rp2040", new List<uint>{}),
        new Board("sparkfun_promicro_rp2040", "SparkFun Pro Micro - RP2040", 0, "sparkfun_promicro_rp2040", new List<uint>{}),
        new Board("sparkfun_thingplus_rp2040", "SparkFun Thing Plus - RP2040", 0, "sparkfun_thingplus_rp2040", new List<uint>{}),
        new Board("vgaboard_rp2040", "Pimoroni Pico VGA Demo Base", 0, "vgaboard_rp2040", new List<uint>{}),
        new Board("waveshare_rp2040_lcd_0.96", "Waveshare RP2040-LCD-0.96", 0, "waveshare_rp2040_lcd_0.96", new List<uint>{}),
        new Board("waveshare_rp2040_plus_4mb", "Waveshare RP2040-Plus (4MB)", 0, "waveshare_rp2040_plus_4mb", new List<uint>{}),
        new Board("waveshare_rp2040_plus_16mb", "Waveshare RP2040-Plus (16MB)", 0, "waveshare_rp2040_plus_16mb", new List<uint>{}),
        new Board("waveshare_rp2040_zero", "Waveshare RP2040-Zero", 0, "waveshare_rp2040_zero", new List<uint>{})
    };

    public static readonly Board[] MiniBoards = new Board[]{
        new Board("mini", "Arduino Pro Mini 5V", 16000000, "mini", new List<uint>{}),
        new Board("mini", "Arduino Pro Mini 3.3V", 8000000, "mini", new List<uint>{}),
        };
    public static readonly Board[] MegaBoards = new Board[]{
        new Board("mega2560-atmega16u2", "Arduino Mega 2560", 0, "mega2560-atmega16u2", new List<uint>{0x2FEF}),
        new Board("mega2560-at90usb82", "Arduino Mega 2560", 0, "mega2560-at90usb82", new List<uint>{0x2FF7}),
        new Board("mega2560", "Arduino Mega 2560", 0, "mega2560", new List<uint>{0x0010, 0x0042}),
        new Board("megaadk-atmega16u2", "Arduino Mega ADK", 0, "megaadk-atmega16u2", new List<uint>{0x2FEF}),
        new Board("megaadk-at90usb82", "Arduino Mega ADK", 0, "megaadk-at90usb82", new List<uint>{0x2FF7}),
        new Board("megaadk", "Arduino Mega ADK", 0, "megaadk", new List<uint>{0x003f, 0x0044}),
    };
    public static readonly Board[] UnoBoards = new Board[]{
        new Board("uno-atmega16u2", "Arduino Uno", 0, "uno-atmega16u2", new List<uint>{0x2FEF}),
        new Board("uno-at90usb82", "Arduino Uno", 0, "uno-at90usb82", new List<uint>{0x2FF7}),
        new Board("uno", "Arduino Uno", 0, "uno", new List<uint>{0x0043, 0x7523, 0x0001, 0xea60, 0x0243}),
       };

    public static readonly Board[] Boards = UnoBoards
        .Concat(MiniBoards)
        .Concat(MegaBoards)
        .Concat(ATMEGA32U4Boards)
        .Concat(RP2040Boards)
        .ToArray();

    public static Board findBoard(string ardwiinoName, uint cpuFreq)
    {
        foreach (var board in Boards)
        {
            if (board.ardwiinoName == ardwiinoName && (cpuFreq == 0 || board.cpuFreq == 0 || board.cpuFreq == cpuFreq))
            {
                return board;
            }
        }
        return Generic;
    }
    public static Microcontroller findMicrocontroller(Board board)
    {
        if (Board.ATMEGA32U4Boards.Contains(board))
        {
            return new Micro();
        }
        else if (Board.UnoBoards.Contains(board))
        {
            return new Uno();
        }
        else if (Board.MegaBoards.Contains(board))
        {
            return new Mega();
        }
        else if (Board.RP2040Boards.Contains(board))
        {
            return new Pico();
        }
        else if (Board.MiniBoards.Contains(board))
        {
            throw new NotSupportedException("TODO: support mini");
        }
        throw new NotSupportedException("Not sure how we got here");

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;

namespace GuitarConfigurator.NetCore;

public struct Board
{
    public static readonly short RaspberryPiVendorID = 0x2e8a;
    public string ArdwiinoName { get; }
    public string Name { get; }
    public string Environment { get; }

    public uint CpuFreq { get; }
    public List<uint> ProductIDs { get; }

    public bool HasUsbmcu { get; }

    public Board(string ardwiinoName, string name, uint cpuFreq, string environment, List<uint> productIDs,
        bool hasUsbmcu)
    {
        ArdwiinoName = ardwiinoName;
        Name = name;
        Environment = environment;
        ProductIDs = productIDs;
        CpuFreq = cpuFreq;
        HasUsbmcu = hasUsbmcu;
    }

    public static readonly Board Generic = new("generic", "Generic Serial Device", 0, "generic", new List<uint>(),
        false);

    public static readonly Board[] Atmega32U4Boards =
    {
        new("a-micro", "Arduino Micro in Bootloader Mode", 16000000, "arduino_micro_16",
            new List<uint> {0x0037, 0x0237},
            false),
        new("a-micro", "Arduino Micro", 16000000, "arduino_micro_16", new List<uint> {0x8037, 0x8237}, false),
        new("micro", "Sparkfun Pro Micro 3.3V", 8000000, "sparkfun_promicro_8", new List<uint> {0x9204}, false),
        new("micro", "Sparkfun Pro Micro 5V", 16000000, "sparkfun_promicro_16", new List<uint> {0x9206}, false),
        new("leonardo", "Arduino Leonardo", 16000000, "arduino_leonardo_16", new List<uint> {0x8036, 0x800c}, false),
        new("leonardo", "Arduino Leonardo 3.3V", 8000000, "arduino_leonardo_8", new List<uint>(), false),
        new("leonardo", "Arduino Micro / Pro Micro / Leonardo in Bootloader Mode", 16000000, "leonardo",
            new List<uint> {0x0036}, false),
        new("micro", "Arduino Pro Micro in Bootloader Mode", 8000000, "sparkfun_promicro_8",
            new List<uint> {0x9203, 0x9207},
            false),
        new("micro", "Arduino Pro Micro in Bootloader Mode", 16000000, "sparkfun_promicro_16", new List<uint> {0x9205},
            false)
    };

    public static readonly Board[] Rp2040Boards =
    {
        new("pico", "Raspberry PI Pico", 0, "pico", new List<uint> {0x000a}, false),
        new("rpipicow", "Raspberry PI Pico W", 0, "picow", new List<uint>(0xf00a), false),
        //Raspberry Pi
        new("rpipico", "Raspberry Pi Pico", 0, "rpipico", new List<uint> {0x000a}, false),
        new("rpipicow", "Raspberry Pi Pico W", 0, "rpipicow", new List<uint> {0xf00a}, false),

        //0xCB
        new("0xcb_helios", "0xCB Helios", 0, "0xcb_helios", new List<uint> {0xCB74}, false),

        //Adafruit
        new("adafruit_feather", "Adafruit Feather RP2040", 0, "adafruit_feather", new List<uint> {0x80f1}, false),
        new("adafruit_feather_scorpio", "Adafruit Feather RP2040 SCORPIO", 0, "adafruit_feather_scorpio",
            new List<uint> {0x8121}, false),
        new("adafruit_feather_dvi", "Adafruit Feather RP2040 DVI", 0, "adafruit_feather_dvi", new List<uint> {0x8127},
            false),
        new("adafruit_itsybitsy", "Adafruit ItsyBitsy RP2040", 0, "adafruit_itsybitsy", new List<uint> {0x80fd}, false),
        new("adafruit_qtpy", "Adafruit QT Py RP2040", 0, "adafruit_qtpy", new List<uint> {0x80f7}, false),
        new("adafruit_stemmafriend", "Adafruit STEMMA Friend RP2040", 0, "adafruit_stemmafriend",
            new List<uint> {0x80e3}, false),
        new("adafruit_trinkeyrp2040qt", "Adafruit Trinkey RP2040 QT", 0, "adafruit_trinkeyrp2040qt",
            new List<uint> {0x8109}, false),
        new("adafruit_macropad2040", "Adafruit MacroPad RP2040", 0, "adafruit_macropad2040", new List<uint> {0x8107},
            false),
        new("adafruit_kb2040", "Adafruit KB2040", 0, "adafruit_kb2040", new List<uint> {0x8105}, false),

        //Arduino
        new("arduino_nano_connect", "Arduino Nano RP2040 Connect", 0, "arduino_nano_connect",
            new List<uint> {0x005e, 0x805e, 0x015e, 0x025e}, false),

        //BridgeTek
        new("bridgetek_idm2040-7a", "BridgeTek IDM2040-7A", 0, "bridgetek_idm2040-7a", new List<uint> {0x1041}, false),

        //Cytron
        new("cytron_maker_nano_rp2040", "Cytron Maker Nano RP2040", 0, "cytron_maker_nano_rp2040",
            new List<uint> {0x100f}, false),
        new("cytron_maker_pi_rp2040", "Cytron Maker Pi RP2040", 0, "cytron_maker_pi_rp2040", new List<uint> {0x1000},
            false),

        //DatanoiseTV
        new("datanoisetv_picoadk", "DatanoiseTV PicoADK", 0, "datanoisetv_picoadk", new List<uint> {0x000a}, false),

        //DeRuiLab
        new("flyboard2040_core", "DeRuiLab FlyBoard2040Core", 0, "flyboard2040_core", new List<uint> {0x008a}, false),

        //DFRobot
        new("dfrobot_beetle_rp2040", "DFRobot Beetle RP2040", 0, "dfrobot_beetle_rp2040", new List<uint> {0x4253},
            false),

        //ElectronicCat
        new("electroniccats_huntercat_nfc", "ElectronicCats HunterCat NFC RP2040", 0, "electroniccats_huntercat_nfc",
            new List<uint> {0x1037}, false),

        //ExtremeElectronics
        new("extelec_rc2040", "ExtremeElectronics RC2040", 0, "extelec_rc2040", new List<uint> {0xee20}, false),

        //iLabs
        new("challenger_2040_lte", "iLabs Challenger 2040 LTE", 0, "challenger_2040_lte", new List<uint> {0x100b},
            false),
        new("challenger_2040_lora", "iLabs Challenger 2040 LoRa", 0, "challenger_2040_lora", new List<uint> {0x1023},
            false),
        new("challenger_2040_subghz", "iLabs Challenger 2040 SubGHz", 0, "challenger_2040_subghz",
            new List<uint> {0x1032}, false),
        new("challenger_2040_wifi", "iLabs Challenger 2040 WiFi", 0, "challenger_2040_wifi", new List<uint> {0x1006},
            false),
        new("challenger_2040_wifi_ble", "iLabs Challenger 2040 WiFi/BLE", 0, "challenger_2040_wifi_ble",
            new List<uint> {0x102C}, false),
        new("challenger_nb_2040_wifi", "iLabs Challenger NB 2040 WiFi", 0, "challenger_nb_2040_wifi",
            new List<uint> {0x100d}, false),
        new("challenger_2040_sdrtc", "iLabs Challenger 2040 SD/RTC", 0, "challenger_2040_sdrtc",
            new List<uint> {0x102d}, false),
        new("challenger_2040_nfc", "iLabs Challenger 2040 NFC", 0, "challenger_2040_nfc", new List<uint> {0x1036},
            false),
        new("ilabs_rpico32", "iLabs RPICO32", 0, "ilabs_rpico32", new List<uint> {0x1010}, false),

        //Melopero
        new("melopero_cookie_rp2040", "Melopero Cookie RP2040", 0, "melopero_cookie_rp2040", new List<uint> {0x1011},
            false),
        new("melopero_shake_rp2040", "Melopero Shake RP2040", 0, "melopero_shake_rp2040", new List<uint> {0x1005},
            false),

        //nullbits
        new("nullbits_bit_c_pro", "nullbits Bit-C PRO", 0, "nullbits_bit_c_pro", new List<uint> {0x6e61}, false),

        //Pimoroni
        new("pimoroni_pga2040", "Pimoroni PGA2040", 0, "pimoroni_pga2040", new List<uint> {0x1008}, false),

        //Solder Party
        new("solderparty_rp2040_stamp", "Solder Party RP2040 Stamp", 0, "solderparty_rp2040_stamp",
            new List<uint> {0xa182}, false),

        //SparkFun
        new("sparkfun_promicrorp2040", "SparkFun ProMicro RP2040", 0, "sparkfun_promicrorp2040",
            new List<uint> {0x0026}, false),
        new("sparkfun_thingplusrp2040", "SparkFun Thing Plus RP2040", 0, "sparkfun_thingplusrp2040",
            new List<uint> {0x0026}, false),

        //Upesy
        new("upesy_rp2040_devkit", "uPesy RP2040 DevKit", 0, "upesy_rp2040_devkit", new List<uint> {0x1007}, false),

        //Seeed
        new("seeed_xiao_rp2040", "Seeed XIAO RP2040", 0, "seeed_xiao_rp2040", new List<uint> {0x000a}, false),

        //VCC-GND YD-2040 - Use generic SPI/4 because boards seem to come with varied flash modules but same name
        new("vccgnd_yd_rp2040", "VCC-GND YD RP2040 0x2e8a", 0, "vccgnd_yd_rp2040", new List<uint> {0x800a}, false),

        //Viyalab
        new("viyalab_mizu", "Viyalab Mizu RP2040", 0, "viyalab_mizu", new List<uint> {0x000a}, false),

        //Waveshare
        new("waveshare_rp2040_zero", "Waveshare RP2040 Zero", 0, "waveshare_rp2040_zero", new List<uint> {0x0003},
            false),
        new("waveshare_rp2040_one", "Waveshare RP2040 One", 0, "waveshare_rp2040_one", new List<uint> {0x103a}, false),
        new("waveshare_rp2040_plus_4mb", "Waveshare RP2040 Plus 4MB", 0, "waveshare_rp2040_plus_4mb",
            new List<uint> {0x1020}, false),
        new("waveshare_rp2040_plus_16mb", "Waveshare RP2040 Plus 16MB", 0, "waveshare_rp2040_plus_16mb",
            new List<uint> {0x1020}, false),
        new("waveshare_rp2040_lcd_0_96", "Waveshare RP2040 LCD 0.96", 0, "waveshare_rp2040_lcd_0_96",
            new List<uint> {0x1021}, false),
        new("waveshare_rp2040_lcd_1_28", "Waveshare RP2040 LCD 1.28", 0, "waveshare_rp2040_lcd_1_28",
            new List<uint> {0x1039}, false),

        //WIZnet
        new("wiznet_5100s_evb_pico", "WIZnet W5100S-EVB-Pico", 0, "wiznet_5100s_evb_pico", new List<uint> {0x1027},
            false),
        new("wiznet_wizfi360_evb_pico", "WIZnet WizFi360-EVB-Pico", 0, "wiznet_wizfi360_evb_pico",
            new List<uint> {0x1028}, false),
        new("wiznet_5500_evb_pico", "WIZnet W5500-EVB-Pico", 0, "wiznet_5500_evb_pico", new List<uint> {0x1029}, false),
    };

    public static readonly Board[] MiniBoards =
    {
        new("mini", "Arduino Pro Mini 5V", 16000000, "arduino_mini_16", new List<uint>(), false),
        new("mini", "Arduino Pro Mini 3.3V", 8000000, "arduino_mini_8", new List<uint>(), false)
    };

    public static readonly Board[] MegaBoards =
    {
        new("mega2560", "Arduino Mega 2560", 0, "arduino_mega_2560", new List<uint> {0x0010, 0x0042}, true),
        new("megaadk", "Arduino Mega ADK", 0, "arduino_mega_adk", new List<uint> {0x003f, 0x0044}, true)
    };

    public static readonly Board DfuBoard = new("usb", "Arduino Uno / Mega / Mega ADK in DFU mode", 0, "",
        new List<uint> {0x2FF7, 0x2FEF}, true);

    public static readonly Board Uno = new("uno", "Arduino Uno", 0, "arduino_uno",
        new List<uint> {0x0043, 0x0001, 0x0243}, true);

    public static readonly Board UsbUpload = new("usb", "Arduino Uno / Mega in Firmware Update Mode", 0, "",
        new List<uint> {0x2883}, true);

    public static readonly Board[] Boards = MiniBoards
        .Concat(MegaBoards)
        .Concat(Atmega32U4Boards)
        .Concat(Rp2040Boards)
        .Concat(new[] {UsbUpload, Uno, DfuBoard})
        .ToArray();

    public static Board FindBoard(string ardwiinoName, uint cpuFreq)
    {
        foreach (var board in Boards)
            if (board.ArdwiinoName == ardwiinoName &&
                (cpuFreq == 0 || board.CpuFreq == 0 || board.CpuFreq == cpuFreq))
                return board;

        return Generic;
    }

    public static Microcontroller FindMicrocontroller(Board board)
    {
        if (Atmega32U4Boards.Contains(board)) return new Micro(board);

        if (board.Name == Uno.Name) return new Uno(board);

        if (MegaBoards.Contains(board)) return new Mega(board);

        if (Rp2040Boards.Contains(board)) return new Pico(board);

        // In terms of pin layout, the uno is close enough to a mini
        if (MiniBoards.Contains(board)) return new Uno(board);

        throw new NotSupportedException("Not sure how we got here");
    }

    public bool IsAvr()
    {
        return Atmega32U4Boards.Contains(this) || Name == Uno.Name || MegaBoards.Contains(this) ||
               MiniBoards.Contains(this);
    }

    public bool IsPico()
    {
        return Rp2040Boards.Contains(this);
    }

    public bool IsGeneric()
    {
        return Generic.Name == Name;
    }

    public bool IsMini()
    {
        return MiniBoards.Contains(this);
    }
}
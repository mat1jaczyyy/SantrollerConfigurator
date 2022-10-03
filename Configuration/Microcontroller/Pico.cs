using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Kernel;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class Pico : Microcontroller
{
    private const int GpioCount = 30;
    private const int PinA0 = 26;

    public override Board Board { get; }

    public Pico(Board board)
    {
        Board = board;
    }

    public override string GenerateDigitalRead(int pin, bool pullUp)
    {
        // Invert on pullup
        if (pullUp)
        {
            return $"(sio_hw->gpio_in & (1 << {pin})) == 0";
        }

        return $"sio_hw->gpio_in & (1 << {pin})";
    }

    public static readonly Dictionary<int, SpiPinType> SpiTypesByPin = new()
    {
        {0, SpiPinType.MISO},
        {1, SpiPinType.CSn},
        {2, SpiPinType.SCK},
        {3, SpiPinType.MOSI},
        {4, SpiPinType.MISO},
        {5, SpiPinType.CSn},
        {6, SpiPinType.SCK},
        {7, SpiPinType.MOSI},
        {19, SpiPinType.MOSI},
        {18, SpiPinType.SCK},
        {17, SpiPinType.CSn},
        {16, SpiPinType.MISO},
        {8, SpiPinType.MISO},
        {9, SpiPinType.CSn},
        {10, SpiPinType.SCK},
        {11, SpiPinType.MOSI},
        {12, SpiPinType.MISO},
        {13, SpiPinType.CSn},
        {14, SpiPinType.SCK},
        {15, SpiPinType.MOSI},
    };

    public static readonly Dictionary<int, int> SpiIndexByPin = new()
    {
        {0, 0},
        {1, 0},
        {2, 0},
        {3, 0},
        {4, 0},
        {5, 0},
        {6, 0},
        {7, 0},
        {19, 0},
        {18, 0},
        {17, 0},
        {16, 0},
        {8, 1},
        {9, 1},
        {10, 1},
        {11, 1},
        {12, 1},
        {13, 1},
        {14, 1},
        {15, 1},
    };

    public static readonly Dictionary<int, int> TwiIndexByPin = new()
    {
        {0, 0},
        {1, 0},
        {2, 1},
        {3, 1},
        {4, 0},
        {5, 0},
        {6, 1},
        {7, 1},
        {8, 0},
        {9, 0},
        {10, 1},
        {11, 1},
        {12, 0},
        {13, 0},
        {14, 1},
        {15, 1},
        {16, 0},
        {17, 0},
        {18, 1},
        {19, 1},
        {20, 0},
        {21, 0},
        {26, 1},
        {27, 1},
    };

    public static readonly Dictionary<int, TwiPinType> TwiTypeByPin = new()
    {
        {0, TwiPinType.SDA},
        {1, TwiPinType.SCL},
        {2, TwiPinType.SDA},
        {3, TwiPinType.SCL},
        {4, TwiPinType.SDA},
        {5, TwiPinType.SCL},
        {6, TwiPinType.SDA},
        {7, TwiPinType.SCL},
        {8, TwiPinType.SDA},
        {9, TwiPinType.SCL},
        {10, TwiPinType.SDA},
        {11, TwiPinType.SCL},
        {12, TwiPinType.SDA},
        {13, TwiPinType.SCL},
        {14, TwiPinType.SDA},
        {15, TwiPinType.SCL},
        {16, TwiPinType.SDA},
        {17, TwiPinType.SCL},
        {18, TwiPinType.SDA},
        {19, TwiPinType.SCL},
        {20, TwiPinType.SDA},
        {21, TwiPinType.SCL},
        {26, TwiPinType.SDA},
        {27, TwiPinType.SCL},
    };


    public override SpiConfig? AssignSpiPins(string type, int mosi, int miso, int sck, bool cpol, bool cpha,
        bool msbfirst,
        int clock)
    {
        int pin = SpiIndexByPin[mosi];
        if (pin != SpiIndexByPin[miso] || SpiTypesByPin[mosi] != SpiPinType.MOSI || SpiTypesByPin[miso] != SpiPinType.MISO)
        {
            return null;
        }

        PicoSpiConfig? config = SpiConfigs.Cast<PicoSpiConfig>().FirstOrDefault(c => c.Type == type);
        if (config != null)
        {
            return config;
        }
        config = SpiConfigs.Cast<PicoSpiConfig>().FirstOrDefault(c => c.Index == pin);
        if (config != null) return null;
        config = new PicoSpiConfig(type, mosi, miso, sck, cpol, cpha, msbfirst, clock);
        SpiConfigs.Add(config);
        return config;
    }

    public override TwiConfig? AssignTwiPins(string type, int sda, int scl, int clock)
    {
        int pin = TwiIndexByPin[sda];
        if (pin != TwiIndexByPin[scl] || TwiTypeByPin[sda] != TwiPinType.SDA || TwiTypeByPin[scl] != TwiPinType.SCL)
        {
            return null;
        }

        PicoTwiConfig? config = TwiConfigs.Cast<PicoTwiConfig>().FirstOrDefault(c => c.Type == type);
        if (config != null)
        {
            return config;
        }
        config = TwiConfigs.Cast<PicoTwiConfig>().FirstOrDefault(c => c.Index == pin);
        if (config != null) return null;
        config = new PicoTwiConfig(type,  sda, scl, clock);
        TwiConfigs.Add(config);
        return config;
    }

    public override bool HasConfigurableSpiPins => true;
    public override bool HasConfigurableTwiPins => true;

    public override bool TwiPinsFree => TwiConfigs.Count < 2;
    public override bool SpiPinsFree => SpiConfigs.Count < 2;
    public override List<KeyValuePair<int, SpiPinType>> SpiPins(string type)
    {
        var types = SpiTypesByPin.AsEnumerable();
        foreach (var config in SpiConfigs.Cast<PicoSpiConfig>())
        {
            if (config.Type != type)
            {
                types = types.Where(s => SpiIndexByPin[s.Key] != config.Index).ToDictionary(s => s.Key, s => s.Value);
            }   
        }
        return types.ToList();
    }

    public override List<KeyValuePair<int, TwiPinType>> TwiPins(string type)
    {
        var types = TwiTypeByPin.AsEnumerable();
        foreach (var config in TwiConfigs.Cast<PicoTwiConfig>())
        {
            if (config.Type != type)
            {
                types = types.Where(s => TwiIndexByPin[s.Key] != config.Index).ToDictionary(s => s.Key, s => s.Value);
            }   
        }
        return types.ToList();
    }

    public override void UnAssignSPIPins(string type)
    {
        SpiConfigs.AsList().RemoveAll(s => ((PicoSpiConfig)s).Type == type);
    }

    public override void UnAssignTWIPins(string type)
    {
        TwiConfigs.AsList().RemoveAll(s => s.Type == type);
    }

    public override string GenerateDefinitions()
    {
        List<int> skippedPins = new List<int>();
        foreach (var config in SpiConfigs)
        {
            skippedPins.AddRange(config.GetPins());
        }
        foreach (var config in TwiConfigs)
        {
            skippedPins.AddRange(config.GetPins());
        }

        int skip = 0;
        foreach (var pin in skippedPins)
        {
            if (pin != 0xFF)
            {
                skip |= 1 << pin;
            }
        }

        var skipDef = $"#define SKIP_MASK_PICO {{{skip.ToString()}}}";
        return skipDef;
    }

    public override string GenerateInit(List<Output> bindings)
    {
        string ret = "";
        foreach (var output in bindings)
        {
            if (output.Input?.InnermostInput() is DirectInput direct)
            {
                if (direct.IsAnalog)
                {
                    ret += $"adc_gpio_init({direct.Pin});";
                }
                else
                {
                    bool up = direct.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullDown;
                    bool down = direct.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullUp;
                    ret += $"gpio_init({direct.Pin});";
                    ret += $"gpio_set_dir({direct.Pin},false);";
                    ret += $"gpio_set_pulls({direct.Pin},{up.ToString().ToLower()},{down.ToString().ToLower()});";
                }
            }
        }

        return ret;
    }

    public override int GetChannel(int pin)
    {
        return pin;
    }

    public override string GetPin(int pin)
    {
        string ret = $"GP{pin}";
        if (pin >= 26)
        {
            ret += $" / ADC{pin - 26}";
        }

        return ret;
    }
}
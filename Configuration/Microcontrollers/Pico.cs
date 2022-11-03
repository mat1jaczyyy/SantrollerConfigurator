using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class Pico : Microcontroller
{
    private const int GpioCount = 30;
    private const int PinA0 = 26;

    public override Board Board { get; }
    public override string GeneratePulseRead(int pin, PulseMode mode, int timeout)
    {
        return $"puseIn({pin},{mode},{timeout})";
    }

    public override int GetFirstAnalogPin()
    {
        return PinA0;
    }

    public Pico(Board board)
    {
        Board = board;
    }

    public override string GenerateDigitalRead(int pin, bool pullUp)
    {
        Console.WriteLine(pin);
        // Invert on pullup
        if (pullUp)
        {
            return $"(sio_hw->gpio_in & (1 << {pin})) == 0";
        }

        return $"sio_hw->gpio_in & (1 << {pin})";
    }

    public override string GenerateDigitalWrite(int pin, bool val)
    {
        if (val)
        {
            return $"sio_hw->gpio_out = {1 << pin}";
        }

        return $"sio_hw->gpio_clr = {1 << pin}";
    }

    public static readonly Dictionary<int, SpiPinType> SpiTypesByPin = new()
    {
        {0, SpiPinType.Miso},
        {1, SpiPinType.CSn},
        {2, SpiPinType.Sck},
        {3, SpiPinType.Mosi},
        {4, SpiPinType.Miso},
        {5, SpiPinType.CSn},
        {6, SpiPinType.Sck},
        {7, SpiPinType.Mosi},
        {19, SpiPinType.Mosi},
        {18, SpiPinType.Sck},
        {17, SpiPinType.CSn},
        {16, SpiPinType.Miso},
        {8, SpiPinType.Miso},
        {9, SpiPinType.CSn},
        {10, SpiPinType.Sck},
        {11, SpiPinType.Mosi},
        {12, SpiPinType.Miso},
        {13, SpiPinType.CSn},
        {14, SpiPinType.Sck},
        {15, SpiPinType.Mosi},
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
        {0, TwiPinType.Sda},
        {1, TwiPinType.Scl},
        {2, TwiPinType.Sda},
        {3, TwiPinType.Scl},
        {4, TwiPinType.Sda},
        {5, TwiPinType.Scl},
        {6, TwiPinType.Sda},
        {7, TwiPinType.Scl},
        {8, TwiPinType.Sda},
        {9, TwiPinType.Scl},
        {10, TwiPinType.Sda},
        {11, TwiPinType.Scl},
        {12, TwiPinType.Sda},
        {13, TwiPinType.Scl},
        {14, TwiPinType.Sda},
        {15, TwiPinType.Scl},
        {16, TwiPinType.Sda},
        {17, TwiPinType.Scl},
        {18, TwiPinType.Sda},
        {19, TwiPinType.Scl},
        {20, TwiPinType.Sda},
        {21, TwiPinType.Scl},
        {26, TwiPinType.Sda},
        {27, TwiPinType.Scl},
    };


    public override SpiConfig? AssignSpiPins(string type, int mosi, int miso, int sck, bool cpol, bool cpha,
        bool msbfirst,
        int clock)
    {
        var pin = SpiIndexByPin[mosi];
        if (pin != SpiIndexByPin[miso] || SpiTypesByPin[mosi] != SpiPinType.Mosi || SpiTypesByPin[miso] != SpiPinType.Miso)
        {
            return null;
        }

        var config = PinConfigs.OfType<PicoSpiConfig>().FirstOrDefault(c => c.Type == type);
        if (config != null)
        {
            return config;
        }
        if (PinConfigs.Any(c => c is PicoSpiConfig s && s.Index == pin)) return null;
        config = new PicoSpiConfig(type, mosi, miso, sck, cpol, cpha, msbfirst, clock);
        PinConfigs.Add(config);
        return config;
    }

    public override TwiConfig? AssignTwiPins(string type, int sda, int scl, int clock)
    {
        var pin = TwiIndexByPin[sda];
        if (pin != TwiIndexByPin[scl] || TwiTypeByPin[sda] != TwiPinType.Sda || TwiTypeByPin[scl] != TwiPinType.Scl)
        {
            return null;
        }

        var config = PinConfigs.OfType<PicoTwiConfig>().FirstOrDefault(c => c.Type == type);
        if (config != null)
        {
            return config;
        }
        if (PinConfigs.Any(c => c is PicoTwiConfig s && s.Index == pin)) return null;
        config = new PicoTwiConfig(type,  sda, scl, clock);
        PinConfigs.Add(config);
        return config;
    }

    public override string GenerateAckDefines(int ack)
    {
        return "";
    }

    public override List<int> SupportedAckPins()
    {
        return Enumerable.Range(0, GpioCount).ToList();
    }

    public override List<KeyValuePair<int, SpiPinType>> SpiPins(string type)
    {
        var types = SpiTypesByPin.AsEnumerable();
        foreach (var config in PinConfigs.OfType<PicoSpiConfig>())
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
        foreach (var config in PinConfigs.OfType<PicoTwiConfig>())
        {
            if (config.Type != type)
            {
                types = types.Where(s => TwiIndexByPin[s.Key] != config.Index).ToDictionary(s => s.Key, s => s.Value);
            }   
        }
        return types.ToList();
    }

    public override void UnAssignPins(string type)
    {
        var elements = PinConfigs.Where(s => s.Type == type).ToList();
        PinConfigs.RemoveAll(elements);
    }
    public override void AssignPin(PinConfig pinConfig)
    {
        UnAssignPins(pinConfig.Type);
        PinConfigs.Add(pinConfig);
    }

    public override string GenerateInit()
    {
        var ret = "";
        var pins = PinConfigs.OfType<DirectPinConfig>();
        foreach (var devicePin in pins)
        {
            if (devicePin.PinMode == DevicePinMode.Analog)
            {
                ret += $"adc_gpio_init({devicePin.Pin});";
            }
            else
            {
                var up = devicePin.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullDown;
                var down = devicePin.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullUp;
                ret += $"gpio_init({devicePin.Pin});";
                ret += $"gpio_set_dir({devicePin.Pin},{(devicePin.PinMode == DevicePinMode.Output).ToString().ToLower()});";
                ret += $"gpio_set_pulls({devicePin.Pin},{up.ToString().ToLower()},{down.ToString().ToLower()});";
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
        var ret = $"GP{pin}";
        if (pin >= 26)
        {
            ret += $" / ADC{pin - 26}";
        }

        return ret;
    }
    
    public override List<int> GetFreePins()
    {
        var used = PinConfigs.SelectMany(s => s.Pins).ToHashSet();
        return Enumerable.Range(0, GpioCount).Where(s => !used.Contains(s)).ToList();
    }
}
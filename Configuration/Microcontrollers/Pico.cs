using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class Pico : Microcontroller
{
    private const int GpioCount = 30;
    private const int PinA0 = 26;

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
        {15, SpiPinType.Mosi}
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
        {15, 1}
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
        {27, 1}
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
        {27, TwiPinType.Scl}
    };

    public Pico(Board board)
    {
        Board = board;
    }

    public override Board Board { get; }

    public override List<int> AnalogPins => new() {26, 27, 28, 29};

    public override string GeneratePulseRead(int pin, PulseMode mode, int timeout)
    {
        return $"pulseIn({pin},{mode},{timeout})";
    }

    public override int GetFirstAnalogPin()
    {
        return PinA0;
    }

    public override string GenerateAnalogRead(int pin)
    {
        return $"adc({pin - PinA0})";
    }

    public override string GenerateDigitalRead(int pin, bool pullUp)
    {
        // Invert on pullup
        return pullUp ? $"(sio_hw->gpio_in & (1 << {pin})) == 0" : $"sio_hw->gpio_in & (1 << {pin})";
    }

    public override string GenerateDigitalWrite(int pin, bool val)
    {
        return val ? $"sio_hw->gpio_set = {1 << pin}" : $"sio_hw->gpio_clr = {1 << pin}";
    }


    public override SpiConfig? AssignSpiPins(ConfigViewModel model, string type, int mosi, int miso, int sck, bool cpol,
        bool cpha,
        bool msbfirst,
        uint clock)
    {
        //Some things don't need MISO (like apa102s)
        if ((mosi != -1 && SpiTypesByPin[mosi] != SpiPinType.Mosi) ||
            (miso != -1 && SpiTypesByPin[miso] != SpiPinType.Miso))
            return null;

        var config = PinConfigs.OfType<PicoSpiConfig>().FirstOrDefault(c => c.Type == type);
        if (config != null) return config;

        config = new PicoSpiConfig(model, type, mosi, miso, sck, cpol, cpha, msbfirst, clock);
        PinConfigs.Add(config);
        return config;
    }

    public override TwiConfig? AssignTwiPins(ConfigViewModel model, string type, int sda, int scl, int clock)
    {
        var pin = TwiIndexByPin[sda];
        if (pin != TwiIndexByPin[scl] || TwiTypeByPin[sda] != TwiPinType.Sda ||
            TwiTypeByPin[scl] != TwiPinType.Scl) return null;

        var config = PinConfigs.OfType<PicoTwiConfig>().FirstOrDefault(c => c.Type == type);
        if (config != null) return config;

        if (PinConfigs.Any(c => c is PicoTwiConfig s && s.Index == pin)) return null;
        config = new PicoTwiConfig(model, type, sda, scl, clock);
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
        return SpiTypesByPin.ToList();
    }

    public override List<KeyValuePair<int, TwiPinType>> TwiPins(string type)
    {
        return TwiTypeByPin.ToList();
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
            if (devicePin.PinMode == DevicePinMode.Analog)
            {
                ret += $"adc_gpio_init({devicePin.Pin});";
            }
            else
            {
                var up = devicePin.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullUp;
                var down = devicePin.PinMode is DevicePinMode.BusKeep or DevicePinMode.PullDown;
                ret += $"gpio_init({devicePin.Pin});";
                ret +=
                    $"gpio_set_dir({devicePin.Pin},{(devicePin.PinMode == DevicePinMode.Output).ToString().ToLower()});";
                ret += $"gpio_set_pulls({devicePin.Pin},{up.ToString().ToLower()},{down.ToString().ToLower()});";
            }

        return ret;
    }

    public override int GetChannel(int pin, bool reconfigurePin)
    {
        var chan = pin - PinA0;
        if (reconfigurePin) chan |= 1 << 7;
        return chan;
    }

    public override string GetPinForMicrocontroller(int pin, bool twi, bool spi)
    {
        var ret = $"GP{pin}";
        if (twi && TwiIndexByPin.ContainsKey(pin))
            ret += $" / TWI{TwiIndexByPin[pin]} {TwiTypeByPin[pin].ToString().ToUpper()}";

        if (spi && SpiIndexByPin.ContainsKey(pin))
            ret += $" / SPI{SpiIndexByPin[pin]} {SpiTypesByPin[pin].ToString().ToUpper()}";

        if (pin >= 26) ret += $" / ADC{pin - 26}";

        return ret;
    }

    public override List<int> GetAllPins(bool isAnalog)
    {
        return isAnalog ? AnalogPins : Enumerable.Range(0, GpioCount).ToList();
    }

    public override void PinsFromPortMask(int port, int mask, byte pins,
        Dictionary<int, bool> digitalRaw)
    {
        for (var i = 0; i < 8; i++)
            if ((mask & (1 << i)) != 0)
                digitalRaw[port * 8 + i] = (pins & (1 << i)) == 0;
    }

    public override int GetAnalogMask(DevicePin devicePin)
    {
        return 0;
    }

    public override Dictionary<int, int> GetPortsForTicking(IEnumerable<DevicePin> digital)
    {
        Dictionary<int, int> ports = new();
        foreach (var devicePin in digital)
        {
            var port = devicePin.Pin / 8;
            var mask = 1 << (devicePin.Pin % 8);
            mask |= ports.GetValueOrDefault(port, 0);
            ports[port] = mask;
        }

        return ports;
    }
}
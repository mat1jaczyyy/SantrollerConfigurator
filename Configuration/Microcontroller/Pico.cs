using System.Collections.Generic;
using System.Linq;
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

    private readonly Dictionary<int, SpiPinType> _spiTypes = new()
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

    private readonly Dictionary<int, int> _spiPins = new()
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

    private readonly Dictionary<int, int> _twiPins = new()
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

    private readonly Dictionary<int, TwiPinType> _twiTypes = new()
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

    private PicoTwiConfig? _twiConfig0;
    private PicoTwiConfig? _twiConfig1;
    private PicoSpiConfig? _spiConfig0;
    private PicoSpiConfig? _spiConfig1;

    public override SpiConfig[] SpiConfigs => new[] {_spiConfig0, _spiConfig1}.Where(s => s != null).Select(s => s!)
        .Cast<SpiConfig>().ToArray();

    public override TwiConfig[] TwiConfigs => new[] {_twiConfig0, _twiConfig1}.Where(s => s != null).Select(s => s!)
        .Cast<TwiConfig>().ToArray();

    public override SpiConfig? AssignSpiPins(string type, int mosi, int miso, int sck, bool cpol, bool cpha,
        bool msbfirst,
        int clock)
    {
        int pin = _spiPins[mosi];
        if (pin != _spiPins[miso] || _spiTypes[mosi] != SpiPinType.MOSI || _spiTypes[miso] != SpiPinType.MISO)
        {
            return null;
        }

        if (pin == 0)
        {
            if (_spiConfig0?.Type == type) return _spiConfig0;
            if (_spiConfig0 != null) return null;
            _spiConfig0 = new PicoSpiConfig(type, 0, mosi, miso, sck, cpol, cpha, msbfirst, clock);
            return _spiConfig0;
        }

        if (_spiConfig1?.Type == type) return _spiConfig1;
        if (_spiConfig1 != null) return null;
        _spiConfig1 = new PicoSpiConfig(type, 1, mosi, miso, sck, cpol, cpha, msbfirst, clock);
        return _spiConfig1;
    }

    public override TwiConfig? AssignTwiPins(string type, int sda, int scl, int clock)
    {
        int pin = _twiPins[sda];
        if (pin != _twiPins[scl] || _twiTypes[sda] != TwiPinType.SDA || _twiTypes[scl] != TwiPinType.SCL)
        {
            return null;
        }

        if (pin == 0)
        {
            if (_twiConfig0?.Type == type) return _twiConfig0;
            if (_twiConfig0 != null) return null;
            _twiConfig0 = new PicoTwiConfig(type, 0, sda, scl, clock);
            return _twiConfig0;
        }

        if (_twiConfig1?.Type == type) return _twiConfig0;
        if (_twiConfig1 != null) return _twiConfig1;
        _twiConfig1 = new PicoTwiConfig(type, 1, sda, scl, clock);
        return _twiConfig1;
    }

    public override bool HasConfigurableSpiPins => true;
    public override bool HasConfigurableTwiPins => true;

    public override bool TwiPinsFree => _twiConfig0 == null || _twiConfig1 == null;
    public override bool SpiPinsFree => _spiConfig0 == null || _spiConfig1 == null;
    public override List<KeyValuePair<int, SpiPinType>> SpiPins(string type)
    {
        var types = _spiTypes.AsEnumerable();
        if (_spiConfig0 != null && _spiConfig0.Type != type)
        {
           types = types.Where(s => s.Key != 0).ToDictionary(s => s.Key, s => s.Value);
        }
        
        if (_spiConfig1 != null && _spiConfig1.Type != type)
        {
            types = types.Where(s => s.Key != 1).ToDictionary(s => s.Key, s => s.Value);
        }

        return types.ToList();
    }

    public override List<KeyValuePair<int, TwiPinType>> TwiPins(string type)
    {
        var types = _twiTypes.AsEnumerable();
        if (_twiConfig0 != null && _twiConfig0.Type != type)
        {
            types = types.Where(s => s.Key != 0).ToDictionary(s => s.Key, s => s.Value);
        }
        
        if (_twiConfig1 != null && _twiConfig1.Type != type)
        {
            types = types.Where(s => s.Key != 1).ToDictionary(s => s.Key, s => s.Value);
        }

        return types.ToList();
    }

    public override void UnAssignSPIPins(string type)
    {
        if (_spiConfig0?.Type == type)
        {
            _spiConfig0 = null;
        }
        else if (_spiConfig1?.Type == type)
        {
            _spiConfig1 = null;
        }
    }

    public override void UnAssignTWIPins(string type)
    {
        if (_twiConfig0?.Type == type)
        {
            _twiConfig0 = null;
        }
        else if (_twiConfig1?.Type == type)
        {
            _twiConfig1 = null;
        }
    }

    public override string GenerateDefinitions()
    {
        List<int> skippedPins = new List<int>();
        if (_twiConfig0 != null)
        {
            skippedPins.AddRange(_twiConfig0.GetPins());
        }

        if (_twiConfig1 != null)
        {
            skippedPins.AddRange(_twiConfig1.GetPins());
        }

        if (_spiConfig0 != null)
        {
            skippedPins.AddRange(_spiConfig0.GetPins());
        }

        if (_spiConfig1 != null)
        {
            skippedPins.AddRange(_spiConfig1.GetPins());
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
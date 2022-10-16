using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public abstract class AvrController : Microcontroller
{
    protected abstract int PinA0 { get; }
    protected abstract int SpiMiso { get; }

    protected abstract int SpiMosi { get; }
    protected abstract int SpiSck { get; }
    protected abstract int SpiCSn { get; }
    protected abstract int I2CSda { get; }
    protected abstract int I2CScl { get; }

    public enum AvrPinMode
    {
        Input,
        InputPulldown,
        Output
    }

    public override string GenerateDigitalRead(int pin, bool pullUp)
    {
        // Invert on pullup
        if (pullUp)
        {
            return $"(PIN{GetPort(pin)} & (1 << {GetIndex(pin)})) == 0";
        }

        return $"PIN{GetPort(pin)} & ({1 << GetIndex(pin)})";
    }

    public override string GenerateDigitalWrite(int pin, bool val)
    {
        if (val)
        {
            return $"PORT{GetPort(pin)} |= {1 << GetIndex(pin)}";
        }

        return $"PORT{GetPort(pin)} &= {~(1 << GetIndex(pin))}";
    }

    public override string GeneratePulseRead(int pin, PulseMode mode, int timeout)
    {
        return $"pulseIn(PIN{GetPort(pin)},{1 << GetIndex(pin)},{mode},{timeout})";
    }

    public abstract int GetIndex(int pin);
    public abstract char GetPort(int pin);

    public abstract AvrPinMode? ForcedMode(int pin);

    public abstract int PinCount { get; }


    private AvrTwiConfig? _twiConfig;
    private AvrSpiConfig? _spiConfig;

    public override SpiConfig? AssignSpiPins(string type, int mosi, int miso, int sck, bool cpol, bool cpha,
        bool msbfirst,
        int clock)
    {
        if (_spiConfig != null) return null;
        _spiConfig = new AvrSpiConfig(type, SpiMosi, SpiMiso, SpiSck, cpol, cpha, msbfirst, clock);
        SpiConfigs.Add(_spiConfig);
        return _spiConfig;
    }

    public override TwiConfig? AssignTwiPins(string type, int sda, int scl, int clock)
    {
        if (_twiConfig != null) return null;
        _twiConfig = new AvrTwiConfig(type, I2CSda, I2CScl, clock);
        TwiConfigs.Add(_twiConfig);
        return _twiConfig;
    }

    public override void UnAssignSpiPins(string type)
    {
        SpiConfigs.Clear();
        _spiConfig = null;
    }

    public override void UnAssignTwiPins(string type)
    {
        TwiConfigs.Clear();
        _twiConfig = null;
    }

    public override List<KeyValuePair<int, SpiPinType>> SpiPins(string type)
    {
        if (_spiConfig != null && _spiConfig.Type != type)
        {
            return new();
        }

        return new List<KeyValuePair<int, SpiPinType>>
        {
            new(SpiCSn, SpiPinType.CSn),
            new(SpiMiso, SpiPinType.Miso),
            new(SpiMosi, SpiPinType.Mosi),
            new(SpiSck, SpiPinType.Sck),
        };
    }

    public override List<KeyValuePair<int, TwiPinType>> TwiPins(string type)
    {
        if (_twiConfig != null && _twiConfig.Type != type)
        {
            return new();
        }

        return new()
        {
            new(I2CScl, TwiPinType.Scl),
            new(I2CSda, TwiPinType.Sda),
        };
    }

    public override string GenerateDefinitions()
    {
        return (_spiConfig?.Generate() ?? "") + (_twiConfig?.Generate() ?? "");
    }

    public override string GenerateInit(List<Output> bindings)
    {
        // DDRx 1 = output, 0 = input
        // PORTx input 1= pullup, 0 = floating
        Dictionary<char, int> ddrByPort = new Dictionary<char, int>();
        Dictionary<char, int> portByPort = new Dictionary<char, int>();
        var pins = bindings.SelectMany(s => s.GetPins(bindings)).Distinct();
        foreach (var pin in pins)
        {
            var port = GetPort(pin.Pin);
            var idx = GetIndex(pin.Pin);
            var currentPort = portByPort.GetValueOrDefault(port, 0);
            var currentDdr = ddrByPort.GetValueOrDefault(port, 0);
            if (pin.PinMode == DevicePinMode.Output)
            {
                currentDdr += 1 << idx;
            }
            else if (pin.PinMode == DevicePinMode.PullUp)
            {
                currentPort += 1 << idx;
            }

            portByPort[port] = currentPort;
            ddrByPort[port] = currentDdr;
        }

        for (var i = 0; i < PinCount; i++)
        {
            var force = ForcedMode(i);
            var port = GetPort(i);
            var idx = GetIndex(i);
            if (ForcedMode(i) is not null)
            {
                var currentPort = portByPort.GetValueOrDefault(port, 0);
                var currentDdr = ddrByPort.GetValueOrDefault(port, 0);
                switch (force)
                {
                    case AvrPinMode.InputPulldown:
                        currentPort |= 1 << idx;
                        break;
                    case AvrPinMode.Output:
                        currentDdr |= 1 << idx;
                        break;
                }

                portByPort[port] = currentPort;
                ddrByPort[port] = currentDdr;
            }
        }

        string ret = "uint8_t oldSREG = SREG;cli();";
        foreach (var port in portByPort)
        {
            ret += $"PORT{port.Key} = {port.Value};";
        }

        foreach (var port in ddrByPort)
        {
            ret += $"DDR{port.Key} = {port.Value};";
        }

        ret += "SREG = oldSREG;";
        return ret;
    }

    public override string GetPin(int pin)
    {
        var ret = $"{pin}";
        if (pin >= PinA0)
        {
            ret += $" / A{pin - PinA0}";
        }

        if (pin == SpiCSn)
        {
            ret += " / SPI CS";
        }

        if (pin == SpiMiso)
        {
            ret += " / SPI MISO";
        }

        if (pin == SpiMosi)
        {
            ret += " / SPI MOSI";
        }

        if (pin == SpiSck)
        {
            ret += " / SPI CLK";
        }

        if (pin == I2CScl)
        {
            ret += " / I2C SCL";
        }

        if (pin == I2CSda)
        {
            ret += " / I2C SDA";
        }

        return ret;
    }

    public override string GenerateAckDefines(int ack)
    {
        return $"INTERRUPT_PS2_ACK {GetInterruptForPin(ack)}";
    }

    protected abstract string GetInterruptForPin(int ack);
}
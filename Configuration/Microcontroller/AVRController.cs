using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

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
        INPUT,
        INPUT_PULLDOWN,
        OUTPUT
    }

    public override string GenerateDigitalRead(int pin, bool pullUp)
    {
        // Invert on pullup
        if (pullUp)
        {
            return $"(PIN{GetPort(pin)} & (1 << {GetIndex(pin)})) == 0";
        }

        return $"PIN{GetPort(pin)} & (1 << {GetIndex(pin)})";
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

    public override void UnAssignSPIPins(string type)
    {
        SpiConfigs.Clear();
        _spiConfig = null;
    }

    public override void UnAssignTWIPins(string type)
    {
        TwiConfigs.Clear();
        _twiConfig = null;
    }
    
    public override bool HasConfigurableSpiPins => false;
    public override bool HasConfigurableTwiPins => false;
    
    public override bool TwiPinsFree => _twiConfig == null;
    public override bool SpiPinsFree => _spiConfig == null;

    public override List<KeyValuePair<int, SpiPinType>> SpiPins(string type)
    {
        if (_spiConfig != null && _spiConfig.Type != type)
        {
            return new();
        }
        return new()
        {
            new (SpiCSn, SpiPinType.CSn),
            new (SpiMiso, SpiPinType.MISO),
            new (SpiMosi, SpiPinType.MOSI),
            new (SpiSck, SpiPinType.SCK),
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
            new (I2CScl, TwiPinType.SCL),
            new (I2CSda, TwiPinType.SDA),
        };
    }

    public override string GenerateDefinitions()
    {
        List<int> skippedPins = new List<int>();
        if (_spiConfig != null)
        {
            skippedPins.Add(SpiCSn);
            skippedPins.Add(SpiMiso);
            skippedPins.Add(SpiMosi);
            skippedPins.Add(SpiSck);
        }

        if (_twiConfig != null)
        {
            skippedPins.Add(I2CScl);
            skippedPins.Add(I2CSda);
        }

        Dictionary<char, int> skippedByPort = new Dictionary<char, int>();
        for (var i = 0; i < PinCount; i++)
        {
            if (ForcedMode(i) is not null)
            {
                skippedPins.Add(i);
            }

            skippedByPort[GetPort(i)] = 0;
        }

        foreach (var pin in skippedPins)
        {
            skippedByPort[GetPort(pin)] |= 1 << GetIndex(pin);
        }

        var ret = "#define SKIP_MASK_AVR {" + string.Join(", ",
                            skippedByPort.Keys.OrderBy(x => x).Select(x => skippedByPort[x].ToString())) +
                        "}";
        if (_spiConfig != null)
        {
            ret += _spiConfig.generate();
        }

        if (_twiConfig != null)
        {
            ret += _twiConfig.Generate();
        }
        return ret;
    }

    public override string GenerateInit(List<Output> bindings)
    {
        // DDRx 1 = output, 0 = input
        // PORTx input 1= pullup, 0 = floating
        // TODO: outputs (Start power led?)
        Dictionary<char, int> ddrByPort = new Dictionary<char, int>();
        Dictionary<char, int> portByPort = new Dictionary<char, int>();
        foreach (var output in bindings)
        {
            if (output.Input?.InnermostInput() is DirectInput direct)
            {
                if (!direct.IsAnalog)
                {
                    var port = GetPort(direct.Pin);
                    var idx = GetIndex(direct.Pin);
                    var currentPort = portByPort.GetValueOrDefault(port, 0);
                    var currentDdr = ddrByPort.GetValueOrDefault(port, 0);
                    if (direct.PinMode == DevicePinMode.PullUp)
                    {
                        currentPort += 1 << idx;
                    }

                    if (currentPort != 0)
                    {
                        portByPort[port] = currentPort;
                    }

                    ddrByPort[port] = currentDdr;
                }
            }
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
                    case AvrPinMode.INPUT_PULLDOWN:
                        currentPort |= 1 << idx;
                        break;
                    case AvrPinMode.OUTPUT:
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
        string ret = $"{pin}";
        if (pin >= PinA0)
        {
            ret += $" / A{pin - PinA0}";
        }

        if (pin == SpiCSn)
        {
            ret += $" / SPI CS";
        }

        if (pin == SpiMiso)
        {
            ret += $" / SPI MISO";
        }

        if (pin == SpiMosi)
        {
            ret += $" / SPI MOSI";
        }

        if (pin == SpiSck)
        {
            ret += $" / SPI CLK";
        }

        if (pin == I2CScl)
        {
            ret += $" / I2C SCL";
        }

        if (pin == I2CSda)
        {
            ret += $" / I2C SDA";
        }

        return ret;
    }
}
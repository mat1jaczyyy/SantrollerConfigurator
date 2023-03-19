using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class AvrController : Microcontroller
{
    public enum AvrPinMode
    {
        Input,
        InputPulldown,
        Output
    }

    public override bool TwiAssignable => false;
    public override bool SpiAssignable => false;

    protected abstract int PinA0 { get; }
    protected abstract int SpiMiso { get; }

    protected abstract int SpiMosi { get; }
    protected abstract int SpiSck { get; }
    protected abstract int SpiCSn { get; }
    protected abstract int I2CSda { get; }
    protected abstract int I2CScl { get; }

    public abstract int PinCount { get; }

    protected abstract char[] PortNames { get; }
    protected abstract Dictionary<Tuple<char, int>, int> PinByMask { get; }

    public override string GenerateDigitalRead(int pin, bool pullUp)
    {
        // Invert on pullup
        if (pullUp) return $"(PIN{GetPort(pin)} & (1 << {GetIndex(pin)})) == 0";

        return $"PIN{GetPort(pin)} & ({1 << GetIndex(pin)})";
    }

    public override string GenerateDigitalWrite(int pin, bool val)
    {
        if (val) return $"PORT{GetPort(pin)} |= {1 << GetIndex(pin)}";

        return $"PORT{GetPort(pin)} &= {~(1 << GetIndex(pin))}";
    }

    public abstract int GetIndex(int pin);
    public abstract char GetPort(int pin);

    public abstract AvrPinMode? ForcedMode(int pin);


    public override SpiConfig? AssignSpiPins(ConfigViewModel model, string type, int mosi, int miso, int sck, bool cpol,
        bool cpha,
        bool msbfirst,
        uint clock)
    {
        var conf = new AvrSpiConfig(model, type, SpiMosi, SpiMiso, SpiSck, SpiCSn, cpol, cpha, msbfirst, clock);
        PinConfigs.Add(conf);
        return conf;
    }

    public override TwiConfig? AssignTwiPins(ConfigViewModel model, string type, int sda, int scl, int clock)
    {
        var conf = new AvrTwiConfig(model, type, I2CSda, I2CScl, clock);
        PinConfigs.Add(conf);
        return conf;
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

    public override List<KeyValuePair<int, SpiPinType>> SpiPins(string type)
    {
        return new List<KeyValuePair<int, SpiPinType>>
        {
            new(SpiCSn, SpiPinType.CSn),
            new(SpiMiso, SpiPinType.Miso),
            new(SpiMosi, SpiPinType.Mosi),
            new(SpiSck, SpiPinType.Sck)
        };
    }

    public override List<KeyValuePair<int, TwiPinType>> TwiPins(string type)
    {
        return new List<KeyValuePair<int, TwiPinType>>
        {
            new(I2CScl, TwiPinType.Scl),
            new(I2CSda, TwiPinType.Sda)
        };
    }
    
    public override List<KeyValuePair<int, SpiPinType>> FreeSpiPins(string type)
    {
        return PinConfigs.Any(s => s is AvrSpiConfig) ? Array.Empty<KeyValuePair<int, SpiPinType>>().ToList() : SpiPins(type);
    }

    public override List<KeyValuePair<int, TwiPinType>> FreeTwiPins(string type)
    {
        return PinConfigs.Any(s => s is AvrTwiConfig) ? Array.Empty<KeyValuePair<int, TwiPinType>>().ToList() : TwiPins(type);
    }

    public override void PinsFromPortMask(int port, int mask, byte pins,
        Dictionary<int, bool> digitalRaw)
    {
        var portChar = PortNames[port];
        for (var i = 0; i < 8; i++)
        {
            if ((mask & (1 << i)) == 0) continue;
            digitalRaw[PinByMask[new Tuple<char, int>(portChar, i)]] = (pins & (1 << i)) == 0;
        }
    }

    public override int GetAnalogMask(DevicePin devicePin)
    {
        return 1 << GetIndex(devicePin.Pin);
    }

    public override Dictionary<int, int> GetPortsForTicking(IEnumerable<DevicePin> digital)
    {
        var maskByPort = new Dictionary<int, int>();
        foreach (var devicePin in digital)
        {
            var port = PortNames.IndexOf(GetPort(devicePin.Pin));
            var mask = 1 << GetIndex(devicePin.Pin);
            maskByPort[port] = mask | maskByPort.GetValueOrDefault(port, 0);
        }

        return maskByPort;
    }

    public override string GenerateInit()
    {
        // DDRx 1 = output, 0 = input
        // PORTx input 1= pullup, 0 = floating
        var ddrByPort = new Dictionary<char, int>();
        var portByPort = new Dictionary<char, int>();
        var pins = PinConfigs.OfType<DirectPinConfig>();
        foreach (var pin in pins)
        {
            var port = GetPort(pin.Pin);
            var idx = GetIndex(pin.Pin);
            var currentPort = portByPort.GetValueOrDefault(port, 0);
            var currentDdr = ddrByPort.GetValueOrDefault(port, 0);
            switch (pin.PinMode)
            {
                case DevicePinMode.Output:
                    currentDdr |= 1 << idx;
                    break;
                case DevicePinMode.PullUp:
                    currentPort |= 1 << idx;
                    break;
            }

            portByPort[port] = currentPort;
            ddrByPort[port] = currentDdr;
        }

        for (var i = 0; i < PinCount; i++)
        {
            var force = ForcedMode(i);
            var port = GetPort(i);
            var idx = GetIndex(i);

            if (ForcedMode(i) is null) continue;
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

        var ret = "uint8_t oldSREG = SREG;cli();";

        foreach (var port in ddrByPort) ret += $"DDR{port.Key} = {port.Value};";
        foreach (var port in portByPort) ret += $"PORT{port.Key} = {port.Value};";

        ret += "SREG = oldSREG;";
        return ret;
    }

    public override string GenerateAnalogRead(int pin)
    {
        return "adc({pin})";
    }

    public override string GetPinForMicrocontroller(int pin, bool spi, bool twi)
    {
        var ret = $"{pin}";
        if (pin >= PinA0) ret += $" / A{pin - PinA0}";

        if (pin == SpiCSn) ret += " / SPI CS";

        if (pin == SpiMiso) ret += " / SPI MISO";

        if (pin == SpiMosi) ret += " / SPI MOSI";

        if (pin == SpiSck) ret += " / SPI CLK";

        if (pin == I2CScl) ret += " / I2C SCL";

        if (pin == I2CSda) ret += " / I2C SDA";

        return ret;
    }

    public override string GenerateAckDefines(int ack)
    {
        return $"INTERRUPT_PS2_ACK {GetInterruptForPin(ack)}";
    }

    protected abstract string GetInterruptForPin(int ack);

    public override int GetFirstAnalogPin()
    {
        return PinA0;
    }
}
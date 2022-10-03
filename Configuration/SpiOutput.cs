using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class SpiOutput : Output
{
    protected readonly Microcontroller.Microcontroller Microcontroller;

    // ReSharper disable ExplicitCallerInfoArgument
    protected SpiOutput(ConfigViewModel model, Microcontroller.Microcontroller microcontroller, string spiType, int spiFreq, bool cpol,
        bool cpha, bool msbFirst, string name): base(model, null, Colors.Transparent, Colors.Transparent, name)
    {
        Microcontroller = microcontroller;
        SpiFreq = spiFreq;
        SpiType = spiType;
        var config = microcontroller.GetSpiForType(SpiType);
        if (config != null)
        {
            _spiConfig = config;
            this.WhenAnyValue(x => x._spiConfig.Miso).Subscribe(s => this.RaisePropertyChanged("Miso"));
            this.WhenAnyValue(x => x._spiConfig.Mosi).Subscribe(s => this.RaisePropertyChanged("Mosi"));
            this.WhenAnyValue(x => x._spiConfig.Sck).Subscribe(s => this.RaisePropertyChanged("Sck"));
            return;
        }

        var pins = microcontroller.SpiPins(SpiType);
        if (!pins.Any())
        {
            throw new PinUnavailableException("I2C already in use!");
        }

        var miso = pins.First(pair => pair.Value is SpiPinType.MISO).Key;
        var mosi = pins.First(pair => pair.Value is SpiPinType.MOSI).Key;
        var sck = pins.First(pair => pair.Value is SpiPinType.SCK).Key;
        _spiConfig = microcontroller.AssignSpiPins(SpiType, mosi, miso, sck, cpol, cpha, msbFirst, spiFreq)!;
        this.WhenAnyValue(x => x._spiConfig.Miso).Subscribe(s => this.RaisePropertyChanged("Miso"));
        this.WhenAnyValue(x => x._spiConfig.Mosi).Subscribe(s => this.RaisePropertyChanged("Mosi"));
        this.WhenAnyValue(x => x._spiConfig.Sck).Subscribe(s => this.RaisePropertyChanged("Sck"));
    }

    public string SpiType { get; }
    public int SpiFreq { get; }

    private readonly SpiConfig _spiConfig;

    public int Mosi
    {
        get => _spiConfig.Mosi;
        set => _spiConfig.Mosi = value;
    }

    public int Miso
    {
        get => _spiConfig.Miso;
        set => _spiConfig.Miso = value;
    }

    public int Sck
    {
        get => _spiConfig.Sck;
        set => _spiConfig.Sck = value;
    }

// ReSharper disable ExplicitCallerInfoArgument

    public List<int> AvailableMosiPins => GetMosiPins();
    public List<int> AvailableMisoPins => GetMisoPins();
    public List<int> AvailableSckPins => GetSckPins();

    private List<int> GetMosiPins()
    {
        return Microcontroller.SpiPins(SpiType)
            .Where(s => s.Value is SpiPinType.MOSI)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetMisoPins()
    {
        return Microcontroller.SpiPins(SpiType)
            .Where(s => s.Value is SpiPinType.MISO)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSckPins()
    {
        return Microcontroller.SpiPins(SpiType)
            .Where(s => s.Value is SpiPinType.SCK)
            .Select(s => s.Key).ToList();
    }
}
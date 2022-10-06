using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class SpiInput : Input
{
    private readonly Microcontroller _microcontroller;

    // ReSharper disable ExplicitCallerInfoArgument
    protected SpiInput(Microcontroller microcontroller, string spiType, int spiFreq, bool cpol,
        bool cpha, bool msbFirst, int? miso = null, int? mosi = null, int? sck = null)
    {
        _microcontroller = microcontroller;
        _spiType = spiType;
        var config = microcontroller.GetSpiForType(_spiType);
        if (config != null)
        {
            _spiConfig = config;
        }
        else
        {
            var pins = microcontroller.SpiPins(_spiType);
            if (!pins.Any())
            {
                throw new PinUnavailableException("No SPI Pins Available!");
            }

            if (miso == null || mosi == null || sck == null)
            {
                miso = pins.First(pair => pair.Value is SpiPinType.MISO).Key;
                mosi = pins.First(pair => pair.Value is SpiPinType.MOSI).Key;
                sck = pins.First(pair => pair.Value is SpiPinType.SCK).Key;
            }

            _spiConfig = microcontroller.AssignSpiPins(_spiType, mosi.Value, miso.Value, sck.Value, cpol, cpha, msbFirst, spiFreq)!;
        }

        this.WhenAnyValue(x => x._spiConfig.Miso).Subscribe(_ => this.RaisePropertyChanged("Miso"));
        this.WhenAnyValue(x => x._spiConfig.Mosi).Subscribe(_ => this.RaisePropertyChanged("Mosi"));
        this.WhenAnyValue(x => x._spiConfig.Sck).Subscribe(_ => this.RaisePropertyChanged("Sck"));
        microcontroller.TwiConfigs.CollectionChanged +=
            (_, _) =>
            {
                var mosi2 = Mosi;
                var miso2 = Miso;
                var sck2 = Sck;
                this.RaisePropertyChanged("AvailableMosiPins");
                this.RaisePropertyChanged("AvailableMisoPins");
                this.RaisePropertyChanged("AvailableSckPins");
                Mosi = mosi2;
                Miso = miso2;
                Sck = sck2;
            };
    }

    private string _spiType;

    private readonly SpiConfig _spiConfig;
    
    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {$"{_spiType.ToUpper()}_SPI_PORT {_spiConfig.Definition}"};
    }
    
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

    public List<int> AvailableMosiPins => GetMosiPins();
    public List<int> AvailableMisoPins => GetMisoPins();
    public List<int> AvailableSckPins => GetSckPins();

    private List<int> GetMosiPins()
    {
        return _microcontroller.SpiPins(_spiType)
            .Where(s => s.Value is SpiPinType.MOSI)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetMisoPins()
    {
        return _microcontroller.SpiPins(_spiType)
            .Where(s => s.Value is SpiPinType.MISO)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSckPins()
    {
        return _microcontroller.SpiPins(_spiType)
            .Where(s => s.Value is SpiPinType.SCK)
            .Select(s => s.Key).ToList();
    }
}
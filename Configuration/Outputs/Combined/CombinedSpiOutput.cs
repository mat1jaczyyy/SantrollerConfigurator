using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedSpiOutput : CombinedOutput, ISpi
{
    protected readonly SpiConfig _spiConfig;

    protected CombinedSpiOutput(ConfigViewModel model, string spiType, uint spiFreq,
        bool cpol,
        bool cpha, bool msbFirst, string name, int? miso = null, int? mosi = null, int? sck = null) : base(model, null,
        name)
    {
        SpiType = spiType;
        BindableSpi = Model.Microcontroller.SpiAssignable;
        var config = Model.Microcontroller.GetSpiForType(SpiType);
        if (config != null)
        {
            _spiConfig = config;
        }
        else
        {
            if (miso == null || mosi == null || sck == null)
            {
                var pins = Model.Microcontroller.SpiPins(SpiType);
                miso = pins.First(pair => pair.Value is SpiPinType.Miso).Key;
                mosi = pins.First(pair => pair.Value is SpiPinType.Mosi).Key;
                sck = pins.First(pair => pair.Value is SpiPinType.Sck).Key;
            }

            _spiConfig = Model.Microcontroller.AssignSpiPins(model, SpiType, mosi.Value, miso.Value, sck.Value, cpol, cpha,
                msbFirst, spiFreq)!;
        }

        this.WhenAnyValue(x => x._spiConfig.Miso).Subscribe(_ => this.RaisePropertyChanged(nameof(Miso)));
        this.WhenAnyValue(x => x._spiConfig.Mosi).Subscribe(_ => this.RaisePropertyChanged(nameof(Mosi)));
        this.WhenAnyValue(x => x._spiConfig.Sck).Subscribe(_ => this.RaisePropertyChanged(nameof(Sck)));
    }

    public bool BindableSpi { get; }

    private string SpiType { get; }

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

    public List<int> SpiPins()
    {
        return new List<int> {Mosi, Miso, Sck};
    }

    private List<int> GetMosiPins()
    {
        return Model.Microcontroller.SpiPins(SpiType)
            .Where(s => s.Value is SpiPinType.Mosi)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetMisoPins()
    {
        return Model.Microcontroller.SpiPins(SpiType)
            .Where(s => s.Value is SpiPinType.Miso)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSckPins()
    {
        return Model.Microcontroller.SpiPins(SpiType)
            .Where(s => s.Value is SpiPinType.Sck)
            .Select(s => s.Key).ToList();
    }

    public override void Dispose()
    {
        Model.Microcontroller.UnAssignPins(SpiType);
        base.Dispose();
    }
}
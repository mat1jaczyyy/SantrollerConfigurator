using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration;

public abstract class TwiInput : Input, ITwi
{
    private readonly Microcontroller _microcontroller;

    protected TwiInput(Microcontroller microcontroller, string twiType, int twiFreq, int? sda, int? scl,
        ConfigViewModel model) : base(model)
    {
        _microcontroller = microcontroller;
        _twiType = twiType;
        var config = microcontroller.GetTwiForType(_twiType);
        if (config != null)
        {
            _twiConfig = config;
        }
        else
        {
            if (sda == null || scl == null)
            {
                var pins = microcontroller.TwiPins(_twiType);
                scl = pins.First(pair => pair.Value is TwiPinType.Scl).Key;
                sda = pins.First(pair => pair.Value is TwiPinType.Sda).Key;
            }

            _twiConfig = microcontroller.AssignTwiPins(model, _twiType, sda.Value, scl.Value, twiFreq)!;
        }

       
        this.WhenAnyValue(x => x._twiConfig.Scl).Subscribe(_ => this.RaisePropertyChanged(nameof(Scl)));
        this.WhenAnyValue(x => x._twiConfig.Sda).Subscribe(_ => this.RaisePropertyChanged(nameof(Sda)));
    }

    private readonly string _twiType;

    private readonly TwiConfig _twiConfig;

    public int Sda
    {
        get => _twiConfig.Sda;
        set => _twiConfig.Sda = value;
    }

    public int Scl
    {
        get => _twiConfig.Scl;
        set => _twiConfig.Scl = value;
    }


    public List<int> AvailableSdaPins => GetSdaPins();
    public List<int> AvailableSclPins => GetSclPins();

    private List<int> GetSdaPins()
    {
        return _microcontroller.TwiPins(_twiType)
            .Where(s => s.Value is TwiPinType.Sda)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins()
    {
        return _microcontroller.TwiPins(_twiType)
            .Where(s => s.Value is TwiPinType.Scl)
            .Select(s => s.Key).ToList();
    }
    
    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {$"{_twiType.ToUpper()}_TWI_PORT {_twiConfig.Definition}"};
    }

    public override void Dispose()
    {
        _microcontroller.UnAssignPins(_twiType);
    }
    public override IList<PinConfig> PinConfigs => new List<PinConfig>() {_twiConfig};
    public List<int> TwiPins() => new() {Sda, Scl};
}
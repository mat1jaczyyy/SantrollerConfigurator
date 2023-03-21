using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public abstract class TwiInput : Input, ITwi
{

    private readonly TwiConfig _twiConfig;

    private readonly string _twiType;

    protected TwiInput(string twiType, int twiFreq, int? sda, int? scl,
        ConfigViewModel model) : base(model)
    {
        _twiType = twiType;
        var config = Model.Microcontroller.GetTwiForType(_twiType);
        if (config != null)
        {
            _twiConfig = config;
        }
        else
        {
            if (sda == null || scl == null)
            {
                var pins = Model.Microcontroller.FreeTwiPins(_twiType);
                scl = pins.First(pair => pair.Value is TwiPinType.Scl).Key;
                sda = pins.First(pair => pair.Value is TwiPinType.Sda).Key;
            }

            _twiConfig = Model.Microcontroller.AssignTwiPins(model, _twiType, sda.Value, scl.Value, twiFreq)!;
        }


        this.WhenAnyValue(x => x._twiConfig.Scl).Subscribe(_ => this.RaisePropertyChanged(nameof(Scl)));
        this.WhenAnyValue(x => x._twiConfig.Sda).Subscribe(_ => this.RaisePropertyChanged(nameof(Sda)));
    }

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
    public override IList<PinConfig> PinConfigs => new List<PinConfig> {_twiConfig};

    public List<int> TwiPins()
    {
        return new List<int> {Sda, Scl};
    }

    private List<int> GetSdaPins()
    {
        return Model.Microcontroller.TwiPins(_twiType)
            .Where(s => s.Value is TwiPinType.Sda)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins()
    {
        return Model.Microcontroller.TwiPins(_twiType)
            .Where(s => s.Value is TwiPinType.Scl)
            .Select(s => s.Key).ToList();
    }

    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {$"{_twiType.ToUpper()}_TWI_PORT {_twiConfig.Definition}"};
    }

    public override void Dispose()
    {
        Model.Microcontroller.UnAssignPins(_twiType);
    }
}
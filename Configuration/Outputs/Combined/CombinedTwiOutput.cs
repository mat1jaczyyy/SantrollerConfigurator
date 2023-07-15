using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedTwiOutput : CombinedOutput, ITwi
{
    private readonly TwiConfig _twiConfig;

    private readonly string _twiType;


    protected CombinedTwiOutput(ConfigViewModel model, string twiType,
        int twiFreq, string name, int sda = -1, int scl = -1) : base(model)

    {
        BindableTwi = Model.Microcontroller.TwiAssignable;
        _twiType = twiType;
        var config = Model.GetTwiForType(_twiType);
        _twiConfig = config ?? Model.Microcontroller.AssignTwiPins(model, _twiType, sda, scl, twiFreq);


        this.WhenAnyValue(x => x._twiConfig.Scl).Subscribe(_ => this.RaisePropertyChanged(nameof(Scl)));
        this.WhenAnyValue(x => x._twiConfig.Sda).Subscribe(_ => this.RaisePropertyChanged(nameof(Sda)));
    }

    public bool BindableTwi { get; }

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

    protected override IEnumerable<PinConfig> GetOwnPinConfigs()
    {
        return new[] {_twiConfig};
    }
}
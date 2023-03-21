using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedTwiOutput : CombinedOutput, ITwi
{

    private readonly TwiConfig _twiConfig;

    private readonly string _twiType;


    protected CombinedTwiOutput(ConfigViewModel model, string twiType,
        int twiFreq, string name, int? sda = null, int? scl = null) : base(model, new FixedInput(model, 0))

    {
        BindableTwi = Model.Microcontroller.TwiAssignable;
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

    public override void Dispose()
    {
        Model.Microcontroller.UnAssignPins(_twiType);
        base.Dispose();
    }
}
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

public abstract class TwiOutput : Output
{
    protected Microcontroller.Microcontroller Microcontroller;

    // ReSharper disable ExplicitCallerInfoArgument
    protected TwiOutput(ConfigViewModel model, Microcontroller.Microcontroller microcontroller, string twiType,
        int twiFreq, string name) : base(model, null, Colors.Transparent, Colors.Transparent, name)
    {
        Microcontroller = microcontroller;
        TwiFreq = twiFreq;
        TwiType = twiType;
        var config = microcontroller.GetTwiForType(TwiType);
        if (config != null)
        {
            _twiConfig = config;
            this.WhenAnyValue(x => x._twiConfig.Scl).Subscribe(s => this.RaisePropertyChanged("Scl"));
            this.WhenAnyValue(x => x._twiConfig.Sda).Subscribe(s => this.RaisePropertyChanged("Sda"));
            return;
        }

        var pins = microcontroller.TwiPins(TwiType);
        if (!pins.Any())
        {
            throw new PinUnavailableException("I2C already in use!");
        }

        var scl = pins.First(pair => pair.Value is TwiPinType.SCL).Key;
        var sda = pins.First(pair => pair.Value is TwiPinType.SDA).Key;
        _twiConfig = microcontroller.AssignTwiPins(TwiType, sda, scl, TwiFreq)!;
        this.WhenAnyValue(x => x._twiConfig.Scl).Subscribe(s => this.RaisePropertyChanged("Scl"));
        this.WhenAnyValue(x => x._twiConfig.Sda).Subscribe(s => this.RaisePropertyChanged("Sda"));
    }

    public string TwiType;

    public int TwiFreq;

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
        return Microcontroller.TwiPins(TwiType)
            .Where(s => s.Value is TwiPinType.SDA)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins()
    {
        return Microcontroller.TwiPins(TwiType)
            .Where(s => s.Value is TwiPinType.SCL)
            .Select(s => s.Key).ToList();
    }
}
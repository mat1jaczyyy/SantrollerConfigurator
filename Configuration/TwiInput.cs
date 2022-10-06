using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class TwiInput : Input
{
    private readonly Microcontroller _microcontroller;

    // ReSharper disable ExplicitCallerInfoArgument
    protected TwiInput(Microcontroller microcontroller, string twiType, int twiFreq, int? sda, int? scl)
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
                if (!pins.Any())
                {
                    throw new PinUnavailableException("No I2C Pins Available!");
                }

                scl = pins.First(pair => pair.Value is TwiPinType.SCL).Key;
                sda = pins.First(pair => pair.Value is TwiPinType.SDA).Key;
            }

            _twiConfig = microcontroller.AssignTwiPins(_twiType, sda.Value, scl.Value, twiFreq)!;
        }

       
        this.WhenAnyValue(x => x._twiConfig.Scl).Subscribe(_ => this.RaisePropertyChanged("Scl"));
        this.WhenAnyValue(x => x._twiConfig.Sda).Subscribe(_ => this.RaisePropertyChanged("Sda"));
        microcontroller.TwiConfigs.CollectionChanged +=
            (sender, args) =>
            {
                var sda2 = Sda;
                var scl2 = Scl;
                this.RaisePropertyChanged("AvailableSdaPins");
                this.RaisePropertyChanged("AvailableSclPins");
                Sda = sda2;
                Scl = scl2;
            };
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
            .Where(s => s.Value is TwiPinType.SDA)
            .Select(s => s.Key).ToList();
    }

    private List<int> GetSclPins()
    {
        return _microcontroller.TwiPins(_twiType)
            .Where(s => s.Value is TwiPinType.SCL)
            .Select(s => s.Key).ToList();
    }
    
    public override IReadOnlyList<string> RequiredDefines()
    {
        return new[] {$"{_twiType.ToUpper()}_SPI_PORT {_twiConfig.Definition}"};
    }
}
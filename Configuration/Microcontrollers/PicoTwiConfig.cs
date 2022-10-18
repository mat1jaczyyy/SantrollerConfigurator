using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class PicoTwiConfig : TwiConfig
{
    public PicoTwiConfig(string type, int sda, int scl, int clock) : base(type, sda, scl, clock)
    {
    }

    public int Index => Pico.TwiIndexByPin[_sda];
    public override string Definition => $"TWI_{Index}";

    protected override void UpdatePins([CallerMemberName] string? propertyName = null)
    {
        var indexSda = Pico.TwiIndexByPin[_sda];
        var indexScl = Pico.TwiIndexByPin[_scl];
        if (indexSda == indexScl) return;
        switch (propertyName)
        {
            case nameof(Sda):
                this.RaiseAndSetIfChanged(ref _scl,
                    Pico.TwiIndexByPin.Where(x => Pico.TwiTypeByPin[x.Key] == TwiPinType.Scl)
                        .MinBy(x => x.Key - _sda).Key, nameof(Scl));
                break;
            case nameof(Scl):
                this.RaiseAndSetIfChanged(ref _sda,
                    Pico.TwiIndexByPin.Where(x => Pico.TwiTypeByPin[x.Key] == TwiPinType.Sda)
                        .MinBy(x => x.Key - _scl).Key, nameof(Sda));
                break;
        }
    }
}
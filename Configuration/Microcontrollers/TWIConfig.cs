using System.Collections.Generic;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class TwiConfig : PinConfig
{
    protected int _sda;
    protected int _scl;
    protected int _clock;
    protected string _type;

    protected TwiConfig(ConfigViewModel model, string type, int sda, int scl, int clock): base(model)
    {
        _type = type;
        _sda = sda;
        _scl = scl;
        _clock = clock;
    }

    public override string Generate()
    {
        return $@"
#define {Definition}_SDA {_sda}
#define {Definition}_SCL {_scl}
#define {Definition}_CLOCK {_clock}
";
    }

    public override string Type => _type;

    public int Sda
    {
        get => _sda;
        set => this.RaiseAndSetIfChanged(ref _sda, value);
    }

    public int Scl
    {
        get => _scl;
        set => this.RaiseAndSetIfChanged(ref _scl, value);
    }

    public override IEnumerable<int> Pins => new List<int> {_sda, _scl};
}
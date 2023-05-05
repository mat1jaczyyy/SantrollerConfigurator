using System.Collections.Generic;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class TwiConfig : PinConfig
{
    private int _clock;
    private int _scl;
    private int _sda;
    private string _type;

    protected TwiConfig(ConfigViewModel model, string type, int sda, int scl, int clock) : base(model)
    {
        _type = type;
        _sda = sda;
        _scl = scl;
        _clock = clock;
    }
    protected abstract bool Reassignable { get; }

    public override string Type => _type;

    public int Sda
    {
        get => _sda;
        set
        {
            if (!Reassignable) return;
            this.RaiseAndSetIfChanged(ref _sda, value);
            Update();
        }
    }

    public int Scl
    {
        get => _scl;
        set
        {
            if (!Reassignable) return;
            this.RaiseAndSetIfChanged(ref _scl, value);
            Update();
        }
    }

    public override IEnumerable<int> Pins => new List<int> {_sda, _scl};

    public override string Generate()
    {
        return $@"
#define {Definition}_SDA {_sda}
#define {Definition}_SCL {_scl}
#define {Definition}_CLOCK {_clock}
";
    }
}
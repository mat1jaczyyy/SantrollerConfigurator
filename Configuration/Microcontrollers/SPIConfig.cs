using System.Collections.Generic;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class SpiConfig : PinConfig
{
    private readonly uint _clock;
    private readonly bool _cpha;
    private readonly bool _cpol;
    private readonly bool _msbfirst;

    protected int _miso;

    protected int _mosi;

    protected int _sck;

    protected SpiConfig(ConfigViewModel model, string type, int mosi, int miso, int sck, bool cpol, bool cpha,
        bool msbfirst, uint clock) : base(model)
    {
        Type = type;
        _mosi = mosi;
        _miso = miso;
        _sck = sck;
        _cpol = cpol;
        _cpha = cpha;
        _msbfirst = msbfirst;
        _clock = clock;
    }

    public override string Type { get; }
    protected abstract bool Reassignable { get; }

    public int Mosi
    {
        get => _mosi;
        set
        {
            if (!Reassignable) return;
            this.RaiseAndSetIfChanged(ref _mosi, value);
            Update();
        }
    }

    public int Miso
    {
        get => _miso;
        set
        {
            if (!Reassignable) return;
            this.RaiseAndSetIfChanged(ref _miso, value);
            Update();
        }
    }

    public int Sck
    {
        get => _sck;
        set
        {
            if (!Reassignable) return;
            this.RaiseAndSetIfChanged(ref _sck, value);
            Update();
        }
    }

    public override IEnumerable<int> Pins => new List<int> {_mosi, _miso, _sck};

    public override string Generate()
    {
        return $@"
#define {Definition}_MISO {_miso}
#define {Definition}_MOSI {_mosi}
#define {Definition}_SCK {_sck}
#define {Definition}_CPOL {(_cpol ? 1 : 0)}
#define {Definition}_CPHA {(_cpha ? 1 : 0)}
#define {Definition}_MSBFIRST {(_msbfirst ? 1 : 0)}
#define {Definition}_CLOCK {_clock}
";
    }
}
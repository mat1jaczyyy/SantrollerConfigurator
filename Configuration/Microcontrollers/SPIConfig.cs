using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public abstract class SpiConfig : PinConfig
{
    private bool _cpol;
    private bool _cpha;
    private bool _msbfirst;
    private int _clock;

    protected int _mosi;

    protected int _miso;

    protected int _sck;

    protected SpiConfig(string type, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, int clock)
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

    public override string Generate()
    {
        return $@"
#define {Definition}_MOSI {_mosi}
#define {Definition}_MISO {_miso}
#define {Definition}_SCK {_sck}
#define {Definition}_CPOL {(_cpol ? 1 : 0)}
#define {Definition}_CPHA {(_cpha ? 1 : 0)}
#define {Definition}_MSBFIRST {(_msbfirst ? 1 : 0)}
#define {Definition}_CLOCK {_clock}
";
    }

    public override string Type { get; }
    protected abstract void UpdatePins([CallerMemberName]string? propertyName = null);

    public int Mosi
    {
        get => _mosi;
        set
        {
            this.RaiseAndSetIfChanged(ref _mosi, value);
            UpdatePins();
        }
    }

    public int Miso
    {
        get => _miso;
        set
        {
            this.RaiseAndSetIfChanged(ref _miso, value);
            UpdatePins();
        }
    }

    public int Sck
    {
        get => _sck;
        set
        {
            this.RaiseAndSetIfChanged(ref _sck, value);
            UpdatePins();
        }
    }
    public override IEnumerable<int> Pins => new List<int> {_miso, _miso, _sck};
}
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public abstract class SpiConfig : ReactiveObject
{
    private bool _cpol;
    private bool _cpha;
    private bool _msbfirst;
    private int _clock;
    private string _type;

    protected int _mosi;

    protected int _miso;

    protected int _sck;

    protected SpiConfig(string type, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, int clock)
    {
        _type = type;
        _mosi = mosi;
        _miso = miso;
        _sck = sck;
        _cpol = cpol;
        _cpha = cpha;
        _msbfirst = msbfirst;
        _clock = clock;
    }

    public string Generate()
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

    public int[] GetPins()
    {
        return new[] {_mosi, _miso, _sck};
    }

    public string Type => _type;
    
    public abstract string Definition { get; }
    protected abstract void UpdatePins(string field);

    public int Mosi
    {
        get => _mosi;
        set
        {
            this.RaiseAndSetIfChanged(ref _mosi, value);
            UpdatePins("mosi");
        }
    }

    public int Miso
    {
        get => _miso;
        set
        {
            this.RaiseAndSetIfChanged(ref _miso, value);
            UpdatePins("miso");
        }
    }

    public int Sck
    {
        get => _sck;
        set
        {
            this.RaiseAndSetIfChanged(ref _sck, value);
            UpdatePins("sck");
        }
    }
}
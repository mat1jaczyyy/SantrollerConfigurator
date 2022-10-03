using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public abstract class TwiConfig: ReactiveObject
{
    protected int _sda;
    protected int _scl;
    protected int _clock;
    protected string _definition;
    protected string _type;

    protected TwiConfig(string type, int sda, int scl, int clock, string definition)
    {
        _type = type;
        _sda = sda;
        _scl = scl;
        _clock = clock;
        _definition = definition;
    }

    public string Generate()
    {
        return $@"
    #define {_definition}_SDA {_sda}
    #define {_definition}_SCL {_scl}
    #define {_definition}_CLOCK {_clock}
";
    }

    public int[] GetPins()
    {
        return new[] {_sda, _scl};
    }

    public string Type => _type;

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

    public int Clock => _clock;
}
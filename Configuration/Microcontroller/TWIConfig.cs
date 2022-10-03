using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public abstract class TwiConfig: ReactiveObject
{
    protected int _sda;
    protected int _scl;
    protected int _clock;
    protected string _type;

    protected TwiConfig(string type, int sda, int scl, int clock)
    {
        _type = type;
        _sda = sda;
        _scl = scl;
        _clock = clock;
    }

    public string Generate()
    {
        return $@"
    #define {Definition}_SDA {_sda}
    #define {Definition}_SCL {_scl}
    #define {Definition}_CLOCK {_clock}
";
    }

    public int[] GetPins()
    {
        return new[] {_sda, _scl};
    }

    public string Type => _type;
    public abstract string Definition { get; }

    //TODO: in these setters, update other pins if they don't match up
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
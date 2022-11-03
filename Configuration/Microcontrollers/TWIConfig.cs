using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public abstract class TwiConfig : PinConfig
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

    public virtual string GenerateInit()
    {
        return "";
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
    protected abstract void UpdatePins([CallerMemberName] string? propertyName = null);

    public int Sda
    {
        get => _sda;
        set
        {
            this.RaiseAndSetIfChanged(ref _sda, value);
            UpdatePins();
        }
    }

    public int Scl
    {
        get => _scl;
        set
        {
            this.RaiseAndSetIfChanged(ref _scl, value);
            UpdatePins();
        }
    }

    public override IEnumerable<int> Pins => new List<int> {_sda, _scl};
}
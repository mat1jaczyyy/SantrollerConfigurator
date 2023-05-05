using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class PicoTwiConfig : TwiConfig
{
    public PicoTwiConfig(ConfigViewModel model, string type, int sda, int scl, int clock) : base(model, type, sda, scl,
        clock)
    {
    }

    public int Index => Pico.TwiIndexByPin[Sda];
    public override string Definition => $"TWI_{Index}";
    protected override bool Reassignable => true;
}
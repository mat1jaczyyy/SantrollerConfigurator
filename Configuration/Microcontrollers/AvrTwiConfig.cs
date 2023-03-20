using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class AvrTwiConfig : TwiConfig
{
    public AvrTwiConfig(ConfigViewModel model, string type, int sda, int scl, int clock) : base(model, type, sda, scl,
        clock)
    {
    }

    public override string Definition => "GC_TWI";
    protected override bool Reassignable => false;
}
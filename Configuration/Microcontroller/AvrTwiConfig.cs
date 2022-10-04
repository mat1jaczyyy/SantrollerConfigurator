namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class AvrTwiConfig : TwiConfig
{
    public AvrTwiConfig(string type, int sda, int scl, int clock) : base(type, sda, scl, clock)
    {
    }

    public override string Definition => "TWI";
    protected override void UpdatePins(string field)
    {
    }
}
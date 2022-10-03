namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class AvrTwiConfig : TwiConfig
{
    public AvrTwiConfig(string type, int sda, int scl, int clock) : base(type, sda, scl, clock, "TWI_")
    {
    }

    public virtual TwiConfig SetSda(int value)
    {
        return this;
    }

    public virtual TwiConfig SetScl(int value)
    {
        return this;
    }
}
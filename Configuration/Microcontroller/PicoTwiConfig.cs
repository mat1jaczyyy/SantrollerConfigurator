namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class PicoTwiConfig : TwiConfig
{
    public PicoTwiConfig(string type, int sda, int scl, int clock) : base(type, sda, scl, clock
    )
    {
    }

    public int Index => Pico.TwiIndexByPin[_sda];
    public override string Definition => $"TWI_{Index}_";
}
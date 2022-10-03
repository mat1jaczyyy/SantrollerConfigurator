namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class PicoTwiConfig : TwiConfig
{
    private int _index;
    public PicoTwiConfig(string type, int index, int sda, int scl, int clock) : base(type, sda, scl, clock,
        $"TWI_{index}_")
    {
        _index = index;
    }
}
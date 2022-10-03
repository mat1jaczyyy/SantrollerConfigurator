namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class PicoSpiConfig: SpiConfig
{
    private int _index;
    public PicoSpiConfig(string type, int index, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, int clock) : base(type, mosi, miso, sck, cpol, cpha, msbfirst, clock, $"SPI_{index}_")
    {
        _index = index;
    }
}
namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class PicoSpiConfig: SpiConfig
{
    public PicoSpiConfig(string type, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, int clock) : base(type, mosi, miso, sck, cpol, cpha, msbfirst, clock)
    {
    }

    public int Index => Pico.SpiIndexByPin[Mosi];
    public override string Definition => $"SPI_{Index}_";
}
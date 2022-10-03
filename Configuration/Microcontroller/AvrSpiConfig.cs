namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class AvrSpiConfig: SpiConfig
{
    public AvrSpiConfig(string type, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, int clock) : base(type, mosi, miso, sck, cpol, cpha, msbfirst, clock, "SPI_")
    {
    }

    public virtual SpiConfig SetMosi(int mosi)
    {
        return this;
    }

    public virtual SpiConfig SetMiso(int miso)
    {
        return this;
    }

    public virtual SpiConfig SetSck(int sck)
    {
        return this;
    }
}
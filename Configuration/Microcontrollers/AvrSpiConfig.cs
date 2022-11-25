using System.Runtime.CompilerServices;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class AvrSpiConfig: SpiConfig
{
    public AvrSpiConfig(string type, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, uint clock) : base(type, mosi, miso, sck, cpol, cpha, msbfirst, clock)
    {
    }

    public override string Definition => "GC_SPI";
}
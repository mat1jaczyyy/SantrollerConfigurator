using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class PicoSpiConfig : SpiConfig
{
    public PicoSpiConfig(ConfigViewModel model, string type, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, uint clock) :
        base(model, type, mosi, miso, sck, cpol, cpha, msbfirst, clock)
    {
    }

    public int Index => Pico.SpiIndexByPin[Mosi];
    protected override bool Reassignable => true;
    public override string Definition => $"SPI_{Index}";
}
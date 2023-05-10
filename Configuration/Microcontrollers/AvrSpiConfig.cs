using System.Collections.Generic;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class AvrSpiConfig : SpiConfig
{
    private readonly int _ss;

    public AvrSpiConfig(ConfigViewModel model, string type, bool includesMiso, int mosi, int miso, int sck, int ss, bool cpol, bool cpha,
        bool msbfirst, uint clock) : base(model, type, includesMiso, mosi, miso, sck, cpol, cpha, msbfirst, clock)
    {
        _ss = ss;
    }

    public override string Definition => "GC_SPI";
    protected override bool Reassignable => false;
    public override IEnumerable<int> Pins => base.Pins.Concat(new List<int> {_ss}).ToList();
}

using System.Linq;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class PicoSpiConfig : SpiConfig
{
    public PicoSpiConfig(ConfigViewModel model, string type, bool includesMiso, int mosi, int miso, int sck, bool cpol, bool cpha,
        bool msbfirst, uint clock) :
        base(model, type, includesMiso, mosi, miso, sck, cpol, cpha, msbfirst, clock)
    {
    }

    public int Index => Mosi == -1 ? 0 : Pico.SpiIndexByPin[Mosi];
    protected override bool Reassignable => true;
    public override string Definition => $"SPI_{Index}";

    protected override string? CalculateError()
    {
        var ret = base.CalculateError();
        if (ret != null) return ret;
        if (IncludesMiso && Pico.SpiIndexByPin[Mosi] != Pico.SpiIndexByPin[Miso])
        {
            return Resources.DifferentSPIGroup;
        }
        if (Pico.SpiIndexByPin[Mosi] != Pico.SpiIndexByPin[Sck])
        {
            return Resources.DifferentSPIGroup;
        }
        var ret2 = Model.Bindings.Items
            .Where(output => output.GetPinConfigs().OfType<PicoSpiConfig>().Any(s => s != this && s.Index == Index))
            .Select(output => string.Format(Resources.SPIGroup, output.LocalisedName, Index)).ToList();
        return ret2.Any() ? string.Join(", ", ret2) : null;
    }
}
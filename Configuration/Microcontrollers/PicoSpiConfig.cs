using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class PicoSpiConfig : SpiConfig
{
    public PicoSpiConfig(string type, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, int clock) :
        base(type, mosi, miso, sck, cpol, cpha, msbfirst, clock)
    {
    }

    public int Index => Pico.SpiIndexByPin[Mosi];
    public override string Definition => $"SPI_{Index}";

    protected override void UpdatePins([CallerMemberName] string? propertyName = null)
    {
        var indexMiso = Pico.SpiIndexByPin[_miso];
        var indexMosi = Pico.SpiIndexByPin[_mosi];
        var indexSck = Pico.SpiIndexByPin[_sck];
        switch (propertyName)
        {
            case nameof(Miso):
                if (indexMiso != indexMosi)
                {
                    this.RaiseAndSetIfChanged(ref _mosi,
                        Pico.SpiIndexByPin.Where(x => Pico.SpiTypesByPin[x.Key] == SpiPinType.Mosi)
                            .MinBy(x => Math.Abs(x.Key - _miso)).Key, nameof(Mosi));
                }

                if (indexMiso != indexSck)
                {
                    this.RaiseAndSetIfChanged(ref _sck,
                        Pico.SpiIndexByPin.Where(x => Pico.SpiTypesByPin[x.Key] == SpiPinType.Sck)
                            .MinBy(x => x.Key - _miso).Key, nameof(Sck));
                }

                break;
            case nameof(Mosi):
                if (indexMosi != indexMiso)
                {
                    this.RaiseAndSetIfChanged(ref _miso,
                        Pico.SpiIndexByPin.Where(x => Pico.SpiTypesByPin[x.Key] == SpiPinType.Miso)
                            .MinBy(x => x.Key - _mosi).Key, nameof(Miso));
                }

                if (indexMosi != indexSck)
                {
                    this.RaiseAndSetIfChanged(ref _sck,
                        Pico.SpiIndexByPin.Where(x => Pico.SpiTypesByPin[x.Key] == SpiPinType.Sck)
                            .MinBy(x => x.Key - _mosi).Key, nameof(Sck));
                }

                break;
            case nameof(Sck):
                if (indexSck != indexMosi)
                {
                    this.RaiseAndSetIfChanged(ref _mosi,
                        Pico.SpiIndexByPin.Where(x => Pico.SpiTypesByPin[x.Key] == SpiPinType.Mosi)
                            .MinBy(x => x.Key - _sck).Key, nameof(Mosi));
                }

                if (indexSck != indexMiso)
                {
                    this.RaiseAndSetIfChanged(ref _miso,
                        Pico.SpiIndexByPin.Where(x => Pico.SpiTypesByPin[x.Key] == SpiPinType.Miso)
                            .MinBy(x => x.Key - _sck).Key, nameof(Miso));
                }

                break;
        }
    }
}
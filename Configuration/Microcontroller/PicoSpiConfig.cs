using System;
using System.Linq;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class PicoSpiConfig: SpiConfig
{
    public PicoSpiConfig(string type, int mosi, int miso, int sck, bool cpol, bool cpha, bool msbfirst, int clock) : base(type, mosi, miso, sck, cpol, cpha, msbfirst, clock)
    {
    }

    public int Index => Pico.SpiIndexByPin[Mosi];
    public override string Definition => $"SPI_{Index}_";
    // ReSharper disable ExplicitCallerInfoArgument
    protected override void UpdatePins(string field)
    {
        var indexMiso = Pico.SpiIndexByPin[_miso];
        var indexMosi = Pico.SpiIndexByPin[_mosi];
        var indexSck = Pico.SpiIndexByPin[_sck];
        if (indexMiso != indexMosi || indexMiso != indexSck || indexMosi != indexSck)
        {
            switch (field)
            {
                case "miso":
                    this.RaiseAndSetIfChanged(ref _mosi,
                        Pico.SpiIndexByPin.OrderBy(x => Math.Abs(x.Key - _miso)).First(x => x.Value == indexMiso && x.Key != _miso).Key, "Mosi");
                    this.RaiseAndSetIfChanged(ref _sck,
                        Pico.SpiIndexByPin.OrderBy(x => Math.Abs(x.Key - _miso)).First(x => x.Value == indexMiso && x.Key != _miso && x.Key != _mosi).Key, "Sck");
                    break;
                case "mosi":
                    this.RaiseAndSetIfChanged(ref _miso,
                        Pico.SpiIndexByPin.OrderBy(x => Math.Abs(x.Key - _mosi)).First(x => x.Value == indexMiso && x.Key != _mosi).Key, "Miso");
                    this.RaiseAndSetIfChanged(ref _sck,
                        Pico.SpiIndexByPin.OrderBy(x => Math.Abs(x.Key - _mosi)).First(x => x.Value == indexMiso && x.Key != _mosi && x.Key != _miso).Key, "Sck");
                    break;
                case "sck":
                    this.RaiseAndSetIfChanged(ref _mosi,
                        Pico.SpiIndexByPin.OrderBy(x => Math.Abs(x.Key - _sck)).First(x => x.Value == indexMiso && x.Key != _sck).Key, "Mosi");
                    this.RaiseAndSetIfChanged(ref _miso,
                        Pico.SpiIndexByPin.OrderBy(x => Math.Abs(x.Key - _sck)).First(x => x.Value == indexMiso && x.Key != _mosi && x.Key != _sck).Key, "Miso");
                    break;
            } 
        }
    }
}
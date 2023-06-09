using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Devices;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.ViewModels;

public class RestoreViewModel : ReactiveObject, IRoutableViewModel
{
    private readonly Santroller _santroller;

    public RestoreViewModel(MainWindowViewModel screen, Santroller device)
    {
        Main = screen;
        Main.Message = "Starting restore";
        Main.Progress = 0;
        HostScreen = screen;
        _santroller = device;
        GoBack = ReactiveCommand.CreateFromObservable(() => Main.GoBack.Execute(),
            this.WhenAnyValue(x => x.Main.Working).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler));
    }

    public MainWindowViewModel Main { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoBack { get; }

    public string? UrlPathSegment => Guid.NewGuid().ToString()[..5];
    public IScreen HostScreen { get; }

    public IDisposable RegisterConnections()
    {
        var ret =
            Main.AvailableDevices.Connect().Subscribe(s =>
            {
                foreach (var change in s)
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            AddDevice(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            RemoveDevice(change.Item.Current);
                            break;
                    }
            });
        _santroller.Bootloader();
        Main.Working = true;
        Main.Message = "Entering programming mode";
        Main.Progress = 10;
        return ret;
    }

    private void RemoveDevice(IConfigurableDevice device)
    {
    }

    private void AddDevice(IConfigurableDevice device)
    {
        var configFile = Path.Combine(AssetUtils.GetAppDataFolder(), "platformio", "packages", "tool-avrdude",
            "avrdude.conf");
        if (_santroller.IsPico() && device is PicoDevice)
        {
            Main.Message = "Programming";
            Main.Progress = 50;
            // Copy blank firmware back to device
            var firmware = Path.Combine(AssetUtils.GetAppDataFolder(), "default_firmwares",
                _santroller.Board.Environment + ".uf2");
            File.Copy(firmware, Path.Combine(device.GetUploadPortAsync().Result!, "firmware.uf2"));
            Main.Complete(100);
        }
        else if (_santroller.Board.HasUsbmcu && device is Dfu dfu)
        {
            Main.Message = "Programming";
            Main.Progress = 50;
            // Write back a default firmware
            var firmware = Path.Combine(AssetUtils.GetAppDataFolder(), "default_firmwares",
                _santroller.Board.Environment + "_usb_" + dfu.GetRestoreSuffix() + ".hex");
            _ = Main.Pio.RunPlatformIo("avrdude",
                new[]
                {
                    "pkg", "exec", "avrdude", "-c",
                    $"avrdude -F -C \"{configFile}\"' -p {dfu.GetRestoreProcessor()} -c flip1 -U flash:w:{firmware}:i"
                }, "", 0, 100, device).Subscribe(s => { }, s => { }, () =>
            {
                Main.Message = "Exiting Programming mode";
                Main.Progress = 90;
                dfu.Launch();
            });
        }
        else if (!_santroller.IsPico() && !_santroller.Board.HasUsbmcu && device is Arduino)
        {
            Main.Message = "Programming";
            Main.Progress = 50;
            // Erase the device so it stays in bootloader mode, the ide can just program that
            _ = Main.Pio.RunPlatformIo("avrdude",
                new[]
                {
                    "pkg", "exec", "avrdude", "-c",
                    $"avrdude -p atmega32u4 -C \"{configFile}\" -P {device.GetUploadPortAsync().Result!} -c avr109 -e"
                }, "", 0, 100, device).Subscribe(s => { }, s => { }, () => { Main.Complete(100); });
        }
        else if (!_santroller.IsPico() && _santroller.Board.HasUsbmcu && device is Arduino)
        {
            Main.Complete(100);
        }
    }
}
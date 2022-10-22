using System;
using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace GuitarConfiguratorSharp.NetCore;

public abstract class ConfigurableUsbDevice : IConfigurableDevice
{
    protected readonly UsbDevice Device;
    protected readonly string Product;
    protected readonly string Serial;
    protected readonly Version Version;
    protected readonly string Path;

    protected Board Board {get; set;}

    protected ConfigurableUsbDevice(UsbDevice device, string path, string product, string serial, ushort version)
    {
        Device = device;
        Path = path;
        Product = product;
        Serial = serial;
        Version = new Version((version >> 8) & 0xff, (version >> 4) & 0xf, (version) & 0xf);
    }

    public abstract bool MigrationSupported { get; }

    public abstract void Bootloader();
    public abstract void BootloaderUsb();
    public bool IsSameDevice(PlatformIoPort port)
    {
        return false;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return Serial == serialOrPath || Path == serialOrPath;
    }

    public byte[] ReadData(ushort wValue, byte bRequest, ushort size = 128)
    {
        UsbCtrlFlags requestType = UsbCtrlFlags.Direction_In | UsbCtrlFlags.RequestType_Class | UsbCtrlFlags.Recipient_Interface;
        byte[] buffer = new byte[size];

        var sp = new UsbSetupPacket(
            ((byte)requestType),
            bRequest,
            wValue,
            2,
            buffer.Length);
        Device.ControlTransfer(ref sp, buffer, buffer.Length, out var length);
        Array.Resize(ref buffer, length);
        return buffer;
    }


    public uint WriteData(ushort wValue, byte bRequest, byte[] buffer)
    {
        UsbCtrlFlags requestType = UsbCtrlFlags.Direction_Out | UsbCtrlFlags.RequestType_Class | UsbCtrlFlags.Recipient_Interface;


        var sp = new UsbSetupPacket(
            ((byte)requestType),
            bRequest,
            wValue,
            2,
            buffer.Length);
        Device.ControlTransfer(ref sp, buffer, buffer.Length, out var length);
        return (uint)length;
    }
    public IConfigurableDevice? BootloaderDevice { get; private set; }
    private TaskCompletionSource<string?>? _bootloaderPath;
    public bool DeviceAdded(IConfigurableDevice device)
    {
        if (Board.ArdwiinoName.Contains("pico"))
        {
            var pico = device as PicoDevice;
            if (pico != null)
            {
                _bootloaderPath?.SetResult(pico.GetPath());
            }
        }
        else if (Board.HasUsbmcu)
        {
            var dfu = device as Dfu;
            if (dfu != null && dfu.Board.HasUsbmcu && dfu.Board.Environment.Contains("arduino_uno_mega"))
            {
                BootloaderDevice = dfu;
                _bootloaderPath?.SetResult(dfu.Board.Environment);
            }
        }

        if (device is ConfigurableUsbDevice other)
        {
            return other.Serial == Serial;
        }

        if (device is Arduino arduino)
        {
            return arduino.Board.ArdwiinoName == Board.UsbUpload.ArdwiinoName;
        }
        return false;
    }

    public abstract Task LoadConfiguration(ConfigViewModel model);

    public async Task<string?> GetUploadPort()
    {
        if (Board.ArdwiinoName.Contains("pico") || Board.HasUsbmcu)
        {
            _bootloaderPath = new TaskCompletionSource<string?>();
            Bootloader();
            return await _bootloaderPath.Task;
        }

        return null;
    }

    public bool IsAvr()
    {
        return Board.IsAvr();
    }
}
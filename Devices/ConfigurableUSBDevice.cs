using System;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using Version = SemanticVersioning.Version;

namespace GuitarConfigurator.NetCore.Devices;

public abstract class ConfigurableUsbDevice : IConfigurableDevice
{
    protected readonly UsbDevice Device;
    protected readonly string Path;
    protected readonly string Product;
    protected readonly string Serial;
    protected readonly Version Version;
    private TaskCompletionSource<string?>? _bootloaderPath;

    protected ConfigurableUsbDevice(UsbDevice device, string path, string product, string serial, ushort version)
    {
        Device = device;
        Path = path;
        Product = product;
        Serial = serial;
        Version = new Version((version >> 8) & 0xff, (version >> 4) & 0xf, version & 0xf);
    }

    protected Board Board { get; set; }

    public IConfigurableDevice? BootloaderDevice { get; private set; }

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

    public void DeviceAdded(IConfigurableDevice device)
    {
        if (Board.Is32U4() && device is Arduino arduino2 && arduino2.Board.Is32U4())
        {
            _bootloaderPath?.TrySetResult(arduino2.GetSerialPort());
        }
        else if (device is PicoDevice pico && Board.IsPico())
        {
            _bootloaderPath?.TrySetResult(pico.GetPath());
        }
        else if (Board.HasUsbmcu && device is Dfu {Board.HasUsbmcu: true} dfu)
        {
            BootloaderDevice = dfu;
            _bootloaderPath?.TrySetResult(dfu.Board.Environment);
        }
    }

    public abstract Microcontroller GetMicrocontroller(ConfigViewModel model);

    public async Task<string?> GetUploadPortAsync()
    {
        if (!Board.ArdwiinoName.Contains("pico") && !Board.HasUsbmcu && !Is32U4()) return null;
        _bootloaderPath = new TaskCompletionSource<string?>();
        Bootloader();
        return await _bootloaderPath.Task;
    }

    public bool IsAvr()
    {
        return Board.IsAvr();
    }

    public bool IsGeneric()
    {
        return Board.IsGeneric();
    }

    public bool IsMini()
    {
        return Board.IsMini();
    }

    public bool IsEsp32()
    {
        return Board.IsEsp32();
    }

    public void Reconnect()
    {
    }

    public abstract void Revert();

    public bool HasDfuMode()
    {
        return Board.HasUsbmcu;
    }

    public bool Is32U4()
    {
        return Board.Is32U4();
    }

    public bool IsPico()
    {
        return Board.IsPico();
    }

    public abstract bool LoadConfiguration(ConfigViewModel model);

    public byte[] ReadData(ushort wValue, byte bRequest, ushort size = 128)
    {
        if (!Device.IsOpen) return Array.Empty<byte>();
        const UsbCtrlFlags requestType = UsbCtrlFlags.Direction_In | UsbCtrlFlags.RequestType_Class |
                                         UsbCtrlFlags.Recipient_Interface;
        var buffer = new byte[size];

        var sp = new UsbSetupPacket(
            (byte) requestType,
            bRequest,
            wValue,
            2,
            buffer.Length);

        Device.ControlTransfer(ref sp, buffer, buffer.Length, out var length);
        Array.Resize(ref buffer, length);
        return buffer;
    }


    public void WriteData(ushort wValue, byte bRequest, byte[] buffer)
    {
        if (!Device.IsOpen) return;
        var requestType = UsbCtrlFlags.Direction_Out | UsbCtrlFlags.RequestType_Class |
                          UsbCtrlFlags.Recipient_Interface;
        var sp = new UsbSetupPacket(
            (byte) requestType,
            bRequest,
            wValue,
            2,
            buffer.Length);
        Device.ControlTransfer(ref sp, buffer, buffer.Length, out var length);
    }
}
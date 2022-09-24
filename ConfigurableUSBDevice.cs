using System;
using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace GuitarConfiguratorSharp.NetCore;

public abstract class ConfigurableUsbDevice : IConfigurableDevice
{
    protected readonly UsbDevice device;
    protected readonly string product;
    protected readonly string serial;
    protected readonly Version version;
    protected readonly string path;

    public Board Board {get; protected set;}

    public ConfigurableUsbDevice(UsbDevice device, string path, string product, string serial, ushort version)
    {
        this.device = device;
        this.path = path;
        this.product = product;
        this.serial = serial;
        this.version = new Version((version >> 8) & 0xff, (version >> 4) & 0xf, (version) & 0xf);
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
        return this.serial == serialOrPath || this.path == serialOrPath;
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
        device.ControlTransfer(ref sp, buffer, buffer.Length, out var length);
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
        device.ControlTransfer(ref sp, buffer, buffer.Length, out var length);
        return (uint)length;
    }
    public IConfigurableDevice? BootloaderDevice { get; private set; }
    private TaskCompletionSource<String?>? _bootloaderPath = null;
    public bool DeviceAdded(IConfigurableDevice device)
    {
        if (this.Board.ArdwiinoName.Contains("pico"))
        {
            var pico = device as PicoDevice;
            if (pico != null)
            {
                _bootloaderPath?.SetResult(pico.GetPath());
            }
        }
        else if (this.Board.HasUsbmcu)
        {
            var dfu = device as Dfu;
            if (dfu != null && dfu.Board.HasUsbmcu && dfu.Board.Environment.Contains("arduino_uno_mega"))
            {
                BootloaderDevice = dfu;
                _bootloaderPath?.SetResult(dfu.Board.Environment);
            }
        }
        var other = device as ConfigurableUsbDevice;
        if (other != null)
        {
            return other.serial == serial;
        }
        var arduino = device as Arduino;
        if (arduino != null)
        {
            return arduino.Board.ArdwiinoName == Board.UsbUpload.ArdwiinoName;
        }
        return false;
    }

    public abstract void LoadConfiguration(ConfigViewModel model);

    public async Task<string?> GetUploadPort()
    {
        if (this.Board.ArdwiinoName.Contains("pico") || this.Board.HasUsbmcu)
        {
            _bootloaderPath = new TaskCompletionSource<string?>();
            Bootloader();
            return await _bootloaderPath.Task;
        }
        else
        {
            return null;
        }
    }

}
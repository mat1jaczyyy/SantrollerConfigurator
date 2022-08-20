
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using GuitarConfiguratorSharp.Utils;
using GuitarConfiguratorSharp.Configuration;
using System.Threading.Tasks;

public abstract class ConfigurableUSBDevice : ConfigurableDevice
{
    protected readonly UsbDevice device;
    protected readonly string product;
    protected readonly string serial;
    protected readonly Version version;
    protected readonly string path;

    protected Board board;

    public ConfigurableUSBDevice(UsbDevice device, string path, string product, string serial, ushort version)
    {
        this.device = device;
        this.path = path;
        this.product = product;
        this.serial = serial;
        this.version = new Version((version >> 8) & 0xff, (version >> 4) & 0xf, (version) & 0xf);
    }

    public abstract bool MigrationSupported { get; }
    public abstract DeviceConfiguration Configuration { get; }

    public abstract void Bootloader();
    public abstract void BootloaderUSB();
    public bool IsSameDevice(PlatformIOPort port)
    {
        return false;
    }

    public bool IsSameDevice(string serial_or_path)
    {
        return this.serial == serial_or_path || this.path == serial_or_path;
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
    public ConfigurableDevice? BootloaderDevice { get; private set; }
    private TaskCompletionSource<String?>? _bootloaderPath = null;
    public void DeviceAdded(ConfigurableDevice device)
    {
        if (this.board.ardwiinoName.Contains("pico"))
        {
            var pico = device as PicoDevice;
            if (pico != null)
            {
                _bootloaderPath?.SetResult(pico.GetPath());
            }
        }
        else if (this.board.hasUSBMCU)
        {
            var dfu = device as Dfu;
            if (dfu != null && dfu.Board.hasUSBMCU && dfu.Board.environment.Contains("arduino_uno_mega"))
            {
                BootloaderDevice = dfu;
                _bootloaderPath?.SetResult(dfu.Board.environment);
            }
        }
    }

    public async Task ExitBootloader()
    {
        var dfu = BootloaderDevice as Dfu;
        if (dfu != null)
        {
            // await dfu.ExitDFU();
        }
    }

    public async Task<string?> getUploadPort()
    {
        if (this.board.ardwiinoName.Contains("pico") || this.board.hasUSBMCU)
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
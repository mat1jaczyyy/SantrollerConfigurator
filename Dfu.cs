using System;
using GuitarConfiguratorSharp.Utils;
using GuitarConfiguratorSharp.Configuration;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.Main;

public class Dfu : ConfigurableDevice
{
    public static readonly uint DFU_PID_8U2 = 0x2FF7;
    public static readonly uint DFU_PID_16U2 = 0x2FEF;
    public static readonly uint DFU_VID = 0x03eb;

    public Board Board { get; }

    public bool MigrationSupported => true;

    public DeviceConfiguration? Configuration => null;

    private DeviceNotifyEventArgs Args;

    private string port;

    public Dfu(DeviceNotifyEventArgs args)
    {
        this.Args = args;
        var pid = args.Device.IdProduct;
        this.port = args.Device.Name;
        foreach (var board in Board.Boards)
        {
            if (board.productIDs.Contains((uint)pid) && board.hasUSBMCU)
            {
                this.Board = board;
                return;
            }
        }
        throw new InvalidOperationException("Not expected");
    }

    public bool IsSameDevice(PlatformIOPort port)
    {
        return false;
    }

    public bool IsSameDevice(string serial_or_path)
    {
        return serial_or_path == this.port;
    }

    public bool DeviceAdded(ConfigurableDevice device)
    {
        return false;
    }

    public Task<string?> getUploadPort()
    {
        return Task.FromResult((string?)this.port);
    }

    public void Bootloader()
    {
    }

    public void BootloaderUSB()
    {
    }
}
using System;
using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using LibUsbDotNet.DeviceNotify;

namespace GuitarConfiguratorSharp.NetCore;

public class Dfu : IConfigurableDevice
{
    public static readonly uint DfuPid8U2 = 0x2FF7;
    public static readonly uint DfuPid16U2 = 0x2FEF;
    public static readonly uint DfuVid = 0x03eb;

    public Board Board { get; }

    public bool MigrationSupported => true;

    private DeviceNotifyEventArgs _args;

    private readonly string _port;

    public Dfu(DeviceNotifyEventArgs args)
    {
        _args = args;
        var pid = args.Device.IdProduct;
        _port = args.Device.Name;
        foreach (var board in Board.Boards)
        {
            if (board.ProductIDs.Contains((uint)pid) && board.HasUsbmcu)
            {
                Board = board;
                return;
            }
        }
        throw new InvalidOperationException("Not expected");
    }

    public bool IsSameDevice(PlatformIoPort port)
    {
        return false;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return serialOrPath == _port;
    }

    public bool DeviceAdded(IConfigurableDevice device)
    {
        return false;
    }

    public void LoadConfiguration(ConfigViewModel model)
    {
    }

    public Task<string?> GetUploadPort()
    {
        return Task.FromResult((string?)_port);
    }

    public bool IsAvr()
    {
        return true;
    }

    public void Bootloader()
    {
    }

    public void BootloaderUsb()
    {
    }
}
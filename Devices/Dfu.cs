using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.Main;

namespace GuitarConfigurator.NetCore.Devices;

public class Dfu : IConfigurableDevice
{
    public static readonly uint DfuPid8U2 = 0x2FF7;
    public static readonly uint DfuPid16U2 = 0x2FEF;
    public static readonly uint DfuVid = 0x03eb;

    private readonly DeviceNotifyEventArgs _args;

    private readonly string _port;

    public Dfu(DeviceNotifyEventArgs args)
    {
        _args = args;
        var pid = args.Device.IdProduct;
        _port = args.Device.Name;
        foreach (var board in Board.Boards)
            if (board.ProductIDs.Contains((uint) pid) && board.HasUsbmcu)
            {
                Board = board;
                Console.WriteLine(Board.Environment);
                return;
            }

        throw new InvalidOperationException("Not expected");
    }

    public Board Board { get; }

    public bool MigrationSupported => true;

    public bool IsSameDevice(PlatformIoPort port)
    {
        return false;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return serialOrPath == _port;
    }

    public void DeviceAdded(IConfigurableDevice device)
    {
    }

    
    public Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        var board = Board;
        if (Board.ArdwiinoName == "usb")
            switch (model.Main.UnoMegaType)
            {
                case UnoMegaType.Uno:
                    board = Board.Uno;
                    break;
                case UnoMegaType.MegaAdk:
                    board = Board.MegaBoards[1];
                    break;
                case UnoMegaType.Mega:
                    board = Board.MegaBoards[0];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        return Board.FindMicrocontroller(board);
    }

    public bool LoadConfiguration(ConfigViewModel model)
    {
        return false;
    }

    public Task<string?> GetUploadPortAsync()
    {
        return Task.FromResult<string?>(null);
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

    public bool IsPico()
    {
        return false;
    }

    public bool IsMini()
    {
        return false;
    }

    public void Reconnect()
    {
    }

    public bool IsGeneric()
    {
        return false;
    }

    public override string ToString()
    {
        return $"{Board.Name} ({_port})";
    }
}
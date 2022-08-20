using System;
using GuitarConfiguratorSharp.Utils;
using GuitarConfiguratorSharp.Configuration;
using System.Threading.Tasks;
using LibUsbDotNet;
using System.Collections.Generic;
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
        throw new NotImplementedException();
    }

    public bool IsSameDevice(string serial_or_path)
    {
        throw new NotImplementedException();
    }

    public void DeviceAdded(ConfigurableDevice device)
    {
        throw new NotImplementedException();
    }

    public Task<string?> getUploadPort()
    {
        throw new NotImplementedException();
    }

    public void ExitBootloader()
    {
        Args.Device.Open(out UsbDevice dev);
        // Initialise the device with https://github.com/dfu-programmer/dfu-programmer/blob/master/src/dfu.c : dfu_device_init
        // Then, https://github.com/dfu-programmer/dfu-programmer/blob/master/src/atmel.c atmel_start_app_reset

        // UsbCtrlFlags requestType = UsbCtrlFlags.Direction_Out | UsbCtrlFlags.RequestType_Class | UsbCtrlFlags.Recipient_Interface;
        // var buffer = new uint[]{};

        // var sp = new UsbSetupPacket(
        //     ((byte)requestType),
        //     1,
        //     wValue,
        //     2,
        //     buffer.Length);
        // device.ControlTransfer(ref sp, buffer, buffer.Length, out var length);
        // return (uint)length;
    }

    public void Bootloader()
    {
        throw new NotImplementedException();
    }

    public void BootloaderUSB()
    {
        throw new NotImplementedException();
    }
}
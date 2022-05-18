
using Device.Net;
using Usb.Net;
using System;
using GuitarConfiguratorSharp.Utils;

public class Arduino : ConfigurableDevice
{
    public static readonly FilterDeviceDefinition ArduinoDeviceFilter = new FilterDeviceDefinition();
    private PlatformIOPort port;

    private Board board;

    public Arduino(PlatformIOPort port)
    {
        this.port = port;
        foreach (var board in Board.Boards) {
            if (board.productIDs.Contains(port.Pid)) {
                this.board = board;
                return;
            }
        }
        this.board = Board.Generic;
    }

    public bool IsSameDevice(IDevice device)
    {
        return false;
    }

    public bool IsSameDevice(PlatformIOPort port)
    {
        return port == this.port;
    }

    public bool IsSameDevice(string path)
    {
        return false;
    }

    public string GetSerialPort() {
        return port.Port;
    }
    
    public override String ToString()
    {
        return $"{board.name} ({port.Port})";
    }
}
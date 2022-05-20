
using Device.Net;
using Usb.Net;
using System;
using System.IO.Ports;
using GuitarConfiguratorSharp.Utils;

public class Arduino : ConfigurableDevice
{
    public static readonly FilterDeviceDefinition ArduinoDeviceFilter = new FilterDeviceDefinition();
    private PlatformIOPort port;

    private Board board;

    public Arduino(PlatformIOPort port)
    {
        this.port = port;
        foreach (var board in Board.Boards)
        {
            if (board.productIDs.Contains(port.Pid))
            {
                this.board = board;
                return;
            }
        }
        if (port.Vid == 0x1209 && port.Pid == 0x2882)
        {
            this.board = Board.OldArdwiino;
            System.IO.Ports.SerialPort serial = new System.IO.Ports.SerialPort(port.Port, 115200);
            serial.Open();
            serial.Write("i\x06\n");
            var boardName = serial.ReadTo("\0");
            serial.Write("i\x04\n");
            var boardFreq = UInt32.Parse(serial.ReadTo("\0"));
            this.board = new Board(boardName, $"Ardwiino - {Board.findBoard(boardName).name} - {boardFreq} - pre 4.3.7 - {Board.OldArdwiino.name}", boardName, Board.OldArdwiino.productIDs);

        }
        else
        {
            this.board = Board.Generic;
        }
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

    public string GetSerialPort()
    {
        return port.Port;
    }

    public override String ToString()
    {
        return $"{board.name} ({port.Port})";
    }
}
using System;
using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Utils;

namespace GuitarConfiguratorSharp.NetCore;

public class Arduino : IConfigurableDevice
{
    // public static readonly FilterDeviceDefinition ArduinoDeviceFilter = new FilterDeviceDefinition();
    private readonly PlatformIoPort _port;

    public Board Board { get; }

    public bool MigrationSupported { get; }

    private readonly DeviceConfiguration? _config;

    public DeviceConfiguration? Configuration => _config;

    public Arduino(PlatformIo pio, PlatformIoPort port)
    {
        this._port = port;
        foreach (var board in Board.Boards)
        {
            if (board.ProductIDs.Contains(port.Pid))
            {
                this.Board = board;
                this.MigrationSupported = true;
                _config = new DeviceConfiguration(Board.FindMicrocontroller(this.Board));
                return;
            }
        }
        // Really, really old ardwiinos had a serial protocol that response to a couple of commands for retrieving data.
        if (port.Vid == 0x1209 && port.Pid == 0x2882)
        {
            this.MigrationSupported = false;

            System.IO.Ports.SerialPort serial = new System.IO.Ports.SerialPort(port.Port, 115200);
            serial.Open();
            serial.Write("i\x06\n");
            var boardName = serial.ReadLine().Trim();
            serial.DiscardInBuffer();
            serial.Write("i\x04\n");
            var boardFreqStr = serial.ReadLine().Replace("UL", "");
            var boardFreq = UInt32.Parse(boardFreqStr);
            var tmp = Board.FindBoard(boardName, boardFreq);
            this.Board = new Board(boardName, $"Ardwiino - {tmp.Name} - pre 4.3.7", boardFreq, tmp.Environment, tmp.ProductIDs, tmp.HasUsbmcu);
            _config = new DeviceConfiguration(Board.FindMicrocontroller(this.Board));
        }
        else
        {
            this.Board = Board.Generic;
            this.MigrationSupported = true;
        }
    }

    public bool IsSameDevice(PlatformIoPort port)
    {
        return port == this._port;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return false;
    }

    public string GetSerialPort()
    {
        return _port.Port;
    }

    public override String ToString()
    {
        return $"{Board.Name} ({_port.Port})";
    }

    public void Bootloader()
    {
        // Automagically handled by pio
    }

    public void BootloaderUsb()
    {
        // Automagically handled by pio
    }

    public Task<string?> GetUploadPort()
    {
        return Task.FromResult((string?)GetSerialPort());
    }

    public bool DeviceAdded(IConfigurableDevice device)
    {
        return false;
    }
}
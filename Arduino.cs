
using LibUsbDotNet;
using System;
using GuitarConfiguratorSharp.Utils;
using GuitarConfiguratorSharp.Configuration;
using System.Threading.Tasks;

public class Arduino : ConfigurableDevice
{
    // public static readonly FilterDeviceDefinition ArduinoDeviceFilter = new FilterDeviceDefinition();
    private PlatformIOPort port;

    public Board Board { get; }

    public bool MigrationSupported { get; }

    private DeviceConfiguration? _config;

    public DeviceConfiguration? Configuration => _config;

    public Arduino(PlatformIO pio, PlatformIOPort port)
    {
        this.port = port;
        foreach (var board in Board.Boards)
        {
            if (board.productIDs.Contains(port.Pid))
            {
                this.Board = board;
                this.MigrationSupported = true;
                _config = new DeviceConfiguration(Board.findMicrocontroller(this.Board));
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
            var tmp = Board.findBoard(boardName, boardFreq);
            this.Board = new Board(boardName, $"Ardwiino - {tmp.name} - pre 4.3.7", boardFreq, tmp.environment, tmp.productIDs, tmp.hasUSBMCU);
            _config = new DeviceConfiguration(Board.findMicrocontroller(this.Board));
        }
        else
        {
            this.Board = Board.Generic;
            this.MigrationSupported = true;
        }
    }

    public bool IsSameDevice(PlatformIOPort port)
    {
        return port == this.port;
    }

    public bool IsSameDevice(string serial_or_path)
    {
        return false;
    }

    public string GetSerialPort()
    {
        return port.Port;
    }

    public override String ToString()
    {
        return $"{Board.name} ({port.Port})";
    }

    public void Bootloader()
    {
        // Automagically handled by pio
    }

    public void BootloaderUSB()
    {
        // Automagically handled by pio
    }

    public Task<string?> getUploadPort()
    {
        return Task.FromResult((string?)GetSerialPort());
    }

    public bool DeviceAdded(ConfigurableDevice device)
    {
        return false;
    }
}
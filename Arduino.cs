using System;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore;

public class Arduino : IConfigurableDevice
{
    // public static readonly FilterDeviceDefinition ArduinoDeviceFilter = new FilterDeviceDefinition();
    private readonly PlatformIoPort _port;

    public Board Board { get; }

    public bool MigrationSupported { get; }
    
    public Subject<bool> DfuDetected { get; }

    public Arduino(PlatformIo pio, PlatformIoPort port)
    {
        DfuDetected = new Subject<bool>();
        _port = port;
        foreach (var board in Board.Boards)
        {
            if (board.ProductIDs.Contains(port.Pid))
            {
                Board = board;
                MigrationSupported = true;
                return;
            }
        }
        // Really, really old ardwiinos had a serial protocol that response to a couple of commands for retrieving data.
        if (port.Vid == 0x1209 && port.Pid == 0x2882)
        {
            MigrationSupported = false;

            var serial = new SerialPort(port.Port, 115200);
            serial.Open();
            serial.Write("i\x06\n");
            var boardName = serial.ReadLine().Trim();
            serial.DiscardInBuffer();
            serial.Write("i\x04\n");
            var boardFreqStr = serial.ReadLine().Replace("UL", "");
            var boardFreq = uint.Parse(boardFreqStr);
            var tmp = Board.FindBoard(boardName, boardFreq);
            Board = new Board(boardName, $"Ardwiino - {tmp.Name} - pre 4.3.7", boardFreq, tmp.Environment, tmp.ProductIDs, tmp.HasUsbmcu);
        }
        else
        {
            Board = Board.Generic;
            MigrationSupported = true;
        }
    }

    public bool IsSameDevice(PlatformIoPort port)
    {
        return port == _port;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return false;
    }

    public string GetSerialPort()
    {
        return _port.Port;
    }

    public override string ToString()
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

    public bool Is32U4()
    {
        return Board.Atmega32U4Boards.Contains(Board);
    }
    
    public bool IsUno()
    {
        return Board.Uno.Name == Board.Name;
    }
    
    public bool IsMega()
    {
        return Board.MegaBoards.Contains(Board);
    }

    public async Task LoadConfiguration(ConfigViewModel model)
    {
        await model.SetDefaults(Board.FindMicrocontroller(Board));
    }

    public Task<string?> GetUploadPort()
    {
        return Task.FromResult((string?)GetSerialPort());
    }

    public bool IsAvr()
    {
        return true;
    }
 
    public bool DeviceAdded(IConfigurableDevice device)
    {

        Console.WriteLine(device);
        switch (device)
        {
            case Dfu when !Is32U4():
            {
                DfuDetected.OnNext(true);
                break;
            }
            case Santroller:
                return true;
        }

        return false;
    }
}
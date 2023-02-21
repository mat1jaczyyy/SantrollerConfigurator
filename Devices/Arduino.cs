using System;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Devices;

public class Arduino : IConfigurableDevice
{
    // public static readonly FilterDeviceDefinition ArduinoDeviceFilter = new FilterDeviceDefinition();
    private readonly PlatformIoPort _port;
    private readonly bool _generic;

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
        // Handle any other RP2040 based boards we don't already have code for, using just the generic rp2040 board in this case
        if (port.Vid == Board.RaspberryPiVendorID)
        {
            Board = Board.Rp2040Boards[0];
            MigrationSupported = true;
            return;
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
            Board = new Board(boardName, $"Ardwiino - {tmp.Name} - pre 4.3.7", boardFreq, tmp.Environment,
                tmp.ProductIDs, tmp.HasUsbmcu);
        }
        else
        {
            Board = Board.Generic;
            MigrationSupported = true;
            _generic = true;
        }
    }

    public Board Board { get; set; }

    public Subject<bool> DfuDetected { get; }

    public bool MigrationSupported { get; }

    public bool IsSameDevice(PlatformIoPort port)
    {
        return port == _port;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return false;
    }

    public void Bootloader()
    {
        // Automagically handled by pio
    }

    public void BootloaderUsb()
    {
        // Automagically handled by pio
    }

    public Microcontroller GetMicrocontroller(ConfigViewModel model)
    {
        if (!_generic) return Board.FindMicrocontroller(Board);
        Board = model.Main.AvrType switch
        {
            AvrType.Mini => Board.MiniBoards[0],
            AvrType.ProMicro => Board.Atmega32U4Boards[2],
            AvrType.Leonardo =>Board.Atmega32U4Boards[4],
            AvrType.Micro => Board.Atmega32U4Boards[1],
            AvrType.Uno => Board.Uno,
            AvrType.Mega => Board.MegaBoards[0],
            AvrType.MegaAdk => Board.MegaBoards[1],
            _ => throw new ArgumentOutOfRangeException()
        };
        return Board.FindMicrocontroller(Board);
    }
    public bool IsMini()
    {
        return Board.IsMini();
    }

    public bool LoadConfiguration(ConfigViewModel model)
    {
        return false;
    }

    public Task<string?> GetUploadPort()
    {
        return Task.FromResult((string?) GetSerialPort());
    }

    public bool IsAvr()
    {
        return true;
    }

    public bool DeviceAdded(IConfigurableDevice device)
    {
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

    public bool IsPico()
    {
        return Board.IsPico();
    }

    public string GetSerialPort()
    {
        return _port.Port;
    }

    public override string ToString()
    {
        return $"{Board.Name} ({_port.Port})";
    }

    public bool Is32U4()
    {
        return Board.Atmega32U4Boards.Contains(Board);
    }

    public bool IsUno()
    {
        return Board.Uno.Name == Board.Name;
    }

    public bool IsGeneric()
    {
        return _generic;
    }

    public bool IsMega()
    {
        return Board.MegaBoards.Contains(Board);
    }
}
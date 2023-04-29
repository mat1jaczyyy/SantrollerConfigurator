using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
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
        if (port.Vid == Board.RaspberryPiVendorId)
        {
            Board = Board.Rp2040Boards[0];
            MigrationSupported = true;
            return;
        }

        // Really, really old ardwiinos had a serial protocol that response to a couple of commands for retrieving data.
        if (port is {Vid: 0x1209, Pid: 0x2882})
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
            serial.Close();
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
        if (model.Main.IsDfu || model.Main.IsMega || model.Main.IsUno)
        {
            var unoMegaType = model.Main.UnoMegaType;
            Board = unoMegaType switch
            {
                UnoMegaType.Uno => Board.Uno,
                UnoMegaType.MegaAdk => Board.MegaBoards[1],
                UnoMegaType.Mega => Board.MegaBoards[0],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        if (model.Main.Is32U4)
        {
            var configFile = Path.Combine(AssetUtils.GetAppDataFolder(), "platformio", "packages", "tool-avrdude", "avrdude.conf");
            model.Main.Pio.RunPlatformIo("avrdude",
                new[]
                {
                    "pkg", "exec", "avrdude", "-c",
                    $"avrdude -p atmega32u4 -C {configFile} -P {_port.Port} -c avr109 -e"
                }, "", 0, 100, this).Wait();

            var u4Mode = model.Main.Arduino32U4Type;
            Board = u4Mode switch
            {
                Arduino32U4Type.ProMicro => Board.Atmega32U4Boards[2],
                Arduino32U4Type.Leonardo => Board.Atmega32U4Boards[4],
                Arduino32U4Type.Micro => Board.Atmega32U4Boards[1],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        if (model.Main.IsGeneric)
        {
            Board = model.Main.AvrType switch
            {
                AvrType.Mini => Board.MiniBoards[0],
                AvrType.ProMicro => Board.Atmega32U4Boards[2],
                AvrType.Leonardo => Board.Atmega32U4Boards[4],
                AvrType.Micro => Board.Atmega32U4Boards[1],
                AvrType.Uno => Board.Uno,
                AvrType.Mega => Board.MegaBoards[0],
                AvrType.MegaAdk => Board.MegaBoards[1],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return Board.FindMicrocontroller(Board);
    }

    public bool IsMini()
    {
        return Board.IsMini();
    }

    public void Reconnect()
    {
    }

    public bool LoadConfiguration(ConfigViewModel model)
    {
        return false;
    }

    public Task<string?> GetUploadPortAsync()
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
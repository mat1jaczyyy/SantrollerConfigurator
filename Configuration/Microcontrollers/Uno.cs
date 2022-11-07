using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class Uno : AvrController
{
    protected override int SpiMiso => 12;

    protected override int SpiMosi => 11;

    protected override int SpiCSn => 10;
    protected override int SpiSck => 13;

    protected override int I2CSda => 18;

    protected override int I2CScl => 19;
    private static readonly int[] PinIndex = {
        0, /* 0, port D */
        1, 2, 3, 4, 5, 6,
        7, 0,                                 /* 8, port B */
        1, 2, 3, 4, 5, 0, /* 14, port C */
        1, 2, 3, 4, 5
    };

    private static readonly char[] Ports = {
        'D',                             /* 0 */
        'D', 'D', 'D', 'D', 'D', 'D', 'D', 'B', /* 8 */
        'B', 'B', 'B', 'B', 'B', 'C',         /* 14 */
        'C', 'C', 'C', 'C', 'C'
    };
    protected override char[] PortNames => new [] { 'B', 'C', 'D' };
    
    private Dictionary<Tuple<char, int>, int> _pinByMask = Enumerable.Zip(Ports, PinIndex).Select((tuple, i) => new Tuple<char, int, int>(tuple.First, tuple.Second, i)).ToDictionary(s => new Tuple<char, int>(s.Item1, s.Item2), s=>s.Item3);
    protected override Dictionary<Tuple<char, int>, int> PinByMask => _pinByMask;
    
    public static readonly Dictionary<int, string> Interrupts = new ()
    {
        {2, "INT0"},
        {3, "INT1"},
    };
    protected override int PinA0 => 14;

    public override int PinCount => PinIndex.Length;
    protected override string GetInterruptForPin(int ack)
    {
        return Interrupts[ack];
    }

    public override List<int> SupportedAckPins()
    {
        return Interrupts.Keys.ToList();
    }

    public override Board Board {get;}

    public Uno(Board board) {
        Board = board;
    }

    public override int GetIndex(int pin)
    {
        return PinIndex[pin];
    }

    public override char GetPort(int pin)
    {
        return Ports[pin];
    }

    public override int GetChannel(int pin)
    {
        return PinA0 - pin;
    }

    public override AvrPinMode? ForcedMode(int pin)
    {
        switch (pin)
        {
            case 0:
                return AvrPinMode.Input;
            case 1:
                return AvrPinMode.Output;
            case 13:
                return AvrPinMode.Input;
            default:
                return null;
        }
    }
    
    public override List<int> GetFreePins()
    {
        var used = PinConfigs.SelectMany(s => s.Pins).ToHashSet();
        return Enumerable.Range(0, PinIndex.Length).Where(s => !used.Contains(s)).ToList();
    }
}
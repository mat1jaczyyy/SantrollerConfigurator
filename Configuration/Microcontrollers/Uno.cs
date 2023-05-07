using System.Collections.Generic;
using System.Linq;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class Uno : AvrController
{
    private static readonly int A0 = 14;

    private static readonly int[] PinIndex =
    {
        0, /* 0, port D */
        1, 2, 3, 4, 5, 6,
        7, 0, /* 8, port B */
        1, 2, 3, 4, 5, 0, /* 14, port C */
        1, 2, 3, 4, 5
    };

    private static readonly char[] Ports =
    {
        'D', /* 0 */
        'D', 'D', 'D', 'D', 'D', 'D', 'D', 'B', /* 8 */
        'B', 'B', 'B', 'B', 'B', 'C', /* 14 */
        'C', 'C', 'C', 'C', 'C'
    };

    public static readonly Dictionary<int, string> Interrupts = new()
    {
        {2, "INT0"},
        {3, "INT1"}
    };

    public Uno(Board board)
    {
        Board = board;
    }

    protected override int SpiMiso => 12;

    protected override int SpiMosi => 11;

    protected override int SpiCSn => 10;
    protected override int SpiSck => 13;

    protected override int I2CSda => 18;

    protected override int I2CScl => 19;

    protected override char[] PortNames { get; } = {'B', 'C', 'D'};

    protected override Dictionary<(char, int), int> PinByMask { get; } = Ports.Zip(PinIndex)
        .Select((tuple, i) => (tuple.First, tuple.Second, i))
        .ToDictionary(s => (s.Item1, s.Item2), s => s.Item3);

    protected override int PinA0 => A0;

    public override int PinCount => PinIndex.Length;

    public override Board Board { get; }

    public override List<int> AnalogPins { get; } = Enumerable.Range(A0, 4).ToList();

    public override List<int> PwmPins { get; } = new() {3, 5, 6, 9, 10, 11};

    protected override string GetInterruptForPin(int ack)
    {
        return Interrupts[ack];
    }

    public override List<int> SupportedAckPins()
    {
        return Interrupts.Keys.ToList();
    }

    public override int GetIndex(int pin)
    {
        return PinIndex[pin];
    }

    public override char GetPort(int pin)
    {
        return Ports[pin];
    }

    public override int GetChannel(int pin, bool reconfigurePin)
    {
        var chan = PinA0 - pin;
        if (reconfigurePin) chan |= 1 << 7;
        return chan;
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

    public override int GetFirstDigitalPin()
    {
        return 2;
    }

    public override List<int> GetAllPins(bool isAnalog)
    {
        return isAnalog ? AnalogPins : Enumerable.Range(0, PinIndex.Length).ToList();
    }
}
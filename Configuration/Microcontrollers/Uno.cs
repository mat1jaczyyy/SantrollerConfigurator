namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class Uno : AvrController
{
    protected override int SpiMiso => 12;

    protected override int SpiMosi => 11;

    protected override int SpiCSn => 10;
    protected override int SpiSck => 13;

    protected override int I2CSda => 18;

    protected override int I2CScl => 19;
    private readonly int[] _pinInputs = {
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
    private static readonly char[] PortNames = { 'B', 'C', 'D' };
    protected override int PinA0 => 14;

    public override int PinCount => _pinInputs.Length;

    public override Board Board {get;}

    public Uno(Board board) {
        Board = board;
    }

    public override int GetIndex(int pin)
    {
        return _pinInputs[pin];
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
                return AvrPinMode.INPUT;
            case 1:
                return AvrPinMode.OUTPUT;
            case 13:
                return AvrPinMode.INPUT;
            default:
                return null;
        }
    }
}
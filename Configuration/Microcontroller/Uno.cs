namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class Uno : AvrController
{
    public override int SpiRx => 12;

    public override int SpiTx => 11;

    public override int SpiCSn => 10;
    public override int SpiSck => 13;

    public override int I2CSda => 18;

    public override int I2CScl => 19;
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
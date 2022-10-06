namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public class Micro : AvrController
{
    protected override int SpiMiso => 14;

    protected override int SpiMosi => 16;

    protected override int SpiCSn => 17;
    protected override int SpiSck => 15;

    protected override int I2CSda => 2;

    protected override int I2CScl => 3;
    private readonly int[] _pinIndex = {
        2, // D0 - PD2
        3,	// D1 - PD3
        1, // D2 - PD1
        0,	// D3 - PD0
        4,	// D4 - PD4
        6, // D5 - PC6
        7, // D6 - PD7
        6, // D7 - PE6
            
        4, // D8 - PB4
        5,	// D9 - PB5
        6, // D10 - PB6
        7,	// D11 - PB7
        6, // D12 - PD6
        7, // D13 - PC7
            
        3,	// D14 - MISO - PB3
        1,	// D15 - SCK - PB1
        2,	// D16 - MOSI - PB2
        0,	// D17 - SS - PB0
            
        7,	// D18 - A0 - PF7
        6, // D19 - A1 - PF6
        5, // D20 - A2 - PF5
        4, // D21 - A3 - PF4
        1, // D22 - A4 - PF1
        0, // D23 - A5 - PF0
            
        4, // D24 / D4 - A6 - PD4
        7, // D25 / D6 - A7 - PD7
        4, // D26 / D8 - A8 - PB4
        5, // D27 / D9 - A9 - PB5
        6, // D28 / D10 - A10 - PB6
        6, // D29 / D12 - A11 - PD6
        5, // D30 / TX Led - PD5	
    };

    public override int PinCount => _pinIndex.Length;

    private static readonly char[] Ports = {
        'D', // D0 - 'D'2
        'D',	// D1 - 'D'3
        'D', // D2 - 'D'1
        'D',	// D3 - 'D'0
        'D',	// D4 - 'D'4
        'C', // D5 - 'C'6
        'D', // D6 - 'D'7
        'E', // D7 - 'E'6

        'B', // D8 - 'B'4
        'B',	// D9 - 'B'5
        'B', // D10 - 'B'6
        'B',	// D11 - 'B'7
        'D', // D12 - 'D'6
        'C', // D13 - 'C'7

        'B',	// D14 - MISO - 'B'3
        'B',	// D15 - SCK - 'B'1
        'B',	// D16 - MOSI - 'B'2
        'B',	// D17 - SS - 'B'0

        'F',	// D18 - A0 - 'F'7
        'F', // D19 - A1 - 'F'6
        'F', // D20 - A2 - 'F'5
        'F', // D21 - A3 - 'F'4
        'F', // D22 - A4 - 'F'1
        'F', // D23 - A5 - 'F'0

        'D', // D24 / D4 - A6 - 'D'4
        'D', // D25 / D6 - A7 - 'D'7
        'B', // D26 / D8 - A8 - 'B'4
        'B', // D27 / D9 - A9 - 'B'5
        'B', // D28 / D10 - A10 - 'B'6
        'D', // D29 / D12 - A11 - 'D'6
        'D' // D30 / TX Led - 'D'5            
    };
    private static readonly char[] PortNames = { 'B', 'C', 'D', 'E', 'F' };

    private static readonly int[] Channels = {
        7,	// A0				PF7					ADC7
        6,	// A1				PF6					ADC6	
        5,	// A2				PF5					ADC5	
        4,	// A3				PF4					ADC4
        1,	// A4				PF1					ADC1	
        0,	// A5				PF0					ADC0	
        8,	// A6		D4		PD4					ADC8
        10,	// A7		D6		PD7					ADC10
        11,	// A8		D8		PB4					ADC11
        12,	// A9		D9		PB5					ADC12
        13,	// A10		D10		PB6					ADC13
        9	// A11		D12		PD6					ADC9
    };

    protected override int PinA0 => 18;

    public override Board Board {get;}

    public Micro(Board board) {
        Board = board;
    }
    public override int GetIndex(int pin)
    {
        return _pinIndex[pin];
    }

    public override char GetPort(int pin)
    {
        return Ports[pin];
    }
    public override int GetChannel(int pin)
    {
        return Channels[pin - PinA0];
    }

    public override AvrPinMode? ForcedMode(int pin)
    {
        return null;
    }
}
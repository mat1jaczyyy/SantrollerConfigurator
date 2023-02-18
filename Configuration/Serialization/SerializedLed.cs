using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Leds;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedLed : SerializedOutput
{
    public SerializedLed(Color ledOn, Color ledOff, byte[] ledIndex, RumbleCommand type, bool outputEnabled, int pin)
    {
        LedOn = ledOn.ToUint32();
        LedOff = ledOff.ToUint32();
        LedIndex = ledIndex;
        Type = type;
        OutputEnabled = outputEnabled;
        Pin = pin;
    }

    public override SerializedInput? Input => null;
    [ProtoMember(1)] public override uint LedOn { get; }
    [ProtoMember(2)] public override uint LedOff { get; }
    [ProtoMember(3)] public override byte[] LedIndex { get; }
    [ProtoMember(4)] public RumbleCommand Type { get; }
    [ProtoMember(5)] public bool OutputEnabled { get; }
    [ProtoMember(6)] public int Pin { get; }

    public override Output Generate(ConfigViewModel model)
    {
        return new Led(model, OutputEnabled, Pin, Color.FromUInt32(LedOn), Color.FromUInt32(LedOff),
            LedIndex, Type);
    }
}
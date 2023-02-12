using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Leds;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedLed : SerializedOutput
{
    public override SerializedInput? Input => null;
    [ProtoMember(1)] public override uint LedOn { get; }
    [ProtoMember(2)] public override uint LedOff { get; }
    [ProtoMember(3)] public override byte[] LedIndex { get; }
    [ProtoMember(4)] public RumbleCommand Type { get; }

    public SerializedLed(Color ledOn, Color ledOff, byte[] ledIndex, RumbleCommand type) {
        LedOn = ledOn.ToUint32();
        LedOff = ledOff.ToUint32();
        LedIndex = ledIndex;
        Type = type;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new Led(model, Color.FromUInt32(LedOn), Color.FromUInt32(LedOff), LedIndex, Type);
    }
}
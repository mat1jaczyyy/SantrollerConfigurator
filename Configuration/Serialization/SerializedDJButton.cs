using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.DJ;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDjButton : SerializedOutput
{
    public SerializedDjButton(SerializedInput input, Color ledOn, Color ledOff, byte[] ledIndex, byte debounce,
        DjInputType type)
    {
        Input = input;
        LedOn = ledOn.ToUint32();
        LedOff = ledOff.ToUint32();
        LedIndex = ledIndex;
        Debounce = debounce;
        Type = type;
    }

    [ProtoMember(1)] public override SerializedInput Input { get; }
    [ProtoMember(2)] public override uint LedOn { get; }
    [ProtoMember(3)] public override uint LedOff { get; }
    [ProtoMember(6)] public override byte[] LedIndex { get; }
    [ProtoMember(4)] public byte Debounce { get; }
    [ProtoMember(5)] public DjInputType Type { get; }

    public override Output Generate(ConfigViewModel model)
    {
        return new DjButton(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, Debounce, Type);
    }
}
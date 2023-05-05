using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedControllerButton : SerializedOutput
{
    public SerializedControllerButton(SerializedInput input, Color ledOn, Color ledOff, byte[] ledIndex, byte debounce,
        StandardButtonType type)
    {
        Input = input;
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        Debounce = debounce;
        Type = type;
    }

    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public uint LedOn { get; }
    [ProtoMember(3)] public uint LedOff { get; }
    [ProtoMember(6)] public byte[] LedIndex { get; }
    [ProtoMember(4)] public byte Debounce { get; }
    [ProtoMember(5)] public StandardButtonType Type { get; }

    public override Output Generate(ConfigViewModel model)
    {
        return new ControllerButton(model, Input.Generate(model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, Debounce, Type);
    }
}
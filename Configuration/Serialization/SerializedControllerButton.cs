using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedControllerButton : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input { get; }
    [ProtoMember(2)] public override uint LedOn { get; }
    [ProtoMember(3)] public override uint LedOff { get; }
    [ProtoMember(6)] public override byte? LedIndex { get; }
    [ProtoMember(4)] public byte Debounce { get; }
    [ProtoMember(5)] public StandardButtonType Type { get; }

    public SerializedControllerButton(SerializedInput? input, Color ledOn, Color ledOff, byte? ledIndex, byte debounce, StandardButtonType type)
    {
        Input = input;
        LedOn = ledOn.ToUint32();
        LedOff = ledOff.ToUint32();
        LedIndex = ledIndex;
        Debounce = debounce;
        Type = type;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new ControllerButton(model, Input?.Generate(microcontroller, model), Color.FromUInt32(LedOn), Color.FromUInt32(LedOff), LedIndex, Debounce, Type);
    }
}
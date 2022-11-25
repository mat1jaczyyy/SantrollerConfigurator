using Avalonia.Input;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedKeyboardButton : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input { get; }
    [ProtoMember(2)] public override uint LedOn { get; }
    [ProtoMember(3)] public override uint LedOff { get; }
    
    [ProtoMember(6)] public override byte LedIndex { get; }
    [ProtoMember(4)] public byte Debounce { get; }
    [ProtoMember(5)] public Key Type { get; }

    public SerializedKeyboardButton(SerializedInput? input, Color ledOn, Color ledOff, byte ledIndex, byte debounce, Key type)
    {
        Input = input;
        LedOn = ledOn.ToUint32();
        LedOff = ledOff.ToUint32();
        Debounce = debounce;
        Type = type;
        LedIndex = ledIndex;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new KeyboardButton(model, Input?.Generate(microcontroller, model), Color.FromUInt32(LedOn), Color.FromUInt32(LedOff), LedIndex, Debounce, Type);
    }
}
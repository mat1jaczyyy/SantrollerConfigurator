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
    [ProtoMember(2)] public override Color LedOn { get; }
    [ProtoMember(3)] public override Color LedOff { get; }
    
    [ProtoMember(6)] public override int? LedIndex { get; }
    [ProtoMember(4)] public int Debounce { get; }
    [ProtoMember(5)] public Key Type { get; }

    public SerializedKeyboardButton(SerializedInput? input, Color ledOn, Color ledOff, int? ledIndex, int debounce, Key type)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Debounce = debounce;
        Type = type;
        LedIndex = ledIndex;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new KeyboardButton(model, Input?.Generate(microcontroller), LedOn, LedOff, LedIndex, Debounce, Type);
    }
}
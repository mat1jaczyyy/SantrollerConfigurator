using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMouseButton : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input { get; }
    [ProtoMember(2)] public override Color LedOn { get; }
    [ProtoMember(3)] public override Color LedOff { get; }
    [ProtoMember(4)] public int Debounce { get; }
    [ProtoMember(5)] public MouseButtonType Type { get; }
    [ProtoMember(6)] public override int? LedIndex { get; }

    public SerializedMouseButton(SerializedInput? input, Color ledOn, Color ledOff, int? ledIndex, int debounce, MouseButtonType type)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        LedIndex = ledIndex;
        Debounce = debounce;
        Type = type;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new MouseButton(model, Input?.Generate(microcontroller), LedOn, LedOff, LedIndex, Debounce, Type);
    }
}
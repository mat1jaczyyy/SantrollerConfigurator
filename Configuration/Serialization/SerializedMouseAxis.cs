using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMouseAxis : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input { get; }
    [ProtoMember(2)] public override uint LedOn { get; }
    [ProtoMember(3)] public override uint LedOff { get; }
    [ProtoMember(7)] public override byte? LedIndex { get; }
    [ProtoMember(4)] public float Multiplier { get; }
    [ProtoMember(5)] public int Offset { get; }
    [ProtoMember(6)] public int Deadzone { get; }

    public MouseAxisType Type { get; }

    public SerializedMouseAxis(SerializedInput? input, MouseAxisType type, Color ledOn, Color ledOff, byte? ledIndex, float multiplier, int offset,
        int deadzone)
    {
        Input = input;
        LedOn = ledOn.ToUint32();
        LedOff = ledOff.ToUint32();
        Multiplier = multiplier;
        Offset = offset;
        Deadzone = deadzone;
        Type = type;
        LedIndex = ledIndex;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new MouseAxis(model, Input?.Generate(microcontroller, model), Color.FromUInt32(LedOn), Color.FromUInt32(LedOff), LedIndex, Multiplier, Offset, Deadzone,
            Type);
    }
}
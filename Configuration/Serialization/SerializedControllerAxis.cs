using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedControllerAxis : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input { get; }
    [ProtoMember(2)] public override uint LedOn { get; }
    [ProtoMember(3)] public override uint LedOff { get; }
    [ProtoMember(8)] public override byte LedIndex { get; }
    [ProtoMember(4)] public int Min { get; }
    [ProtoMember(5)] public int Max { get; }
    [ProtoMember(6)] public int Deadzone { get; }

    [ProtoMember(7)] public StandardAxisType Type { get; }

    public SerializedControllerAxis(SerializedInput? input, StandardAxisType type, Color ledOn, Color ledOff, byte ledIndex, int min, int max, int deadzone)
    {
        Input = input;
        LedOn = ledOn.ToUint32();
        LedOff = ledOff.ToUint32();
        Min = min;
        Max = max;
        Deadzone = deadzone;
        Type = type;
        LedIndex = ledIndex;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new ControllerAxis(model, Input?.Generate(microcontroller, model), Color.FromUInt32(LedOn), Color.FromUInt32(LedOff), LedIndex, Min, Max, Deadzone,
            Type);
    }
}
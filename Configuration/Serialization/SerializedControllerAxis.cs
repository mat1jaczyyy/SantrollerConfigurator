using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedControllerAxis : SerializedOutput
{
    public SerializedControllerAxis(SerializedInput input, StandardAxisType type, Color ledOn, Color ledOff,
        byte[] ledIndex, int min, int max, int deadzone)
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

    [ProtoMember(1)] public override SerializedInput Input { get; }
    [ProtoMember(2)] public override uint LedOn { get; }
    [ProtoMember(3)] public override uint LedOff { get; }
    [ProtoMember(8)] public override byte[] LedIndex { get; }
    [ProtoMember(4)] public int Min { get; }
    [ProtoMember(5)] public int Max { get; }
    [ProtoMember(6)] public int Deadzone { get; }

    [ProtoMember(7)] public StandardAxisType Type { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var input = Input.Generate(model);
        return new ControllerAxis(model, input, Color.FromUInt32(LedOn), Color.FromUInt32(LedOff), LedIndex, Min, Max,
            Deadzone,
            Type);
    }
}
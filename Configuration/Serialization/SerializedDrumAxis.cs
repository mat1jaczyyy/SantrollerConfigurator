using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDrumAxis : SerializedOutput
{
    [ProtoMember(1)] public override SerializedInput? Input { get; }
    [ProtoMember(2)] public override uint LedOn { get; }
    [ProtoMember(3)] public override uint LedOff { get; }
    [ProtoMember(4)] public override byte[] LedIndex { get; }
    [ProtoMember(5)] public int Min { get; }
    [ProtoMember(6)] public int Max { get; }
    [ProtoMember(7)] public int Deadzone { get; }
    [ProtoMember(8)] public int Threshold { get; }
    [ProtoMember(9)] public int Debounce { get; }
    [ProtoMember(10)] public DrumAxisType Type { get; }

    public SerializedDrumAxis(SerializedInput? input, DrumAxisType type, Color ledOn, Color ledOff, byte[] ledIndex,
        int min, int max, int deadzone, int threshold, int debounce)
    {
        Input = input;
        LedOn = ledOn.ToUint32();
        LedOff = ledOff.ToUint32();
        Min = min;
        Max = max;
        Deadzone = deadzone;
        Type = type;
        LedIndex = ledIndex;
        Threshold = threshold;
        Debounce = debounce;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new DrumAxis(model, Input?.Generate(microcontroller, model), Color.FromUInt32(LedOn),
            Color.FromUInt32(LedOff), LedIndex, Min, Max, Deadzone,
            Threshold, Debounce, Type);
    }
}
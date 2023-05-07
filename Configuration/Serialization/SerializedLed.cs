using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedLed : SerializedOutput
{
    public SerializedLed(Color ledOn, Color ledOff, byte[] ledIndex, LedCommandType type, int param1, int param2,
        bool outputEnabled, int pin)
    {
        LedOn = ledOn.ToUInt32();
        LedOff = ledOff.ToUInt32();
        LedIndex = ledIndex;
        Type = type;
        OutputEnabled = outputEnabled;
        Pin = pin;
        Param1 = param1;
        Param2 = param2;
    }

    [ProtoMember(1)] public uint LedOn { get; }
    [ProtoMember(2)] public uint LedOff { get; }
    [ProtoMember(3)] public byte[] LedIndex { get; }
    [ProtoMember(4)] public LedCommandType Type { get; }
    [ProtoMember(5)] public bool OutputEnabled { get; }
    [ProtoMember(6)] public int Pin { get; }
    [ProtoMember(7)] public int Param1 { get; }
    [ProtoMember(8)] public int Param2 { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var combined = new Led(model, OutputEnabled, Pin, Color.FromUInt32(LedOn), Color.FromUInt32(LedOff),
            LedIndex, Type, Param1, Param2);
        model.Bindings.Add(combined);
        return combined;
    }
}
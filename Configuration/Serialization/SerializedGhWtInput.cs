using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGhWtInput : SerializedInput
{
    public SerializedGhWtInput(int pin, GhWtInputType type)
    {
        Pin = pin;
        Type = type;
    }

    [ProtoMember(1)] private int Pin { get; }

    [ProtoMember(2)] private GhWtInputType Type { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new GhWtTapInput(Type, model, Pin);
    }
}
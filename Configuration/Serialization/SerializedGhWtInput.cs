using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Neck;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedGhWtInput : SerializedInput
{
    [ProtoMember(1)] private int Pin { get; }

    [ProtoMember(2)] private GhWtInputType Type { get; }

    public SerializedGhWtInput(int pin, GhWtInputType type)
    {
        Pin = pin;
        Type = type;
    }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new GhWtTapInput(Type, model, microcontroller, Pin);
    }
}
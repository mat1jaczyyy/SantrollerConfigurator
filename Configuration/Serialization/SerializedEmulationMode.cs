using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedEmulationMode : SerializedOutput
{
    [ProtoMember(1)] public SerializedInput Input { get; }
    [ProtoMember(2)] public EmulationModeType Type { get; }

    public SerializedEmulationMode(EmulationModeType type, SerializedInput input)
    {
        Input = input;
        Type = type;
    }

    public override Output Generate(ConfigViewModel model)
    {
        return new EmulationMode.EmulationMode(model, Input.Generate(model), Type);
    }
}
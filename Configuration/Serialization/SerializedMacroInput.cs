using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMacroInput : SerializedInput
{
    public SerializedMacroInput(SerializedInput child1, SerializedInput child2)
    {
        Child1 = child1;
        Child2 = child2;
    }

    [ProtoMember(1)] public SerializedInput Child1 { get; }
    [ProtoMember(2)] public SerializedInput Child2 { get; }

    public override Input Generate(ConfigViewModel model)
    {
        return new MacroInput(Child1.Generate(model), Child2.Generate(model), model);
    }
}
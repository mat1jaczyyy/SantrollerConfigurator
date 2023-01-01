using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedMacroInput : SerializedInput
{
    [ProtoMember(1)] public SerializedInput Child1 { get; }
    [ProtoMember(2)] public SerializedInput Child2 { get; }

    public SerializedMacroInput(SerializedInput child1, SerializedInput child2)
    {
        Child1 = child1;
        Child2 = child2;
    }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new MacroInput(Child1.Generate(microcontroller, model), Child2.Generate(microcontroller, model), model);
    }
}
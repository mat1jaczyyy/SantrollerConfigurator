using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedJoystickToDpad : SerializedOutput
{
    public SerializedJoystickToDpad(int threshold)
    {
        Threshold = threshold;
    }
    [ProtoMember(1)] public int Threshold { get; }
    public override Output Generate(ConfigViewModel model)
    {
        return new JoystickToDpad.JoystickToDpad(model, Threshold);
    }
}
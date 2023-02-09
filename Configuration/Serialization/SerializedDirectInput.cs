using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedDirectInput : SerializedInput
{
    [ProtoMember(1)] private int Pin { get; }
    [ProtoMember(2)] private DevicePinMode PinMode { get; }

    public SerializedDirectInput(int pin, DevicePinMode pinMode)
    {
        Pin = pin;
        PinMode = pinMode;
    }

    public override Input Generate(Microcontroller microcontroller, ConfigViewModel model)
    {
        return new DirectInput(Pin, PinMode, model, microcontroller);
    }
}
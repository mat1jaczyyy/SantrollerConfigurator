using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedBluetoothOutput : SerializedOutput
{
    public SerializedBluetoothOutput(string macAddress)
    {
        MacAddress = macAddress;
    }

    [ProtoMember(1)] public string MacAddress { get; }

    public override Output Generate(ConfigViewModel model)
    {
        var output = new BluetoothOutput(model, MacAddress);
        model.Bindings.Add(output);
        return output;
    }
}
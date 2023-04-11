using System;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Types;
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
        return new BluetoothOutput(model, MacAddress);
    }
}
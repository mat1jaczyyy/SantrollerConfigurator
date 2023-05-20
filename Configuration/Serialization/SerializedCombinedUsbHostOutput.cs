using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedCombinedUsbHostOutput : SerializedOutput
{
    public override Output Generate(ConfigViewModel model)
    {
        var combined = new UsbHostCombinedOutput(model);
        model.Bindings.Add(combined);
        return combined;
    }
}
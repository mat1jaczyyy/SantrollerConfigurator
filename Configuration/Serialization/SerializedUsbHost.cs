using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract(SkipConstructor = true)]
public class SerializedUsbHost : SerializedOutput
{
    public override Output Generate(ConfigViewModel model)
    {
        var combined = new UsbHostInput(model);
        model.Bindings.Add(combined);
        return combined;
    }
}
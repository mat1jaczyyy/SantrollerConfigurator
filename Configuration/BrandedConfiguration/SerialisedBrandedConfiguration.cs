using GuitarConfigurator.NetCore.Configuration.Serialization;
using ProtoBuf;
using SantrollerConfiguratorBranded.NetCore;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public class SerialisedBrandedConfiguration
{
    [ProtoMember(1)] public string VendorName;
    [ProtoMember(2)] public string ProductName;
    [ProtoMember(3)] public SerializedConfiguration Configuration;
    [ProtoMember(4)] public Uf2Block[] Uf2 { get; private set; }
    public SerialisedBrandedConfiguration(BrandedConfiguration configuration)
    {
        Configuration = new SerializedConfiguration(configuration.Model);
        Uf2 = configuration.Uf2;
    }
}
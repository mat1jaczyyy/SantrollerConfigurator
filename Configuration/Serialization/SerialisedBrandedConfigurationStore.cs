using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.Serialization;

[ProtoContract]
public class SerialisedBrandedConfigurationStore
{
    public SerialisedBrandedConfigurationStore()
    {
        
    }
    public SerialisedBrandedConfigurationStore(BrandedConfigurationStore store)
    {
        ToolName = store.ToolName;
        HelpText = store.HelpText;;
        Configurations.AddRange(store.Configurations.Select(s => new SerialisedBrandedConfiguration(s)).ToList());
    }

    [ProtoMember(1)] public string ToolName { get; set; } = null!;
    [ProtoMember(3)] public string HelpText { get; set; } = null!;
    [ProtoMember(4)] public List<SerialisedBrandedConfiguration> Configurations { get; set; } = new();
}
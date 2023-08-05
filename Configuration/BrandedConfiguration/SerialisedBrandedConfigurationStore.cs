using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public class SerialisedBrandedConfigurationStore
{
    public SerialisedBrandedConfigurationStore(BrandedConfigurationStore store)
    {
        ToolName = store.ToolName;
        HelpText = store.HelpText;;
        Configurations = store.Configurations.Select(s => new SerialisedBrandedConfiguration(s)).ToList();
    }

    [ProtoMember(1)] public string ToolName { get; private set; }
    [ProtoMember(3)] public string HelpText { get; private set; }
    [ProtoMember(4)] public List<SerialisedBrandedConfiguration> Configurations { get; private set; }
}
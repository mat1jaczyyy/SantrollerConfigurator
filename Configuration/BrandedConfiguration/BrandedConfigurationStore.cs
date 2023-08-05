using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;
public class BrandedConfigurationStore
{
    public BrandedConfigurationStore(string toolName, string vendorName, string helpText, string productName)
    {
        ToolName = toolName;
        HelpText = helpText;
        Configurations = new List<BrandedConfiguration>();
    }
    
    public BrandedConfigurationStore(SerialisedBrandedConfigurationStore store, ConfigViewModel model)
    {
        ToolName = store.ToolName;
        HelpText = store.HelpText;
        Configurations = store.Configurations.Select(s => new BrandedConfiguration(s, model)).ToList();
    }

    public string ToolName { get; private set; }
    public string HelpText { get; private set; }
    public List<BrandedConfiguration> Configurations { get; private set; }

    public static BrandedConfigurationStore LoadFromBranding(ConfigViewModel model)
    {
#if SINGLE_FILE
        var stream = File.OpenRead(Environment.ProcessPath!);
        var reader = new BinaryReader(stream);
        stream.Seek(-sizeof(int), SeekOrigin.End);
        var offset = reader.ReadInt32();
        stream.Seek(offset, SeekOrigin.Begin);
#else
        var path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "branding.bin");
        var stream = File.OpenRead(path);
#endif
        return new BrandedConfigurationStore(Serializer.Deserialize<SerialisedBrandedConfigurationStore>(stream), model);
    }

    public void WriteToExecutable(string baseExecutable, string outputExecutable)
    {
        File.Copy(baseExecutable, outputExecutable);
#if SINGLE_FILE
        var size = new FileInfo(baseExecutable).Length;
        using var stream = File.Open(outputExecutable, FileMode.Append);
        using var binaryWrite = new BinaryWriter(stream);
        Serializer.Serialize(stream, this);
        binaryWrite.Write((int)size);
#else
        var path = Path.Combine(Path.GetDirectoryName(outputExecutable)!, "branding.bin");
        using var stream = File.OpenWrite(path);
        Serializer.Serialize(stream, new SerialisedBrandedConfigurationStore(this));
#endif
    }
}
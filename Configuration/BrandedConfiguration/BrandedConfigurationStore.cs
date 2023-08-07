using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public class BrandedConfigurationStore : ReactiveObject
{
    public BrandedConfigurationStore(string toolName, string helpText)
    {
        ToolName = toolName;
        HelpText = helpText;
    }

    public BrandedConfigurationStore(SerialisedBrandedConfigurationStore store, bool branded,
        MainWindowViewModel screen)
    {
        ToolName = store.ToolName;
        HelpText = store.HelpText;
        Configurations.AddRange(store.Configurations.Select(s => new BrandedConfiguration(s, branded, screen)));
    }

    [Reactive]
    public string ToolName { get; set; }
    
    [Reactive]
    public string HelpText { get; set; }
    public ObservableCollection<BrandedConfiguration> Configurations { get; } = new();

    public static BrandedConfigurationStore LoadBranding(MainWindowViewModel model)
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
        return new BrandedConfigurationStore(Serializer.Deserialize<SerialisedBrandedConfigurationStore>(stream), true,
            model);
    }

    public void WriteBranding(string baseExecutable, string outputExecutable)
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
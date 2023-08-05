using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.ViewModels;
using ProtoBuf;
using SantrollerConfiguratorBranded.NetCore;

namespace GuitarConfigurator.NetCore.Configuration.BrandedConfiguration;

public class BrandedConfiguration
{
    private const uint BlobOffset = 0x10282000;
    private const uint ConfigOffset = 0x10283000;
    public string VendorName { get; private set; }
    public string ProductName { get; private set; }
    public Uf2Block[] Uf2 { get; private set; }
    public ConfigViewModel Model { get; }
    public BrandedConfiguration(SerialisedBrandedConfiguration configuration, ConfigViewModel model)
    {
        Model = model;
        configuration.Configuration.LoadConfiguration(Model);
        VendorName = configuration.VendorName;
        ProductName = configuration.ProductName;
        Uf2 = configuration.Uf2;
    }

    public string BuildConfig()
    {
        using var streamBlob = new MemoryStream();
        return $"""
                #define DEVICE_VENDOR {string.Join(", ", VendorName.Select(s => $"'{s}'"))}
                #define DEVICE_PRODUCT {string.Join(", ", ProductName.Select(s => $"'{s}'"))}
                {Model.Generate(streamBlob)}
                """;
    }

    public void BuildUf2(string outputFile)
    {
        var blocks = new List<Uf2Block>(Uf2);
        using var stream = File.OpenWrite(outputFile);
        using (var outputStream = new MemoryStream())
        {
            using (var compressStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
            {
                Serializer.Serialize(compressStream, new SerializedConfiguration(Model));
            }
            // Split the config into 256 byte blocks, as thats the standard size
            var block = Uf2.Last();
            block.targetAddr = ConfigOffset;
            while (true)
            {
                block.blockNo++;
                Array.Fill(block.data, (byte)0);
                block.payloadSize = (uint) outputStream.Read(block.data, 0, (int) block.payloadSize);
                if (outputStream.Read(block.data, 0, 256) == 0)
                {
                    break;
                }
                blocks.Add(block);
                block.targetAddr += block.payloadSize;
            }
        }
        
        foreach (var uf2Block in blocks)
        {
            uf2Block.numBlocks = (uint) blocks.Count;
            if (uf2Block.targetAddr == BlobOffset)
            {
                using var streamBlob = new MemoryStream();
                Model.Generate(streamBlob);
                streamBlob.Seek(0, SeekOrigin.Begin);
                streamBlob.Write(uf2Block.data);
            }
            StructTools.RawSerialise(uf2Block, stream);
        }
        
    }
}
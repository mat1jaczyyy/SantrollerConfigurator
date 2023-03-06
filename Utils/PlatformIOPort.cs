using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GuitarConfigurator.NetCore.Utils;

public partial class PlatformIoPort
{
    private static readonly string VidPidPattern = "VID:PID=(\\w{4}):(\\w{4})";
    public string Port { get; set; } = "";

    public string Description { get; set; } = "";

    public string Hwid { get; set; } = "";

    public uint Vid
    {
        get
        {
            var reg = Regex.Match(Hwid, VidPidPattern);
            return reg.Success ? Convert.ToUInt32(reg.Groups[1].Value, 16) : 0;
        }
    }

    public uint Pid
    {
        get
        {
            var reg = Regex.Match(Hwid, VidPidPattern);
            return reg.Success ? Convert.ToUInt32(reg.Groups[2].Value, 16) : 0;
        }
    }
}

public partial class PlatformIoPort
{
    public static PlatformIoPort[] FromJson(string json)
    {
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            WriteIndented = true,
            TypeInfoResolver = SourceGenerationContext.Default
        };
#pragma warning disable IL2026
        return JsonSerializer.Deserialize<PlatformIoPort[]>(json, serializeOptions)!;
#pragma warning restore IL2026
    }
}
[JsonSerializable(typeof(PlatformIoPort[]))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GuitarConfiguratorSharp.NetCore.Utils
{
    public partial class PlatformIoPort
    {
        private static readonly string VidPidPattern = "VID:PID=(\\w{4}):(\\w{4})";
        public string Port { get; set; } = "";

        public string Description { get; set; } = "";

        public string Hwid { get; set; } = "";

        public uint Vid => Convert.ToUInt32(Regex.Match(Hwid, VidPidPattern).Groups[1].Value, 16);
        public uint Pid => Convert.ToUInt32(Regex.Match(Hwid, VidPidPattern).Groups[2].Value, 16);
    }

    public partial class PlatformIoPort
    {
        public static PlatformIoPort[] FromJson(string json)
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
                WriteIndented = true
            };
            return JsonSerializer.Deserialize<PlatformIoPort[]>(json, serializeOptions)!;
        }
    }
}

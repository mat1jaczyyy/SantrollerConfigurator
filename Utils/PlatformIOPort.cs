namespace GuitarConfiguratorSharp.Utils
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    public partial class PlatformIOPort
    {
        private static readonly string VID_PID_PATTERN = "VID:PID=(\\w{4}):(\\w{4})";
        public string Port { get; set; }

        public string Description { get; set; }

        public string Hwid { get; set; } = "";

        public uint Vid => Convert.ToUInt32(Regex.Match(Hwid, VID_PID_PATTERN).Groups[1].Value, 16);
        public uint Pid => Convert.ToUInt32(Regex.Match(Hwid, VID_PID_PATTERN).Groups[2].Value, 16);
    }

    public partial class PlatformIOPort
    {
        public static PlatformIOPort[] FromJson(string json)
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
                WriteIndented = true
            };
            return JsonSerializer.Deserialize<PlatformIOPort[]>(json, serializeOptions)!;
        }
    }
}

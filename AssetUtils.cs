using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;

namespace GuitarConfiguratorSharp.NetCore;

public class AssetUtils
{
    public static async Task ExtractFile(string file, string location)
    {
        using (var f = File.OpenWrite(location))
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
            var uri = new Uri($"avares://{assemblyName}/Assets/{file}");
            using (var target = assets!.Open(uri))
            {
                await target.CopyToAsync(f).ConfigureAwait(false);
            }
        }
    }

    public static async Task ExtractZip(string zip, string zipLocation, string location)
    {
        await ExtractFile(zip, zipLocation);
        ZipFile.ExtractToDirectory(zipLocation, location);
        File.Delete(zipLocation);
    }

    public static string GetAppDataFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(folder, "SantrollerConfigurator");
    }
}
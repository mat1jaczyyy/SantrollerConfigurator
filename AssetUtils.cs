using System;
using Avalonia;
using Avalonia.Platform;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

public class AssetUtils
{
    public static async Task ExtractFile(string file, string location)
    {
        using (var f = System.IO.File.OpenWrite(location))
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            string assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
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
        System.IO.File.Delete(zipLocation);
    }

    public static string GetAppDataFolder()
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(folder, "SantrollerConfigurator");
    }
}
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Joveler.Compression.XZ;
using System.Formats.Tar;

namespace GuitarConfigurator.NetCore;

public class AssetUtils
{
    public static async Task ExtractFileAsync(string file, string location)
    {
        await using var f = File.OpenWrite(location);
        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/{file}");
        await using var target = assets!.Open(uri);
        await target.CopyToAsync(f).ConfigureAwait(false);
    }

    public static void InitNativeLibrary()
    {
        XZInit.GlobalInit();
    }

    public static async Task ExtractXzAsync(string archiveFile, string location)
    {
        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/{archiveFile}");
        await using var target = assets!.Open(uri);
        var decompOpts = new XZDecompressOptions();
        var opts = new XZThreadedDecompressOptions();
        opts.Threads = Environment.ProcessorCount;
        await using XZStream zs = new XZStream(target, decompOpts, opts);
        await TarFile.ExtractToDirectoryAsync(zs, location, true);
    }

    public static string GetAppDataFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(folder, "SantrollerConfigurator");

        return path;
    }
}
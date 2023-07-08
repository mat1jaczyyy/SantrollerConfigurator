using System;
using System.Formats.Tar;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Joveler.Compression.XZ;

namespace GuitarConfigurator.NetCore;

public class AssetUtils
{
    public delegate void ExtractionProgress(float progress);

    public static async Task ExtractFileAsync(string file, string location)
    {
        await using var f = File.OpenWrite(location);
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/{file}");
        await using var target = AssetLoader.Open(uri);
        await target.CopyToAsync(f).ConfigureAwait(false);
    }

    public static void InitNativeLibrary()
    {
        string lib;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            lib = "liblzma.so.5";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            lib = "liblzma.dylib";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            lib = "liblzma.dll";
        else lib = "";
        XZInit.GlobalInit(lib);
    }

    public static async Task<string> ReadFileAsync(string file)
    {
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/{file}");
        await using var target = AssetLoader.Open(uri);
        var reader = new StreamReader(target);
        return await reader.ReadToEndAsync();
    }

    private static async Task GetProgress(XZStream stream, IAsyncResult extract, ExtractionProgress extractionProgress)
    {
        float total = stream.BaseStream.Length;
        while (!extract.IsCompleted)
        {
            stream.GetProgress(out var progressIn, out _);
            var progress = progressIn / total - 0.01f;
            extractionProgress(progress);
            await Task.Delay(100);
        }
    }

    public static async Task ExtractXzAsync(string archiveFile, string location, ExtractionProgress extractionProgress)
    {
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        var uri = new Uri($"avares://{assemblyName}/Assets/{archiveFile}");
        await using var target = AssetLoader.Open(uri);
        var decompOpts = new XZDecompressOptions();
        var opts = new XZThreadedDecompressOptions
        {
            Threads = Environment.ProcessorCount
        };
        await using var zs = new XZStream(target, decompOpts, opts);
        var task = TarFile.ExtractToDirectoryAsync(zs, location, true);
        await GetProgress(zs, task, extractionProgress);
    }

    public static string GetAppDataFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            folder = "/Users/Shared/Library/Application Support";
        }
        var path = Path.Combine(folder, "SantrollerConfigurator");

        return path;
    }
}
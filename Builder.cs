using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;


namespace GuitarConfigurator.NetCore;

public class Builder : Microsoft.Build.Utilities.Task
{
    private const string PlatformIoVersion = "6.1.5";
    public string Parameter1 { get; set; } = null!;
    public string Parameter2 { get; set; } = null!;

    private const bool ForceBuild = false;

    private async Task<bool> ExecuteAsync()
    {
        var appdataFolder = Path.Combine(Parameter1, "SantrollerConfigurator");
        if (!File.Exists(appdataFolder)) Directory.CreateDirectory(appdataFolder);
        
        var pioFolder = Path.Combine(appdataFolder, "platformio");
        if (!File.Exists(appdataFolder)) Directory.CreateDirectory(pioFolder);

        // Copy firmware folder
        var originalFirmwareDir = Path.Combine(Parameter2, "firmware");
        var firmwareDir = Path.Combine(appdataFolder, "firmware");
        new DirectoryInfo(originalFirmwareDir).DeepCopy(firmwareDir);

        // Download python
        var pythonFolder = Path.Combine(appdataFolder, "python");
        
        var pythonExecutable = Path.Combine(pythonFolder, "bin", "python3");
        var pioExecutable = Path.Combine(pythonFolder, "bin", "platformio");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            pythonExecutable = Path.Combine(pythonFolder, "python.exe");
            pioExecutable = Path.Combine(pythonFolder, "Scripts", "platformio.exe");
        }

        if (!File.Exists(pythonExecutable))
        {
            if (!File.Exists(pythonFolder)) Directory.CreateDirectory(pythonFolder);
            var pythonLoc = Path.Combine(pythonFolder, "python.tar.gz");
            var arch = GetPackageArch();
            var package =
                $"https://github.com/indygreg/python-build-standalone/releases/download/20230116/cpython-3.11.1+20230116-{arch}-install_only.tar.gz";
            using var download = new HttpClientDownloadWithProgress(package, pythonLoc);
            download.ProgressChanged += (_, _, percentage) => Log.LogMessage($"Downloading python {percentage}");
            await download.StartDownloadAsync().ConfigureAwait(false);

            // Extract python with tar, as there is no built in tar functions in csharp
            var tarProcess = new Process();
            tarProcess.StartInfo.WorkingDirectory = appdataFolder;
            tarProcess.StartInfo.FileName = "tar";
            tarProcess.StartInfo.Arguments = $"-xvzf {pythonLoc}";
            tarProcess.StartInfo.UseShellExecute = false;
            tarProcess.StartInfo.RedirectStandardOutput = true;
            tarProcess.StartInfo.RedirectStandardError = true;
            tarProcess.StartInfo.CreateNoWindow = true;
            Log.LogMessage("Extracting python");
            tarProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Log.LogMessage(e.Data);
                }
            };
            tarProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Log.LogMessage(e.Data);
                }
            };
            tarProcess.Start();
            tarProcess.BeginOutputReadLine();
            tarProcess.BeginErrorReadLine();
            tarProcess.WaitForExit();

            File.Delete(pythonLoc);
        }

        if (!File.Exists(pioExecutable))
        {
            var installerProcess = new Process();
            installerProcess.StartInfo.FileName = pythonExecutable;
            installerProcess.StartInfo.Arguments = $"-m pip install platformio=={PlatformIoVersion}";
            installerProcess.StartInfo.UseShellExecute = false;
            installerProcess.StartInfo.RedirectStandardOutput = true;
            installerProcess.StartInfo.RedirectStandardError = true;
            installerProcess.StartInfo.CreateNoWindow = true;
            installerProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Log.LogMessage(e.Data);
                }
            };
            installerProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Log.LogMessage(e.Data);
                }
            };
            installerProcess.Start();
            installerProcess.BeginOutputReadLine();
            installerProcess.BeginErrorReadLine();
            installerProcess.WaitForExit();
        }

        if (!File.Exists(Path.Combine(Parameter2,"Assets","platformio.tar.xz")) || ForceBuild)
        {
            // Install pio packages
            var pioProcess = new Process();
            pioProcess.StartInfo.FileName = pythonExecutable;
            pioProcess.StartInfo.WorkingDirectory = firmwareDir;
            pioProcess.StartInfo.EnvironmentVariables["PLATFORMIO_CORE_DIR"] = pioFolder;
            pioProcess.StartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
            pioProcess.StartInfo.Arguments = $"-m platformio pkg install";
            pioProcess.StartInfo.UseShellExecute = false;
            pioProcess.StartInfo.RedirectStandardOutput = true;
            pioProcess.StartInfo.RedirectStandardError = true;
            pioProcess.StartInfo.CreateNoWindow = true;
            pioProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Log.LogMessage(e.Data);
                }
            };
            pioProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Log.LogMessage(e.Data);
                }
            };
            pioProcess.Start();
            pioProcess.BeginOutputReadLine();
            pioProcess.BeginErrorReadLine();
            pioProcess.WaitForExit();

            // Now that we have packages downloaded, remove the cache, remove piolibs and download again. This will get us a .cache directory with only packages

            Directory.Delete(Path.Combine(pioFolder, ".cache"), true);
            Directory.Delete(Path.Combine(firmwareDir, ".pio"), true);
            pioProcess.Start();
            pioProcess.WaitForExit();
            // Drop some unused esp32 sdks
            //platformio/packages/framework-arduinoespressif32/tools/sdk/esp32c3
            foreach (var dir in new[] {"esp32s3", "esp32s2", "esp32c3"})
            {
                var path = Path.Combine(pioFolder, "packages", "framework-arduinoespressif32", "tools", "sdk", dir);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }

            // Drop some unused code from the pico bluetooth stack
            var path2 = Path.Combine(pioFolder, "packages", "framework-arduinopico", "lib", "btstack", "port");
            if (Directory.Exists(path2))
            {
                Directory.Delete(path2, true);
            }
            Directory.Delete(Path.Combine(firmwareDir, ".pio"), true);

            Compress("firmware.tar.xz", firmwareDir);
            Compress("python.tar.xz", pythonFolder);
            Compress("platformio.tar.xz", pioFolder);
        }

        return true;
    }

    private void Compress(string archive, string path)
    {
        var s7ZProcess = new Process();
        s7ZProcess.StartInfo.FileName = "tar";
        s7ZProcess.StartInfo.WorkingDirectory = Directory.GetParent(path)!.ToString();
        s7ZProcess.StartInfo.Arguments = $"cfvJ {Path.Combine(Parameter2,"Assets",archive)} {Path.GetFileName(path)}";
        s7ZProcess.StartInfo.EnvironmentVariables["XZ_OPT"] = "-T0 -9";
        s7ZProcess.StartInfo.UseShellExecute = false;
        s7ZProcess.StartInfo.RedirectStandardOutput = true;
        s7ZProcess.StartInfo.RedirectStandardError = true;
        s7ZProcess.StartInfo.CreateNoWindow = true;
        s7ZProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                Log.LogMessage(e.Data);
            }
        };
        s7ZProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                Log.LogMessage(e.Data);
            }
        };
        s7ZProcess.Start();
        s7ZProcess.BeginOutputReadLine();
        s7ZProcess.BeginErrorReadLine();
        s7ZProcess.WaitForExit();
    }

    public override bool Execute()
    {
#pragma warning disable VSTHRD002
        return ExecuteAsync().Result;
#pragma warning restore VSTHRD002
    }

    private static string GetPackageArch()
    {
        var arch = "unknown";
        switch (RuntimeInformation.OSArchitecture)
        {
            case Architecture.X86:
                arch = "i686";
                break;
            case Architecture.X64:
                arch = "x86_64";
                break;
            case Architecture.Arm:
                arch = "armv6l";
                break;
            case Architecture.Arm64:
                arch = "aarch64";
                break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"{arch}-pc-windows-msvc-shared";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return $"{arch}-apple-darwin";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return $"{arch}-unknown-linux-gnu";

        return "unsupported";
    }
}

public static class DirectoryInfoExtensions
{
    public static void DeepCopy(this DirectoryInfo directory, string destinationDir)
    {
        foreach (string dir in Directory.GetDirectories(directory.FullName, "*", SearchOption.AllDirectories))
        {
            string dirToCreate = dir.Replace(directory.FullName, destinationDir);
            Directory.CreateDirectory(dirToCreate);
        }

        foreach (string newPath in Directory.GetFiles(directory.FullName, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(directory.FullName, destinationDir), true);
        }
    }
}

public class HttpClientDownloadWithProgress : IDisposable
{
    public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded,
        double? progressPercentage);

    private readonly string _destinationFilePath;
    private readonly string _downloadUrl;

    private readonly HttpClient _httpClient;

    public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
    {
        _downloadUrl = downloadUrl;
        _destinationFilePath = destinationFilePath;
        _httpClient = new HttpClient {Timeout = TimeSpan.FromDays(1)};
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
#nullable enable
    public event ProgressChangedHandler? ProgressChanged;
#nullable restore
    public async Task StartDownloadAsync()
    {
        using var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead)
            .ConfigureAwait(false);
        await DownloadFileFromHttpResponseMessageAsync(response).ConfigureAwait(false);
    }

    private async Task DownloadFileFromHttpResponseMessageAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        await ProcessContentStreamAsync(totalBytes, contentStream).ConfigureAwait(false);
    }

    private async Task ProcessContentStreamAsync(long? totalDownloadSize, Stream contentStream)
    {
        var totalBytesRead = 0L;
        var readCount = 0L;
        var buffer = new byte[8192];
        var isMoreToRead = true;
        using var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write,
            FileShare.None,
            8192, true);
        do
        {
            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                isMoreToRead = false;
                TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                continue;
            }

            await fileStream.WriteAsync(buffer, 0, bytesRead);

            totalBytesRead += bytesRead;
            readCount += 1;

            if (readCount % 100 == 0)
                TriggerProgressChanged(totalDownloadSize, totalBytesRead);
        } while (isMoreToRead);
    }

    private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
    {
        if (ProgressChanged == null)
            return;

        double? progressPercentage = null;
        if (totalDownloadSize.HasValue)
            progressPercentage = Math.Round((double) totalBytesRead / totalDownloadSize.Value * 100, 2);

        ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
    }
}
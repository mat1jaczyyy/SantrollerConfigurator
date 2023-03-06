using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.Utils;

namespace GuitarConfigurator.NetCore;

public class PlatformIo
{
    //TODO: probably have a nice script to update this, but for now: ` pio pkg list | grep "@"|cut -f1 -d"(" |cut -c 11- | sort -u | wc -l`
    private const int PackageCount = 17;

    private readonly string _pythonExecutable;

    private readonly Process _portProcess;

    public PlatformIo()
    {
        var appdataFolder = AssetUtils.GetAppDataFolder();
        if (!File.Exists(appdataFolder)) Directory.CreateDirectory(appdataFolder);

        var pioFolder = Path.Combine(appdataFolder, "platformio");
        _pythonExecutable = Path.Combine(appdataFolder, "python", "bin", "python3");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _pythonExecutable = Path.Combine(appdataFolder, "python", "python.exe");

        FirmwareDir = Path.Combine(appdataFolder, "firmware");

        _portProcess = new Process();
        _portProcess.EnableRaisingEvents = true;
        _portProcess.StartInfo.FileName = _pythonExecutable;
        _portProcess.StartInfo.WorkingDirectory = FirmwareDir;
        _portProcess.StartInfo.EnvironmentVariables["PLATFORMIO_CORE_DIR"] = pioFolder;

        _portProcess.StartInfo.Arguments = "-m platformio device list --json-output";

        _portProcess.StartInfo.UseShellExecute = false;
        _portProcess.StartInfo.RedirectStandardOutput = true;
        _portProcess.StartInfo.RedirectStandardError = true;
        _portProcess.StartInfo.CreateNoWindow = true;
    }

    public string FirmwareDir { get; }

    private async Task InitialisePlatformIoAsync(BehaviorSubject<PlatformIoState> platformIoOutput)
    {
        // On startup, reinstall the firmware, this will make sure that an update goes out, and also makes sure that the firmware is clean.

        var appdataFolder = AssetUtils.GetAppDataFolder();
        if (Directory.Exists(FirmwareDir)) Directory.Delete(FirmwareDir, true);
        platformIoOutput.OnNext(new PlatformIoState(0, "Extracting Firmware", ""));
        await AssetUtils.ExtractXzAsync("firmware.tar.xz", appdataFolder);

        var pythonDir = Path.Combine(appdataFolder, "python");
        if (Directory.Exists(pythonDir)) Directory.Delete(pythonDir, true);
        platformIoOutput.OnNext(new PlatformIoState(30, "Extracting Python", ""));
        await AssetUtils.ExtractXzAsync("python.tar.xz", appdataFolder);

        var platformIoDir = Path.Combine(appdataFolder, "platformio");
        if (Directory.Exists(platformIoDir)) Directory.Delete(platformIoDir, true);
        platformIoOutput.OnNext(new PlatformIoState(60, "Extracting Platform.IO", ""));
        await AssetUtils.ExtractXzAsync("platformio.tar.xz", appdataFolder);
        
        await File.WriteAllTextAsync(Path.Combine(FirmwareDir, "platformio.ini"),
            (await File.ReadAllTextAsync(Path.Combine(FirmwareDir, "platformio.ini"))).Replace(
                "post:ardwiino_script_post.py",
                "post:ardwiino_script_post_tool.py")).ConfigureAwait(false);
        var task = RunPlatformIo(null, new[] {"pkg", "install"},
            "Installing packages (This may take a while)",
            60, 90, null);
        task.Subscribe(platformIoOutput.OnNext);
        await task.ToTask();
        task = RunPlatformIo(null, new[] {"system", "prune", "-f"},
            "Cleaning up", 90,
            90, null);
        task.Subscribe(platformIoOutput.OnNext);
        await task.ToTask();
        platformIoOutput.OnCompleted();
    }

    public IObservable<PlatformIoState> InitialisePlatformIo()
    {
        var platformIoOutput =
            new BehaviorSubject<PlatformIoState>(new PlatformIoState(0, "Setting up", null));
        _ = InitialisePlatformIoAsync(platformIoOutput).ConfigureAwait(false);
        return platformIoOutput;
    }

    public async Task<PlatformIoPort[]?> GetPortsAsync()
    {
        _portProcess.Start();
        var output = await _portProcess.StandardOutput.ReadToEndAsync();
        await _portProcess.WaitForExitAsync();
        return output != "" ? PlatformIoPort.FromJson(output) : null;
    }

    public BehaviorSubject<PlatformIoState> RunPlatformIo(string? environment, string[] command,
        string progressMessage,
        double progressStartingPercentage, double progressEndingPercentage,
        IConfigurableDevice? device)
    {
        var platformIoOutput =
            new BehaviorSubject<PlatformIoState>(new PlatformIoState(progressStartingPercentage, progressMessage,
                null));

        async Task Process()
        {
            var percentageStep = (progressEndingPercentage - progressStartingPercentage) / PackageCount;
            var currentProgress = progressStartingPercentage;
            var updating = environment == null && command is [_, "install"];
            var uploading = environment != null && command.Length > 1;
            var appdataFolder = AssetUtils.GetAppDataFolder();
            var pioFolder = Path.Combine(appdataFolder, "platformio");
            var process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo.FileName = _pythonExecutable;
            process.StartInfo.WorkingDirectory = FirmwareDir;
            process.StartInfo.EnvironmentVariables["PLATFORMIO_CORE_DIR"] = pioFolder;
            process.StartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
            process.StartInfo.CreateNoWindow = true;
            var args = new List<string>(command);
            args.Insert(0, "-m");
            args.Insert(1, "platformio");
            var sections = 5;
            var isUsb = false;
            if (environment != null)
            {
                percentageStep = progressEndingPercentage - progressStartingPercentage;
                if (device is Arduino) sections = 10;

                if (environment.EndsWith("_usb"))
                {
                    platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                        $"{progressMessage} - Looking for device", null));
                    currentProgress += percentageStep / sections;
                    if (device != null) isUsb = true;

                    sections = 10;
                }

                args.Add("--environment");
                args.Add(environment);
                if (uploading && !isUsb)
                {
                    if (environment.Contains("pico"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            $"{progressMessage} - Looking for device", null));
                        currentProgress += percentageStep / sections;
                        sections = 4;
                    }

                    if (device != null)
                    {
                        var port = await device.GetUploadPortAsync().ConfigureAwait(false);
                        if (port != null)
                        {
                            args.Add("--upload-port");
                            args.Add(port);
                        }
                    }
                }
            }

            //Some pio stuff uses Standard Output, some uses Standard Error, its easier to just flatten both of those to a single stream
            process.StartInfo.Arguments =
                $"-c \"import subprocess;subprocess.run([{string.Join(",", args.Select(s => $"'{s}'"))}],stderr=subprocess.STDOUT)\"";

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            var state = 0;
            process.Start();

            // process.BeginOutputReadLine();
            // process.BeginErrorReadLine();
            var buffer = new char[1];
            var hasError = false;
            var main = sections == 5;
            var uploadPackage = "";
            var uploadCount = 11;
            var seen = new List<string>();
            while (!process.HasExited)
                if (state == 0)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    Console.WriteLine(line);
                    if (string.IsNullOrEmpty(line))
                    {
                        await Task.Delay(1);
                        continue;
                    }

                    platformIoOutput.OnNext(platformIoOutput.Value.WithLog(line));
                    if (updating)
                    {
                        var matches = Regex.Matches(line, @".+: Installing (.+)");
                        if (matches.Count > 0)
                        {
                            uploadPackage = matches[0].Groups[1].Value;
                            if (seen.Contains(uploadPackage)) continue;
                            seen.Add(uploadPackage);
                            uploadCount = 10;
                            state = 5;
                        }
                    }

                    if (uploading)
                    {
                        var matches = Regex.Matches(line, @"Processing (.+?) \(.+\)");
                        if (matches.Count > 0)
                        {
                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                $"{progressMessage} - Building", null));
                            currentProgress += percentageStep / sections;
                        }

                        if (line.StartsWith("Detecting microcontroller type"))
                            if (device is Santroller)
                                device.Bootloader();

                        if (line.StartsWith("Looking for upload port..."))
                        {
                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                $"{progressMessage} - Looking for port", null));
                            currentProgress += percentageStep / sections;


                            if (device is Santroller or Ardwiino && !isUsb) device.Bootloader();
                        }

                        if (line.Contains("SUCCESS"))
                            if (device is PicoDevice || sections == 5)
                                break;
                    }

                    if (line.Contains("AVR device initialized and ready to accept instructions"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            $"{progressMessage} - Reading Settings", null));
                        state = 1;
                    }

                    if (line.Contains("writing flash"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            $"{progressMessage} - Uploading", null));
                        state = 2;
                    }

                    if (line.Contains("rp2040load"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            $"{progressMessage} - Uploading", null));
                        ;
                    }

                    if (line.Contains("Loading into Flash:"))
                    {
                        var done = line.Count(s => s == '=') / 30.0;
                        platformIoOutput.OnNext(new PlatformIoState(
                            currentProgress + percentageStep / sections * done,
                            $"{progressMessage} - Uploading", null));
                    }

                    if (line.Contains("reading on-chip flash data"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            $"{progressMessage} - Verifying", null));
                        state = 3;
                    }

                    if (line.Contains("avrdude done.  Thank you."))
                    {
                        if (!main)
                        {
                            main = true;
                            continue;
                        }

                        break;
                    }

                    if (line.Contains("FAILED"))
                    {
                        platformIoOutput.OnError(new Exception("{progressMessage} - Error"));
                        hasError = true;
                        break;
                    }
                }
                else
                {
                    while (await process.StandardOutput.ReadAsync(buffer, 0, 1) > 0)
                    {
                        // process character...for example:
                        if (buffer[0] == '#') currentProgress += percentageStep / 50 / sections;

                        if (buffer[0] == 's')
                        {
                            state = 0;
                            break;
                        }

                        switch (state)
                        {
                            case 1:
                                platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                    $"{progressMessage} - Reading Settings", null));
                                break;
                            case 2:
                                platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                    $"{progressMessage} - Uploading", null));
                                break;
                            case 3:
                                platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                    $"{progressMessage} - Verifying", null));
                                break;
                            case 5:
                                if (buffer[0] == '%')
                                {
                                    uploadCount--;
                                    currentProgress += percentageStep / 11;
                                }

                                if (buffer[0] == '\n')
                                {
                                    // If a file is downloaded fast, it doesn't hit 100
                                    if (uploadCount > 0) currentProgress += percentageStep / 11 * uploadCount;

                                    state = 0;
                                }

                                platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                    $"{progressMessage} - {uploadPackage}", null));
                                break;
                        }

                        if (state == 0) break;
                    }
                }

            await process.WaitForExitAsync();

            if (!hasError)
            {
                if (uploading)
                {
                    currentProgress = progressEndingPercentage;
                    platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                        $"{progressMessage} - Waiting for Device", null));
                }

                platformIoOutput.OnCompleted();
            }
        }

        _ = Process();
        return platformIoOutput;
    }

    private string[] GetPythonExecutables()
    {
        var executables = new[] {"python3", "python", Path.Combine("bin", "python3.10")};
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            executables = new[] {"python.exe", Path.Combine("Scripts", "python.exe")};

        return executables;
    }


    private string? GetFullPath(string fileName)
    {
        if (File.Exists(fileName))
            return Path.GetFullPath(fileName);

        var values = Environment.GetEnvironmentVariable("PATH")!;
        foreach (var path in values.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    public record PlatformIoState(double Percentage, string Message, string? Log)
    {
        public PlatformIoState WithLog(string log)
        {
            return this with {Log = log};
        }
    }
}
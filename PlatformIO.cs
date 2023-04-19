using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.Utils;

namespace GuitarConfigurator.NetCore;

public class PlatformIo
{
    private readonly string _pythonExecutable;

    private readonly Process _portProcess;
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public PlatformIo()
    {
        var appdataFolder = AssetUtils.GetAppDataFolder();
        if (!File.Exists(appdataFolder)) Directory.CreateDirectory(appdataFolder);

        var pioFolder = Path.Combine(appdataFolder, "platformio");
        _pythonExecutable = Path.Combine(appdataFolder, "python", "bin", "python3");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _pythonExecutable = Path.Combine(appdataFolder, "python", "python.exe");

        FirmwareDir = Path.Combine(appdataFolder, "Ardwiino");

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

    private async Task InitialisePlatformIoAsync(IObserver<PlatformIoState> platformIoOutput)
    {
        var appdataFolder = AssetUtils.GetAppDataFolder();
        if (Directory.Exists(FirmwareDir))
        {
            Directory.Delete(FirmwareDir, true);
        }
        else
        {
            // If the firmware has not been extracted, make sure the user has enough free space for it.
            var matching = 0;
            long free = 0;
            var info = DriveInfo.GetDrives().First();
            foreach (var driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.RootDirectory.FullName.Length <= matching ||
                    !Path.GetFullPath(FirmwareDir).StartsWith(driveInfo.RootDirectory.FullName)) continue;
                matching = driveInfo.RootDirectory.FullName.Length;
                free = driveInfo.AvailableFreeSpace;
                info = driveInfo;
            }

            free = free / 1024 / 1024 / 1024;
            if (free < 3)
            {
                platformIoOutput.OnError(new Exception(
                    $"Not enough free space, you need 3GB of space free on your {info.Name} drive to use this program"));
                return;
            }
        }

        platformIoOutput.OnNext(new PlatformIoState(0, "Extracting Firmware", ""));
        await AssetUtils.ExtractXzAsync("firmware.tar.xz", appdataFolder);

        var pythonDir = Path.Combine(appdataFolder, "python");
        var platformIoDir = Path.Combine(appdataFolder, "platformio");
        var platformIoVersion = Path.Combine(appdataFolder, "platformio.version");
        if (Directory.Exists(platformIoDir))
        {
            var outdated = true;
            if (File.Exists(platformIoVersion))
            {
                outdated = await File.ReadAllTextAsync(platformIoVersion) !=
                           await AssetUtils.ReadFileAsync("platformio.version");
            }

            if (outdated)
            {
                Directory.Delete(platformIoDir, true);
                Directory.Delete(pythonDir, true);
            }
        }

        if (!Directory.Exists(platformIoDir))
        {
            platformIoOutput.OnNext(new PlatformIoState(60, "Extracting Platform.IO", ""));
            await AssetUtils.ExtractXzAsync("platformio.tar.xz", appdataFolder);

            await AssetUtils.ExtractFileAsync("platformio.version", platformIoVersion);
        }

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

    public BehaviorSubject<PlatformIoState> RunPlatformIo(string environment, string[] command,
        string progressMessage,
        double progressStartingPercentage, double progressEndingPercentage,
        IConfigurableDevice? device)
    {
        var platformIoOutput =
            new BehaviorSubject<PlatformIoState>(new PlatformIoState(progressStartingPercentage, progressMessage,
                null));

        async Task Process()
        {
            await _semaphore.WaitAsync();
            var percentageStep = (progressEndingPercentage - progressStartingPercentage);
            var currentProgress = progressStartingPercentage;
            var uploading = command.Length > 1;
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
            args.Insert(0, _pythonExecutable);
            args.Insert(1, "-m");
            args.Insert(2, "platformio");
            Console.WriteLine(string.Join(", ", args));
            var sections = 5;
            var isUsb = false;

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
                    Console.WriteLine("Detecting port please wait");
                    var port = await device.GetUploadPortAsync().ConfigureAwait(false);
                    Console.WriteLine(port);
                    if (port != null)
                    {
                        args.Add("--upload-port");
                        args.Add(port);
                    }
                }
            }

            //Some pio stuff uses Standard Output, some uses Standard Error, its easier to just flatten both of those to a single stream
            process.StartInfo.Arguments =
                $"-c \"import subprocess;subprocess.run([{string.Join(",", args.Select(s => $"'{s}'"))}],stderr=subprocess.STDOUT)\""
                    .Replace("\\", "\\\\");

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            var state = 0;
            process.Start();
            Console.WriteLine("Starting process " + environment);

            // process.BeginOutputReadLine();
            // process.BeginErrorReadLine();
            var buffer = new char[1];
            var hasError = false;
            var main = sections == 5;
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

                    if (!line.Contains("FAILED")) continue;
                    platformIoOutput.OnError(new Exception("{progressMessage} - Error"));
                    hasError = true;
                    break;
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
                        }
                    }
                }

            await process.WaitForExitAsync();

            if (!hasError)
            {
                if (uploading)
                {
                    currentProgress = progressEndingPercentage;
                    if (device!.IsMini())
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            $"{progressMessage} - Done", null));
                        device.Reconnect();
                    }
                    else
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            $"{progressMessage} - Waiting for Device", null));
                    }
                }

                platformIoOutput.OnCompleted();
            }

            _semaphore.Release(1);
        }

        _ = Process();
        return platformIoOutput;
    }

    public record PlatformIoState(double Percentage, string Message, string? Log)
    {
        public PlatformIoState WithLog(string log)
        {
            return this with {Log = log};
        }
    }
}
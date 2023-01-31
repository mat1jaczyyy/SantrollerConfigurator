using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Utils;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Mono.Unix;

namespace GuitarConfigurator.NetCore
{
    public class PlatformIo
    {
        public record PlatformIoState(double Percentage, string Message, string? Log)
        {
            public PlatformIoState WithLog(string log)
            {
                return this with {Log = log};
            }
        }

        private readonly string _pioExecutable;

        public string FirmwareDir { get; }

        //TODO: probably have a nice script to update this, but for now: ` pio pkg list | grep "@"|cut -f1 -d"(" |cut -c 11- | sort -u | wc -l`
        private const int PackageCount = 17;
        private string PlatformIOVersion = "6.1.5";

        private readonly Process _portProcess;

        public PlatformIo()
        {
            var appdataFolder = AssetUtils.GetAppDataFolder();
            if (!File.Exists(appdataFolder))
            {
                Directory.CreateDirectory(appdataFolder);
            }

            var pioFolder = Path.Combine(appdataFolder, "platformio");
            var pioExecutablePath = Path.Combine(appdataFolder, "python", "bin", "platformio");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pioExecutablePath = Path.Combine(appdataFolder, "python", "Scripts", "platformio.exe");
            }

            _pioExecutable = pioExecutablePath;
            FirmwareDir = Path.Combine(appdataFolder, "firmware");

            _portProcess = new Process();
            _portProcess.EnableRaisingEvents = true;
            _portProcess.StartInfo.FileName = _pioExecutable;
            _portProcess.StartInfo.WorkingDirectory = FirmwareDir;
            _portProcess.StartInfo.EnvironmentVariables["PLATFORMIO_CORE_DIR"] = pioFolder;

            _portProcess.StartInfo.Arguments = "device list --json-output";

            _portProcess.StartInfo.UseShellExecute = false;
            _portProcess.StartInfo.RedirectStandardOutput = true;
            _portProcess.StartInfo.RedirectStandardError = true;
        }

        private async Task RevertFirmware(BehaviorSubject<PlatformIoState> platformIoOutput)
        {
            var appdataFolder = AssetUtils.GetAppDataFolder();
            if (Directory.Exists(FirmwareDir))
            {
                Directory.Delete(FirmwareDir, true);
            }

            platformIoOutput.OnNext(new PlatformIoState(0, "Extracting Firmware", ""));
            var firmwareZipPath = Path.Combine(appdataFolder, "firmware.zip");
            await AssetUtils.ExtractZip("firmware.zip", firmwareZipPath, FirmwareDir);
        }

        public IObservable<PlatformIoState> InitialisePlatformIo()
        {
            var platformIoOutput =
                new BehaviorSubject<PlatformIoState>(new PlatformIoState(0, "Searching for python", null));
            Task.Run(async () =>
            {
                // On startup, reinstall the firmware, this will make sure that an update goes out, and also makes sure that the firmware is clean.
                await RevertFirmware(platformIoOutput);
                await File.WriteAllTextAsync(Path.Combine(FirmwareDir, "platformio.ini"),
                    (await File.ReadAllTextAsync(Path.Combine(FirmwareDir, "platformio.ini"))).Replace(
                        "post:ardwiino_script_post.py",
                        "post:ardwiino_script_post_tool.py"));
                if (File.Exists(_pioExecutable))
                {
                    // Make sure to update platform.io to whatever version we require. This is a noop and doesn't even require internet if the version is up to date.
                    var python = await FindPython(platformIoOutput);
                    var installerProcess = new Process();
                    installerProcess.StartInfo.FileName = python;
                    installerProcess.StartInfo.Arguments = $"-m pip install platformio=={PlatformIOVersion}";
                    installerProcess.StartInfo.UseShellExecute = false;
                    installerProcess.StartInfo.RedirectStandardOutput = true;
                    installerProcess.StartInfo.RedirectStandardError = true;
                    installerProcess.OutputDataReceived += (_, e) =>
                        platformIoOutput.OnNext(platformIoOutput.Value.WithLog(e.Data!));
                    installerProcess.ErrorDataReceived += (_, e) =>
                        platformIoOutput.OnNext(platformIoOutput.Value.WithLog(e.Data!));
                    installerProcess.Start();
                    installerProcess.BeginOutputReadLine();
                    installerProcess.BeginErrorReadLine();
                    await installerProcess.WaitForExitAsync().ConfigureAwait(false);
                }
                else
                {
                    platformIoOutput.OnNext(new PlatformIoState(0, "Searching for python", null));
                    var python = await FindPython(platformIoOutput);
                    platformIoOutput.OnNext(new PlatformIoState(60, "Installing Platform.IO", null));
                    var installerProcess = new Process();
                    installerProcess.StartInfo.FileName = python;
                    installerProcess.StartInfo.Arguments = $"-m pip install platformio=={PlatformIOVersion}";
                    installerProcess.StartInfo.UseShellExecute = false;
                    installerProcess.StartInfo.RedirectStandardOutput = true;
                    installerProcess.StartInfo.RedirectStandardError = true;
                    installerProcess.OutputDataReceived += (_, e) =>
                        platformIoOutput.OnNext(platformIoOutput.Value.WithLog(e.Data!));
                    installerProcess.ErrorDataReceived += (_, e) =>
                        platformIoOutput.OnNext(platformIoOutput.Value.WithLog(e.Data!));
                    installerProcess.Start();
                    installerProcess.BeginOutputReadLine();
                    installerProcess.BeginErrorReadLine();
                    await installerProcess.WaitForExitAsync().ConfigureAwait(false);
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
                }

                platformIoOutput.OnCompleted();
            });
            return platformIoOutput;
        }

        public async Task<PlatformIoPort[]?> GetPorts()
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
            BehaviorSubject<PlatformIoState> platformIoOutput =
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
                var python = await FindPython(platformIoOutput);
                var process = new Process();
                process.EnableRaisingEvents = true;
                process.StartInfo.FileName = python;
                process.StartInfo.WorkingDirectory = FirmwareDir;
                process.StartInfo.EnvironmentVariables["PLATFORMIO_CORE_DIR"] = pioFolder;
                process.StartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
                var args = new List<string>(command);
                args.Insert(0, _pioExecutable);
                var sections = 5;
                var isUsb = false;
                if (environment != null)
                {
                    if (device is Arduino)
                    {
                        sections = 10;
                    }

                    if (environment.EndsWith("_usb"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            $"{progressMessage} - Looking for device", null));
                        currentProgress += percentageStep / sections;
                        if (device != null)
                        {
                            isUsb = true;
                        }

                        sections = 10;
                    }

                    percentageStep = (progressEndingPercentage - progressStartingPercentage);
                    args.Add("--environment");
                    args.Add(environment);
                    if (uploading && !isUsb)
                    {
                        if (environment.Contains("pico"))
                        {
                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                $"{progressMessage} - Looking for device", null));
                            currentProgress += percentageStep / sections;
                            sections = 2;
                        }

                        if (device != null)
                        {
                            var port = await device.GetUploadPort().ConfigureAwait(false);
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
                {
                    if (state == 0)
                    {
                        var line = await process.StandardOutput.ReadLineAsync();
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
                            {
                                if (device is Santroller)
                                {
                                    device.Bootloader();
                                }
                            }

                            if (line.StartsWith("Looking for upload port..."))
                            {
                                platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                    $"{progressMessage} - Looking for port", null));
                                currentProgress += percentageStep / sections;


                                if (device is Santroller or Ardwiino && !isUsb)
                                {
                                    device.Bootloader();
                                }
                            }

                            if (line.Contains("SUCCESS"))
                            {
                                if (device is PicoDevice || sections == 5)
                                {
                                    break;
                                }
                            }
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
                        while ((await process.StandardOutput.ReadAsync(buffer, 0, 1)) > 0)
                        {
                            // process character...for example:
                            if (buffer[0] == '#')
                            {
                                currentProgress += percentageStep / 50 / sections;
                            }

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
                                        if (uploadCount > 0)
                                        {
                                            currentProgress += percentageStep / 11 * uploadCount;
                                        }

                                        state = 0;
                                    }

                                    platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                        $"{progressMessage} - {uploadPackage}", null));
                                    break;
                            }

                            if (state == 0)
                            {
                                break;
                            }
                        }
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

        private async Task<string?> SetupVenv(string pythonApp, string pythonFolder,
            BehaviorSubject<PlatformIoState> platformIoOutput)
        {
            var error = false;
            var penvProcess = new Process();
            penvProcess.StartInfo.FileName = pythonApp;
            penvProcess.StartInfo.Arguments = $"-m venv {pythonFolder}";
            penvProcess.StartInfo.UseShellExecute = false;
            penvProcess.StartInfo.RedirectStandardOutput = true;
            penvProcess.StartInfo.RedirectStandardError = true;
            penvProcess.OutputDataReceived += (_, e) =>
            {
                platformIoOutput.OnNext(platformIoOutput.Value.WithLog(e.Data!));
                // No support for venv here, so just fall back to downloading python in that case.
                if (e.Data != null && e.Data!.Contains("ensurepip is not"))
                {
                    error = true;
                    Directory.Delete(pythonFolder, true);
                    Console.WriteLine("Stdout");
                }
            };
            penvProcess.ErrorDataReceived += (_, e) => platformIoOutput.OnNext(platformIoOutput.Value.WithLog(e.Data!));
            penvProcess.Start();
            penvProcess.BeginOutputReadLine();
            penvProcess.BeginErrorReadLine();
            await penvProcess.WaitForExitAsync();
            if (error)
            {
                return null;
            }

            var executables = GetPythonExecutables();
            foreach (var executable in executables)
            {
                var pythonAppdataExecutable = Path.Combine(pythonFolder, executable);
                if (File.Exists(pythonAppdataExecutable))
                {
                    return pythonAppdataExecutable;
                }
            }

            return null;
        }

        private async Task<string?> FindPython(BehaviorSubject<PlatformIoState> platformIoOutput)
        {
            var appdataFolder = AssetUtils.GetAppDataFolder();
            var pythonFolder = Path.Combine(appdataFolder, "python");
            var pythonLoc = Path.Combine(pythonFolder, "python.tar.gz");
            var executables = GetPythonExecutables();
            Directory.CreateDirectory(appdataFolder);

            foreach (var executable in executables)
            {
                var pythonAppdataExecutable = Path.Combine(pythonFolder, executable);
                if (File.Exists(pythonAppdataExecutable))
                {
                    return pythonAppdataExecutable;
                }
            }

            string? foundExecutable = null;
            foreach (var executable in executables)
            {
                foundExecutable = GetFullPath(executable);
                if (foundExecutable == null) continue;
                var ret = await SetupVenv(executable, pythonFolder, platformIoOutput);
                if (ret != null)
                {
                    return ret;
                }
            }

            Directory.CreateDirectory(pythonFolder);
            if (foundExecutable == null)
            {
                platformIoOutput.OnNext(new PlatformIoState(10, "Downloading python portable", null));
                var pythonJsonLoc = Path.Combine(pythonFolder, "python.json");
                var arch = GetSysType();
                using (var download = new HttpClientDownloadWithProgress(
                           "https://api.github.com/repos/indygreg/python-build-standalone/releases/62235403",
                           pythonJsonLoc))
                {
                    await download.StartDownload().ConfigureAwait(false);
                }

                var jsonRelease = await File.ReadAllTextAsync(pythonJsonLoc);
                var release = GithubRelease.FromJson(jsonRelease);
                var found = false;
                foreach (var asset in release.Assets)
                {
                    if (asset.Name.EndsWith($"{arch}-install_only.tar.gz"))
                    {
                        using (var download =
                               new HttpClientDownloadWithProgress(asset.BrowserDownloadUrl.ToString(), pythonLoc))
                        {
                            // This isnt right, the percentage bar goes too far
                            download.ProgressChanged += (_, _, percentage) =>
                                platformIoOutput.OnNext(new PlatformIoState(20 + (percentage * 0.4) ?? 0,
                                    "Downloading python portable", null));
                            await download.StartDownload().ConfigureAwait(false);
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return null;
                }

                platformIoOutput.OnNext(new PlatformIoState(60, "Extracting python portable", null));
                await using (var inStream = File.OpenRead(pythonLoc))
                {
                    Stream gzipStream = new GZipInputStream(inStream);

                    var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8);
                    tarArchive.ExtractContents(appdataFolder);
                    tarArchive.Close();

                    gzipStream.Close();
                    File.Delete(pythonLoc);
                    File.Delete(pythonJsonLoc);
                }
            }

            foreach (var executable in executables)
            {
                var pythonAppdataExecutable = Path.Combine(pythonFolder, executable);
                if (File.Exists(pythonAppdataExecutable))
                {
                    var unixFileInfo = new UnixFileInfo(pythonAppdataExecutable);
                    unixFileInfo.FileAccessPermissions |= FileAccessPermissions.UserExecute;
                    return pythonAppdataExecutable;
                }
            }

            return null;
        }

        private string[] GetPythonExecutables()
        {
            var executables = new[] {"python3", "python", Path.Combine("bin", "python3.10")};
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                executables = new[] {"python.exe", Path.Combine("Scripts", "python.exe")};
            }

            return executables;
        }

        private string GetSysType()
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{arch}-pc-windows-msvc-shared";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"{arch}-apple-darwin";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"{arch}-unknown-linux-gnu";
            }

            return "unsupported";
        }

        public bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
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
    }
}
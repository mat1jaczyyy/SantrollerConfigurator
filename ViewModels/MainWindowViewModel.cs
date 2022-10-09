using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Collections;
using DynamicData;
using GuitarConfiguratorSharp.NetCore.Utils;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.DeviceNotify.Linux;
using LibUsbDotNet.Main;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen, IDisposable
    {
        // The Router associated with this Screen.
        // Required by the IScreen interface.
        public RoutingState Router { get; } = new();
        public ReactiveCommand<Unit, IRoutableViewModel> Configure { get; }

        // The command that navigates a user back.
        public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

        public AvaloniaList<IConfigurableDevice> Devices { get; } = new();

        private IConfigurableDevice? _selectedDevice;
        private IConfigurableDevice? _disconnectedDevice;

        public bool MigrationSupported => SelectedDevice == null || SelectedDevice.MigrationSupported;

        private bool _writingToUsb = false;

        private readonly static string UdevFile = "99-ardwiino.rules";
        private readonly static string UdevPath = $"/etc/udev/rules.d/{UdevFile}";

        public IConfigurableDevice? SelectedDevice
        {
            get
            {
                return _selectedDevice;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDevice, value);
                this.RaisePropertyChanged("MigrationSupported");
                Connected = SelectedDevice != null;
            }
        }


        private bool _working = true;

        public bool Working
        {
            get
            {
                return _working;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _working, value);
            }
        }


        private bool _installed = false;

        public bool Installed
        {
            get
            {
                return _installed;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _installed, value);
            }
        }

        private string _progressbarcolor = "PrimaryColor";

        public string ProgressbarColor
        {
            get
            {
                return _progressbarcolor;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _progressbarcolor, value);
            }
        }

        private bool _connected = false;

        public bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _connected, value);
            }
        }

        private bool _readyToConfigure = false;

        public bool ReadyToConfigure
        {
            get
            {
                return _readyToConfigure;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _readyToConfigure, value);
            }
        }

        private double _progress = 0;

        public double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _progress, value);
            }
        }

        private string _message = "Connected";

        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _message, value);
            }
        }
        internal async Task Write(ConfigViewModel config)
        {
            if (config.MicroController == null) return;
            config.Generate(Pio);
            if (config.MicroController.Board.HasUsbmcu)
            {
                _writingToUsb = true;
                await Pio.RunPlatformIo(config.MicroController.Board.Environment + "-usb", "run --target upload", "Writing - USB", 0, 0, 40, SelectedDevice);
            }
            else
            {
                await Pio.RunPlatformIo(config.MicroController.Board.Environment, "run --target upload", "Writing", 0, 0, 90, SelectedDevice);
            }
        }

        private readonly IDeviceNotifier _deviceListener;
        public PlatformIo Pio { get; } = new();

        private readonly Timer _timer = new();

        private class RegDeviceNotifyInfo : IUsbDeviceNotifyInfo
        {
            private readonly UsbRegistry _dev;

            public RegDeviceNotifyInfo(UsbRegistry dev)
            {
                _dev = dev;
            }

            public UsbSymbolicName SymbolicName => UsbSymbolicName.Parse(_dev.SymbolicName);

            public string Name => _dev.DevicePath;

            public Guid ClassGuid => _dev.DeviceInterfaceGuids[0];

            public int IdVendor => _dev.Vid;

            public int IdProduct => _dev.Pid;

            public string SerialNumber => _dev.Device.Info.SerialString;

            public bool Open(out UsbDevice usbDevice)
            {
                usbDevice = _dev.Device;
                return usbDevice.Open();
            }
        }

        private class DeviceNotifyArgsRegistry : DeviceNotifyEventArgs
        {
            public DeviceNotifyArgsRegistry(UsbRegistry dev)
            {
                Device = new RegDeviceNotifyInfo(dev);
                DeviceType = DeviceType.DeviceInterface;
                EventType = EventType.DeviceArrival;
            }
        }

        public MainWindowViewModel()
        {
            Configure = ReactiveCommand.CreateFromObservable(
                () => Router.Navigate.Execute(new ConfigViewModel(this))
            );
            Router.Navigate.Execute(new MainViewModel(this));

            Pio.TextChanged += (message, clear) =>
            {
                Console.WriteLine(message);
            };

            Pio.PlatformIoError += (val) =>
            {
                ProgressbarColor = val ? "red" : "PrimaryColor";
            };

            Pio.ProgressChanged += (message, val, val2) =>
            {
                Message = message;
                Progress = val2;
            };

            Devices.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    if (SelectedDevice == null)
                    {
                        SelectedDevice = Devices.First();
                    }

                }
                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    if (Devices.Any())
                    {
                        if (e.OldItems!.Contains(SelectedDevice))
                        {
                            _ = Task.Delay(1).ContinueWith(_ => SelectedDevice = Devices.FirstOrDefault(x => true, null));
                        }
                    }
                }
            };
#if Windows

            DeviceListener = new WindowsDeviceNotifier();
#else
            _deviceListener = new LinuxDeviceNotifier();
#endif
            _deviceListener.OnDeviceNotify += OnDeviceNotify;
            _timer.Elapsed += DevicePoller_Tick;
            _timer.AutoReset = false;
            Pio.PlatformIoInstalled += () =>
            {
                Installed = true;
                foreach (UsbRegistry dev in UsbDevice.AllDevices)
                {
                    OnDeviceNotify(null, new DeviceNotifyArgsRegistry(dev));
                }
                _timer.Start();
            };
            // Do something so that working is only set to false once the guitar appears on the host machine again
            // This will probably involve keeping a copy of the serial number  for the last written to device
            // So that we know the right device is picked back up.
            Pio.PlatformIoWorking += (working) =>
            {
                Working = working;
            };
            _ = Pio.InitialisePlatformIo();

            Task.Run(InstallDependancies);
        }

        private readonly List<string> _currentDrives = new();
        private readonly List<string> _currentPorts = new();

        private void AddDevice(IConfigurableDevice device)
        {
            if (device is Arduino)
            {
                _ = Task.Delay(500).ContinueWith(_ => Devices.Add(device));
            }
            else
            {
                Devices.Add(device);
            }
            if (_disconnectedDevice != null)
            {
                if (_disconnectedDevice.DeviceAdded(device))
                {
                    if (device is ConfigurableUsbDevice)
                    {
                        if (_writingToUsb)
                        {
                            device.BootloaderUsb();
                        }
                        else
                        {
                            _selectedDevice = device;
                            _disconnectedDevice = null;
                            Connected = true;

                            Message = "Writing - Done";
                            Progress = 100;
                        }
                    }
                    else if (_writingToUsb)
                    {
                        _writingToUsb = false;
                        Message = "Writing - USB - Done";
                        Progress = 50;
                        var usbdevice = _disconnectedDevice as ConfigurableUsbDevice;
                        if (usbdevice != null)
                        {
                            Pio.RunPlatformIo(usbdevice.Board.Environment, "run --target upload", "Writing - Main", 0, 50, 90, device).ConfigureAwait(false);
                        }
                    }
                }
            }
        }
        private void DevicePoller_Tick(object? sender, ElapsedEventArgs e)
        {
            var drives = DriveInfo.GetDrives();
            var currentDrivesSet = _currentDrives.ToHashSet();
            foreach (var drive in drives)
            {
                if (currentDrivesSet.Remove(drive.RootDirectory.FullName))
                {
                    continue;
                }
                var uf2 = Path.Combine(drive.RootDirectory.FullName, "INFO_UF2.txt");
                if (drive.IsReady)
                {
                    if (File.Exists(uf2) && File.ReadAllText(uf2).Contains("RPI-RP2"))
                    {
                        AddDevice(new PicoDevice(Pio, drive.RootDirectory.FullName));
                    }
                }
                _currentDrives.Add(drive.RootDirectory.FullName);
            }
            // We removed all valid devices above, so anything left in currentDrivesSet is no longer valid
            Devices.RemoveMany(Devices.Where(x => x is PicoDevice pico && currentDrivesSet.Contains(pico.GetPath())));
            _currentDrives.RemoveMany(currentDrivesSet);

            var existingPorts = _currentPorts.ToHashSet();
            var ports = Pio.GetPorts().Result;
            if (ports != null)
            {
                foreach (var port in ports)
                {
                    if (existingPorts.Contains(port.Port))
                    {
                        continue;
                    }
                    var arduino = new Arduino(Pio, port);
                    AddDevice(arduino);
                    _currentPorts.Add(arduino.GetSerialPort());
                }
                var currentSerialPorts = ports.Select(port => port.Port).ToHashSet();
                _currentPorts.RemoveMany(_currentPorts.Where(port => !currentSerialPorts.Contains(port)));
                Devices.RemoveMany(Devices.Where(device => device is Arduino arduino && !currentSerialPorts.Contains(arduino.GetSerialPort())));
            }
            ReadyToConfigure = null != SelectedDevice && Installed;
            _timer.Start();
        }


        private void OnDeviceNotify(object? sender, DeviceNotifyEventArgs e)
        {
            if (e.DeviceType == DeviceType.DeviceInterface)
            {
                if (e.EventType == EventType.DeviceArrival)
                {
                    var vid = e.Device.IdVendor;
                    var pid = e.Device.IdProduct;
                    if (vid == Dfu.DfuVid && (pid == Dfu.DfuPid16U2 || pid == Dfu.DfuPid8U2))
                    {
                        AddDevice(new Dfu(e));
                    }
                    else if (e.Device.Open(out UsbDevice dev))
                    {
                        ushort revision = (ushort)dev.Info.Descriptor.BcdDevice;
                        String product = dev.Info.ProductString;
                        String serial = dev.Info.SerialString; ;
                        if (product == "Santroller")
                        {
                            var c = new Santroller(Pio, e.Device.Name, dev, product, serial, revision);
                            AddDevice(c);
                        }
                        else if (product == "Ardwiino")
                        {
                            if (revision == Ardwiino.SerialArdwiinoRevision)
                            {
                                return;
                            }
                            else
                            {
                                AddDevice(new Ardwiino(Pio, e.Device.Name, dev, product, serial, revision));
                            }
                        }
                        else
                        {
                            dev.Close();
                        }
                    }
                }
                else
                {
                    Devices.RemoveMany(Devices.Where(device => device.IsSameDevice(e.Device.Name)));
                    if (_disconnectedDevice == null && _selectedDevice is ConfigurableUsbDevice && _selectedDevice?.IsSameDevice(e.Device.Name) == true)
                    {
                        _disconnectedDevice = _selectedDevice;
                        Connected = false;
                    }
                }
            }
        }

        public void Dispose()
        {
            //             DeviceListener.DeviceDisconnected -= DevicePoller_DeviceDisconnected;
            //             DeviceListener.DeviceInitialized -= DevicePoller_DeviceInitialized;
            //             DeviceListener.Dispose();
        }

        public bool CheckDependancies()
        {
            // Call check dependancies on startup, and pop up a dialog saying drivers are missing would you like to install if they are missing
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                ProcessStartInfo info = new ProcessStartInfo(Path.Combine(windowsDir, "sysnative", "pnputil.exe"));
                info.ArgumentList.Add("-e");
                info.UseShellExecute = true;
                info.RedirectStandardOutput = true;
                var process = Process.Start(info);
                if (process == null) return false;
                var output = process.StandardOutput.ReadToEnd();
                // Check if the driver exists (we install this specific version of the driver so its easiest to check for it.)
                return output.Contains("Atmel USB Devices") && output.Contains("Atmel Corporation") && output.Contains("10/02/2010 1.2.2.0");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return File.Exists(UdevPath);
            }
            return true;
        }

        public async void InstallDependancies()
        {
            if (CheckDependancies()) return;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use pnputil to install the drivers, utlising runas to run with admin
                //TODO: Test using SpecialFolder.System instead of windows
                var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string appdataFolder = AssetUtils.GetAppDataFolder();
                string driverZip = Path.Combine(appdataFolder, "drivers.zip");
                string driverFolder = Path.Combine(appdataFolder, "drivers");
                await AssetUtils.ExtractZip("dfu.zip", driverZip, driverFolder);

                var info = new ProcessStartInfo(Path.Combine(windowsDir, "sysnative", "pnputil.exe"));
                info.ArgumentList.AddRange(new string[] { "-i", "-a", Path.Combine(driverFolder, "atmel_usb_dfu.inf") });
                info.UseShellExecute = true;
                info.Verb = "runas";
                Process.Start(info);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Just copy the file to install it, using pkexec for admin
                string appdataFolder = AssetUtils.GetAppDataFolder();
                string rules = Path.Combine(appdataFolder, UdevFile);
                await AssetUtils.ExtractFile(UdevFile, rules);
                var info = new ProcessStartInfo("pkexec");
                info.ArgumentList.AddRange(new string[] { "cp", rules, UdevPath });
                info.UseShellExecute = true;
                Process.Start(info);
            }
            if (!CheckDependancies())
            {
                // Pop open a dialog that it failed and to try again
            }
        }

    }

}
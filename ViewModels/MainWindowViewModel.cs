using System;
using System.Reactive;
using DynamicData;
using GuitarConfiguratorSharp.Utils;
using ReactiveUI;
using System.Linq;
using System.IO;
using System.Timers;
using Avalonia.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Linux;
using LibUsbDotNet.Main;
using LibUsbDotNet.DeviceNotify.Info;

namespace GuitarConfiguratorSharp.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen, IDisposable
    {
        // The Router associated with this Screen.
        // Required by the IScreen interface.
        public RoutingState Router { get; } = new RoutingState();

        public ReactiveCommand<Unit, IRoutableViewModel> Configure { get; }

        // The command that navigates a user back.
        public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

        public AvaloniaList<ConfigurableDevice> Devices { get; } = new AvaloniaList<ConfigurableDevice>();

        private ConfigurableDevice? _selectedDevice;
        private ConfigurableDevice? _disconnectedDevice;

        private bool MigrationSupported => SelectedDevice == null || SelectedDevice.MigrationSupported;

        private readonly static string UDEV_FILE = "99-ardwiino.rules";
        private readonly static string UDEV_PATH = $"/etc/udev/rules.d/{UDEV_FILE}";

        public ConfigurableDevice? SelectedDevice
        {
            get
            {
                return _selectedDevice;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDevice, value);
                this.RaisePropertyChanged("MigrationSupported");
                this.Connected = SelectedDevice != null;
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

        private IDeviceNotifier DeviceListener;
        public PlatformIO pio { get; } = new PlatformIO();

        private Timer timer = new Timer();

        private class RegDeviceNotifyInfo : IUsbDeviceNotifyInfo
        {
            private UsbRegistry dev;

            public RegDeviceNotifyInfo(UsbRegistry dev)
            {
                this.dev = dev;
            }

            public UsbSymbolicName SymbolicName => UsbSymbolicName.Parse(dev.SymbolicName);

            public string Name => dev.DevicePath;

            public Guid ClassGuid => dev.DeviceInterfaceGuids[0];

            public int IdVendor => dev.Vid;

            public int IdProduct => dev.Pid;

            public string SerialNumber => dev.Device.Info.SerialString;

            public bool Open(out UsbDevice usbDevice)
            {
                usbDevice = dev.Device;
                return usbDevice.Open();
            }
        }

        private class DeviceNotifyArgsRegistry: DeviceNotifyEventArgs {
            public DeviceNotifyArgsRegistry(UsbRegistry dev) {
                this.Device = new RegDeviceNotifyInfo(dev);
                this.DeviceType = DeviceType.DeviceInterface;
                this.EventType = EventType.DeviceArrival;
            }
        }

        public MainWindowViewModel()
        {
            Configure = ReactiveCommand.CreateFromObservable(
                () => Router.Navigate.Execute(new ConfigViewModel(this))
            );
            Router.Navigate.Execute(new MainViewModel(this));

            pio.TextChanged += (message, clear) =>
            {
                Console.WriteLine(message);
            };

            pio.PlatformIOError += (val) => this.ProgressbarColor = val ? "red" : "PrimaryColor";

            pio.ProgressChanged += (message, val, val2) =>
            {
                this.Message = message;
                this.Progress = val2;
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
            DeviceListener = new LinuxDeviceNotifier();
#endif
            DeviceListener.OnDeviceNotify += OnDeviceNotify;
            timer.Elapsed += DevicePoller_Tick;
            timer.AutoReset = false;
            pio.PlatformIOInstalled += () =>
            {
                this.Installed = true;
                foreach (UsbRegistry dev in UsbDevice.AllDevices) {
                    OnDeviceNotify(null, new DeviceNotifyArgsRegistry(dev));
                }
                timer.Start();
            };
            // Do something so that working is only set to false once the guitar appears on the host machine again
            // This will probably involve keeping a copy of the serial number  for the last written to device
            // So that we know the right device is picked back up.
            pio.PlatformIOWorking += (working) =>
            {
                this.Working = working;
            };
            _ = pio.InitialisePlatformIO();

            Task.Run(InstallDependancies);
        }

        private List<string> currentDrives = new List<string>();
        private List<string> currentPorts = new List<string>();

        private void AddDevice(ConfigurableDevice device)
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
                _disconnectedDevice.DeviceAdded(device);
            }
        }
        private void DevicePoller_Tick(object? sender, ElapsedEventArgs e)
        {
            var drives = DriveInfo.GetDrives();
            var currentDrivesSet = currentDrives.ToHashSet();
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
                        AddDevice(new PicoDevice(pio, drive.RootDirectory.FullName));
                    }
                }
                currentDrives.Add(drive.RootDirectory.FullName);
            }
            // We removed all valid devices above, so anything left in currentDrivesSet is no longer valid
            Devices.RemoveMany(Devices.Where(x => x is PicoDevice pico && currentDrivesSet.Contains(pico.GetPath())));
            currentDrives.RemoveMany(currentDrivesSet);

            var existingPorts = currentPorts.ToHashSet();
            var ports = pio.GetPorts().Result;
            foreach (var port in ports)
            {
                if (existingPorts.Contains(port.Port))
                {
                    continue;
                }
                var arduino = new Arduino(pio, port);
                AddDevice(arduino);
                currentPorts.Add(arduino.GetSerialPort());
            }
            var currentSerialPorts = ports.Select(port => port.Port).ToHashSet();
            currentPorts.RemoveMany(currentPorts.Where(port => !currentSerialPorts.Contains(port)));
            Devices.RemoveMany(Devices.Where(device => device is Arduino arduino && !currentSerialPorts.Contains(arduino.GetSerialPort())));
            ReadyToConfigure = null != SelectedDevice && Installed;
            timer.Start();
        }


        private void OnDeviceNotify(object? sender, DeviceNotifyEventArgs e)
        {
            Console.WriteLine(e.Device.Name);
            if (e.DeviceType == DeviceType.DeviceInterface)
            {
                if (e.EventType == EventType.DeviceArrival)
                {
                    var vid = e.Device.IdVendor;
                    var pid = e.Device.IdProduct;
                    // If ardwiino / santroller
                    if (e.Device.Open(out UsbDevice dev))
                    {
                        ushort revision = (ushort)dev.Info.Descriptor.BcdDevice;
                        String product = dev.Info.ProductString;
                        String serial = dev.Info.SerialString; ;
                        if (product == "Santroller")
                        {
                            var c = new Santroller(pio, e.Device.Name, dev, product, serial, revision);
                            AddDevice(c);
                            if (_disconnectedDevice?.IsSameDevice(serial) == true)
                            {
                                _selectedDevice = c;
                                this.Connected = true;
                                this.Message = "Writing - Done";
                                this.Progress = 100;
                            }
                        }
                        else if (product == "Ardwiino")
                        {
                            if (revision == Ardwiino.SERIAL_ARDWIINO_REVISION)
                            {
                                return;
                            }
                            else
                            {
                                AddDevice(new Ardwiino(pio, e.Device.Name, dev, product, serial, revision));
                            }
                        }
                    }

                    else if (vid == Dfu.DFU_VID && (pid == Dfu.DFU_PID_16U2 || pid == Dfu.DFU_PID_8U2))
                    {
                        AddDevice(new Dfu(e));
                    }
                }
                else
                {
                    Devices.RemoveMany(Devices.Where(device => device.IsSameDevice(e.Device.Name)));
                    if (_selectedDevice?.IsSameDevice(e.Device.Name) == true)
                    {
                        _disconnectedDevice = _selectedDevice;
                        this.Connected = false;
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
                return File.Exists(UDEV_PATH);
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
                string rules = Path.Combine(appdataFolder, UDEV_FILE);
                await AssetUtils.ExtractFile(UDEV_FILE, rules);
                var info = new ProcessStartInfo("pkexec");
                info.ArgumentList.AddRange(new string[] { "cp", rules, UDEV_PATH });
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
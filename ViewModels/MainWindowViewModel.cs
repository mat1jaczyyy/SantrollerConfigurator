using System;
using System.Reactive;
using DynamicData;
using GuitarConfiguratorSharp.Utils;
using ReactiveUI;
using Device.Net;
using System.Linq;
using Usb.Net;
using System.IO;
using System.Timers;
using Avalonia.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if Windows
using SerialPort.Net.Windows;
using Hid.Net.Windows;
using Usb.Net.Windows;
using Device.Net.Windows;
#else
using Device.Net.LibUsb;
using LibUsbDotNet.LudnMonoLibUsb;
#endif

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

        private DeviceListener DeviceListener;
        public PlatformIO pio { get; } = new PlatformIO();

        private Timer timer = new Timer();

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

            pio.ProgressChanged += (message, val, val2) =>
            {
                this.Message = message;
                this.Progress = val2;
                Console.WriteLine(message);
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
            IDeviceFactory factory = Santroller.SantrollerDeviceFilter.CreateWindowsUsbDeviceFactory();
#else
            IDeviceFactory factory = Santroller.SantrollerDeviceFilter.CreateLibUsbDeviceFactory2();
#endif
            // For arduinos and ardwiinos / santrollers, we can just listen for hotplug events
            DeviceListener = new DeviceListener(factory);
            DeviceListener.DeviceDisconnected += DevicePoller_DeviceDisconnected;
            DeviceListener.DeviceInitialized += DevicePoller_DeviceInitialized;
            timer.Elapsed += DevicePoller_Tick;
            timer.AutoReset = false;
            pio.PlatformIOInstalled += () =>
            {
                DeviceListener.Start();
                this.Installed = true;
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
        private void DevicePoller_DeviceInitialized(object? sender, DeviceEventArgs e)
        {
#if Windows
            String product = e.Device.ConnectedDeviceDefinition.ProductName;
            String serial = e.Device.ConnectedDeviceDefinition.SerialNumber;
            ushort revision = (ushort)e.Device.ConnectedDeviceDefinition.VersionNumber!;
#else
            UsbDevice device = (UsbDevice)e.Device;
            LibUsbInterfaceManager luim = (LibUsbInterfaceManager)device.UsbInterfaceManager;
            String product;
            String serial;
            ushort revision;
            if (luim.UsbDevice is MonoUsbDevice monoUsbDevice)
            {
                revision = (ushort)monoUsbDevice.Profile.DeviceDescriptor.BcdDevice;
                monoUsbDevice.GetString(out product, 0, monoUsbDevice.Profile.DeviceDescriptor.ProductStringIndex);
                monoUsbDevice.GetString(out serial, 0, monoUsbDevice.Profile.DeviceDescriptor.SerialStringIndex);
            }
            else
            {
                product = luim.UsbDevice.Info.ProductString;
                serial = luim.UsbDevice.Info.SerialString;
                revision = (ushort)luim.UsbDevice.Info.Descriptor.BcdDevice;
            }
#endif 
            if (Devices.Any(device => device.IsSameDevice(serial)))
            {
                return;
            }
            if (product == "Santroller")
            {
                var c = new Santroller(pio, e.Device, product, serial, revision);
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
                    AddDevice(new Ardwiino(pio, e.Device, product, serial, revision));
                }
            }
        }

        private void DevicePoller_DeviceDisconnected(object? sender, DeviceEventArgs e)
        {
            Devices.RemoveMany(Devices.Where(device => device.IsSameDevice(e.Device)));
            if (_selectedDevice?.IsSameDevice(e.Device) == true)
            {
                _disconnectedDevice = _selectedDevice;
                this.Connected = false;
            }
        }
        public void Dispose()
        {
            DeviceListener.DeviceDisconnected -= DevicePoller_DeviceDisconnected;
            DeviceListener.DeviceInitialized -= DevicePoller_DeviceInitialized;
            DeviceListener.Dispose();
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
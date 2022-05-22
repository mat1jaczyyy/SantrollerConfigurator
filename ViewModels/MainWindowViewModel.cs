using System;
using System.Reactive;
using System.Reactive.Subjects;
using DynamicData;
using GuitarConfiguratorSharp.Utils;
using ReactiveUI;
using Device.Net;
using System.Linq;
using Usb.Net;
using System.IO;
using System.Timers;
using Avalonia.Collections;
using HidSharp;
using HidSharp.Experimental;
using HidSharp.Reports;
using HidSharp.Reports.Encodings;
using HidSharp.Utility;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private bool MigrationSupported => SelectedDevice == null || SelectedDevice.MigrationSupported;

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
            }
        }


        private bool _ready = false;

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _ready, value);
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
        private PlatformIO pio = new PlatformIO();

        private Timer timer = new Timer();

        public MainWindowViewModel()
        {
            Configure = ReactiveCommand.CreateFromObservable(
                () => Router.Navigate.Execute(new ConfigViewModel(this))
            );
            Router.Navigate.Execute(new MainViewModel(this));

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
            pio.PlatformIOReady += () =>
            {
                DeviceListener.Start();
                timer.Start();
                this.Ready = true;
            };
            ConfigurableDevice.DeviceInitialised += (dev) => Devices.Add(dev);
            _ = pio.InitialisePlatformIO();
        }

        private List<string> currentDrives = new List<string>();
        private List<string> currentPorts = new List<string>();
        private void DevicePoller_Tick(object? sender, ElapsedEventArgs e)
        {
            var drives = DriveInfo.GetDrives();
            var currentDrivesSet = currentDrives.ToHashSet();
            foreach (var drive in drives)
            {
                if (currentDrives.Remove(drive.RootDirectory.FullName))
                {
                    continue;
                }
                var uf2 = Path.Combine(drive.RootDirectory.FullName, "INFO_UF2.txt");
                if (drive.IsReady)
                {
                    if (File.Exists(uf2) && File.ReadAllText(uf2).Contains("RPI-RP2"))
                    {
                        new Pico(drive.RootDirectory.FullName);
                    }
                }
                currentDrives.Add(drive.RootDirectory.FullName);
            }
            // We removed all valid devices above, so anything left in currentDrivesSet is no longer valid
            Devices.RemoveMany(Devices.Where(x => x is Pico pico && currentDrivesSet.Contains(pico.GetPath())));
            currentDrives.RemoveMany(currentDrivesSet);

            var existingPorts = currentPorts.ToHashSet();
            var ports = pio.GetPorts().Result;
            currentPorts.AddRange(ports.Where(port => !existingPorts.Contains(port.Port)).Select(port => new Arduino(port).GetSerialPort()));
            var currentSerialPorts = ports.Select(port => port.Port).ToHashSet();
            currentPorts.RemoveMany(currentPorts.Where(port => !currentSerialPorts.Contains(port)));
            Devices.RemoveMany(Devices.Where(device => device is Arduino arduino && !currentSerialPorts.Contains(arduino.GetSerialPort())));
            ReadyToConfigure = null != SelectedDevice && Ready;
            timer.Start();
        }
        private void DevicePoller_DeviceInitialized(object? sender, DeviceEventArgs e)
        {
            // TODO: check this all works on windows
            // Also, it seems that LibUsbDotNet might even come with some way to install INFs, though if it isnt that easy to use we can just use the standard driver installer for unos.
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
                new Santroller(e.Device, product, serial, revision);
            }
            else if (product == "Ardwiino")
            {
                // These guys are so old that we hardcoded a revision since the config tool didn't work the way it does now
                // They will just show up as a serial device. In theory these old firmwares actually responded to the standard
                // Arduino programming commands, so doing this will support them for free.
                if (revision == 0x3122)
                {
                    return;
                }
                else
                {
                    new Ardwiino(e.Device, product, serial, revision);
                }
            }
        }

        private void DevicePoller_DeviceDisconnected(object? sender, DeviceEventArgs e)
        {
            Devices.RemoveMany(Devices.Where(device => device.IsSameDevice(e.Device)));
        }
        public void Dispose()
        {
            DeviceListener.DeviceDisconnected -= DevicePoller_DeviceDisconnected;
            DeviceListener.DeviceInitialized -= DevicePoller_DeviceInitialized;
            DeviceListener.Dispose();
        }


    }
}
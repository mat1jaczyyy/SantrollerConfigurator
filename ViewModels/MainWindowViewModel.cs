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
using HidSharp;
using HidSharp.Experimental;
using HidSharp.Reports;
using HidSharp.Reports.Encodings;
using HidSharp.Utility;
#if Windows
using SerialPort.Net.Windows;
using Hid.Net.Windows;
using Usb.Net.Windows;
using Device.Net.Windows;
#else
using Device.Net.LibUsb;
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

        public SourceList<ConfigurableDevice> devices = new SourceList<ConfigurableDevice>();

        public double _progress = 0;

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

        public string _message = "Connected";

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
#if Windows
            IDeviceFactory factory = Santroller.SantrollerDeviceFilter.CreateWindowsUsbDeviceFactory();
#else
            IDeviceFactory factory = Santroller.SantrollerDeviceFilter.CreateLibUsbDeviceFactory2();
#endif
            // TODO: replace this with an actual dropdown on the interface
            devices.Connect()
                .ToCollection()
                .Subscribe(x => Console.WriteLine($"Changed: {string.Join(",", x.Select(up => up.ToString()))}"));
            // For arduinos and ardwiinos / santrollers, we can just listen for hotplug events
            DeviceListener = new DeviceListener(factory);
            DeviceListener.DeviceDisconnected += DevicePoller_DeviceDisconnected;
            DeviceListener.DeviceInitialized += DevicePoller_DeviceInitialized;
            timer.Elapsed += DevicePoller_Tick;
            timer.AutoReset = false;
            pio.PlatformIOReady += () => DeviceListener.Start();
            pio.PlatformIOReady += () => timer.Start();
            _ = pio.InitialisePlatformIO();
        }

        private void DevicePoller_Tick(object? sender, ElapsedEventArgs e)
        {
            var drives = DriveInfo.GetDrives();
            var existing = devices.Items.Where(x => x is Pico).Select(x => ((Pico)x).GetPath()).ToHashSet();
            foreach (var drive in drives)
            {
                if (existing.Remove(drive.RootDirectory.FullName))
                {
                    continue;
                }
                var uf2 = Path.Combine(drive.RootDirectory.FullName, "INFO_UF2.txt");
                if (drive.IsReady)
                {
                    if (File.Exists(uf2) && File.ReadAllText(uf2).Contains("RPI-RP2"))
                    {
                        devices.Add(new Pico(drive.RootDirectory.FullName));
                    }
                }
            }
            devices.Edit(innerList => innerList.RemoveMany(innerList.Where(x => x is Pico && existing.Contains(((Pico)x).GetPath()))));

            var existingPorts = devices.Items.Where(x => x is Arduino).Select(x => ((Arduino)x).GetSerialPort()).ToHashSet();
            var ports = pio.GetPorts().Result;
            devices.AddRange(ports.Where(port => !existingPorts.Contains(port.Port)).Select(port => new Arduino(port)));
            var existingSerialPorts = ports.Select(port => port.Port).ToHashSet();
            devices.Edit(innerList => innerList.RemoveMany(innerList.Where(device => device is Arduino && !existingSerialPorts.Contains(((Arduino)device).GetSerialPort()))));
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
            String product = luim.UsbDevice.Info.ProductString;
            String serial = luim.UsbDevice.Info.SerialString;
            ushort revision = (ushort)luim.UsbDevice.Info.Descriptor.BcdDevice;
#endif 
            if (devices.Items.Any(device => device.IsSameDevice(serial))) {
                return;
            }
            if (product == "Santroller")
            {
                devices.Add(new Santroller(e.Device, product, serial, revision));
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
                    devices.Add(new Ardwiino(e.Device, product, serial, revision));
                }
            }
        }

        private void DevicePoller_DeviceDisconnected(object? sender, DeviceEventArgs e)
        {
            devices.Edit(innerList => innerList.RemoveMany(innerList.Where(device => device.IsSameDevice(e.Device))));
        }
        public void Dispose()
        {
            DeviceListener.DeviceDisconnected -= DevicePoller_DeviceDisconnected;
            DeviceListener.DeviceInitialized -= DevicePoller_DeviceInitialized;
            DeviceListener.Dispose();
        }


    }
}
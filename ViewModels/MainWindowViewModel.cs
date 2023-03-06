using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Kernel;
using GuitarConfigurator.NetCore.Devices;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.DeviceNotify.Linux;
using LibUsbDotNet.Main;
using ReactiveUI;
using Timer = System.Timers.Timer;
#if Windows
using GuitarConfigurator.NetCore.Notify;
using LibUsbDotNet.WinUsb;
#endif

namespace GuitarConfigurator.NetCore.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen, IDisposable
    {
        private static readonly string UdevFile = "99-ardwiino.rules";
        private static readonly string UdevPath = $"/etc/udev/rules.d/{UdevFile}";

        private readonly ObservableAsPropertyHelper<bool> _connected;

        private readonly List<string> _currentDrives = new();
        private readonly HashSet<string> _currentDrivesTemp = new();
        private readonly List<string> _currentPorts = new();

        private readonly IDeviceNotifier _deviceListener;

        private readonly ObservableAsPropertyHelper<bool> _is32U4;

        private readonly ObservableAsPropertyHelper<bool> _isDfu;

        private readonly ObservableAsPropertyHelper<bool> _isMega;

        private readonly ObservableAsPropertyHelper<bool> _isPico;

        private readonly ObservableAsPropertyHelper<bool> _isUno;
        private readonly ObservableAsPropertyHelper<bool> _isGeneric;

        private readonly ObservableAsPropertyHelper<bool> _migrationSupported;

        private readonly ObservableAsPropertyHelper<bool> _newDevice;
        private readonly ObservableAsPropertyHelper<bool> _hasSidebar;

        private readonly Timer _timer = new();

        private Arduino32U4Type _arduino32U4Type;

        private DeviceInputType _deviceInputType;

        private bool _installed;

        private MegaType _megaType;

        private string _message = "Connected";

        private Board _picoType = Board.Rp2040Boards[0];


        public bool Programming { get; private set; }

        private double _progress;

        private string _progressbarcolor = "#FF0078D7";

        private bool _readyToConfigure;

        private IConfigurableDevice? _selectedDevice;

        private UnoMegaType _unoMegaType;
        private AvrType _avrType;

        private SourceList<DeviceInputType> _allDeviceInputTypes = new();
        public ReadOnlyObservableCollection<DeviceInputType> DeviceInputTypes { get; }


        private bool _working = true;

        private static Func<DeviceInputType, bool> CreateFilter(IConfigurableDevice? s)
        {
            return type => type != DeviceInputType.Rf || s?.IsGeneric() != true;
        }

        public MainWindowViewModel()
        {
            AssetUtils.InitNativeLibrary();
            _allDeviceInputTypes.AddRange(Enum.GetValues<DeviceInputType>());
            _allDeviceInputTypes
                .Connect()
                .Filter(this.WhenAnyValue(s => s.SelectedDevice).Select(CreateFilter))
                .Bind(out var deviceInputTypes).Subscribe();
            DeviceInputTypes = deviceInputTypes;
            Configure = ReactiveCommand.CreateFromObservable(
                () => Router.Navigate.Execute(new ConfigViewModel(this, SelectedDevice!))
            );
            AvailableDevices.Connect().Bind(out var devices).Subscribe();
            AvailableDevices.Connect().Subscribe(s =>
            {
                IConfigurableDevice? item = null;
                if (AvailableDevices.Items.Any())
                {
                    item = AvailableDevices.Items.First();
                }
                foreach (var change in s)
                {
                    SelectedDevice = change.Reason switch
                    {
                        ListChangeReason.Add when SelectedDevice == null => change.Item.Current,
                        ListChangeReason.Remove when SelectedDevice == change.Item.Current => item,
                        ListChangeReason.Remove when SelectedDevice == null => item,
                        _ => SelectedDevice
                    };
                }
            });
            Devices = devices;
            Router.Navigate.Execute(new MainViewModel(this));
            _hasSidebar = Router.CurrentViewModel
                .Select(s => s is ConfigViewModel)
                .ToProperty(this, s => s.HasSidebar);
            _migrationSupported = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s?.MigrationSupported != false)
                .ToProperty(this, s => s.MigrationSupported);
            _connected = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s != null)
                .ToProperty(this, s => s.Connected);
            _isPico = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s?.IsPico() == true)
                .ToProperty(this, s => s.IsPico);
            _is32U4 = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Arduino arduino && arduino.Is32U4())
                .ToProperty(this, s => s.Is32U4);
            _isUno = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Arduino arduino && arduino.IsUno())
                .ToProperty(this, s => s.IsUno);
            _isMega = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Arduino arduino && arduino.IsMega())
                .ToProperty(this, s => s.IsMega);
            _isDfu = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Dfu)
                .ToProperty(this, s => s.IsDfu);
            _isGeneric = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s?.IsGeneric() == true)
                .ToProperty(this, s => s.IsGeneric);
            _newDevice = this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is not (null or Ardwiino or Santroller))
                .ToProperty(this, s => s.NewDevice);
            // Make sure that the selected device input type is reset so that we don't end up doing something invalid like using RF on a generic serial device
            this.WhenAnyValue(s => s.SelectedDevice).Subscribe(s =>
            {
                DeviceInputType = DeviceInputType.Direct;
                this.RaisePropertyChanged(nameof(DeviceInputType));
            });
#if Windows
            _deviceListener = new WindowsDeviceNotifierAvalonia();

#else
            _deviceListener = new LinuxDeviceNotifier();
#endif
            _deviceListener.OnDeviceNotify += OnDeviceNotify;
            _timer.Elapsed += DevicePoller_Tick;
            _timer.AutoReset = false;
            StartWorking();
            Pio.InitialisePlatformIo().Subscribe(UpdateProgress, _ => ProgressbarColor = "red", () =>
            {
                Complete(100);
                Working = false;
                Installed = true;
#if Windows
                List<UsbRegistry> deviceListAll = new List<UsbRegistry>();
                List<WinUsbRegistry> deviceList = new List<WinUsbRegistry>();
                WinUsbRegistry.GetWinUsbRegistryList(WindowsDeviceNotifierAvalonia.UsbGuid, out deviceList);
                deviceListAll.AddRange(deviceList);
                WinUsbRegistry.GetWinUsbRegistryList(WindowsDeviceNotifierAvalonia.ArdwiinoGuid, out deviceList);
                deviceListAll.AddRange(deviceList);
                WinUsbRegistry.GetWinUsbRegistryList(WindowsDeviceNotifierAvalonia.SantrollerGuid, out deviceList);
                deviceListAll.AddRange(deviceList);
                (_deviceListener as WindowsDeviceNotifierAvalonia)!.StartEventLoop();
#else
                List<UsbRegistry> deviceListAll = UsbDevice.AllDevices.AsList();
#endif
                foreach (var dev in deviceListAll) OnDeviceNotify(null, new DeviceNotifyArgsRegistry(dev));

                _timer.Start();
            });

            _ = Task.Run(InstallDependenciesAsync);
        }

        public ReactiveCommand<Unit, IRoutableViewModel> Configure { get; }

        // The command that navigates a user back.
        public ReactiveCommand<Unit, IRoutableViewModel?> GoBack => Router.NavigateBack;

        internal SourceList<IConfigurableDevice> AvailableDevices { get; } = new();

        public ReadOnlyObservableCollection<IConfigurableDevice> Devices { get; }
        public bool MigrationSupported => _migrationSupported.Value;
        public bool IsPico => _isPico.Value;
        public bool Is32U4 => _is32U4.Value;
        public bool IsUno => _isUno.Value;
        public bool IsMega => _isMega.Value;
        public bool IsDfu => _isDfu.Value;
        public bool IsGeneric => _isGeneric.Value;
        public bool NewDevice => _newDevice.Value;

        public IEnumerable<Arduino32U4Type> Arduino32U4Types => Enum.GetValues<Arduino32U4Type>();
        public IEnumerable<MegaType> MegaTypes => Enum.GetValues<MegaType>();
        public IEnumerable<UnoMegaType> UnoMegaTypes => Enum.GetValues<UnoMegaType>();
        public IEnumerable<AvrType> AvrTypes => Enum.GetValues<AvrType>();
        public IEnumerable<Board> PicoTypes => Board.Rp2040Boards;

        public AvrType AvrType
        {
            get => _avrType;
            set => this.RaiseAndSetIfChanged(ref _avrType, value);
        }

        public UnoMegaType UnoMegaType
        {
            get => _unoMegaType;
            set => this.RaiseAndSetIfChanged(ref _unoMegaType, value);
        }

        public MegaType MegaType
        {
            get => _megaType;
            set => this.RaiseAndSetIfChanged(ref _megaType, value);
        }

        public DeviceInputType DeviceInputType
        {
            get => _deviceInputType;
            set => this.RaiseAndSetIfChanged(ref _deviceInputType, value);
        }

        public Arduino32U4Type Arduino32U4Type
        {
            get => _arduino32U4Type;
            set => this.RaiseAndSetIfChanged(ref _arduino32U4Type, value);
        }

        public Board PicoType
        {
            get => _picoType;
            set => this.RaiseAndSetIfChanged(ref _picoType, value);
        }

        public IConfigurableDevice? SelectedDevice
        {
            get => _selectedDevice;
            set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
        }


        public bool Working
        {
            get => _working;
            set => this.RaiseAndSetIfChanged(ref _working, value);
        }

        public bool Installed
        {
            get => _installed;
            set => this.RaiseAndSetIfChanged(ref _installed, value);
        }

        public string ProgressbarColor
        {
            get => _progressbarcolor;
            set => this.RaiseAndSetIfChanged(ref _progressbarcolor, value);
        }

        public bool Connected => _connected.Value;
        public bool HasSidebar => _hasSidebar.Value;

        public bool ReadyToConfigure
        {
            get => _readyToConfigure;
            set => this.RaiseAndSetIfChanged(ref _readyToConfigure, value);
        }

        public double Progress
        {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public PlatformIo Pio { get; } = new();

        public void Dispose()
        {
            _deviceListener.OnDeviceNotify -= OnDeviceNotify;
        }

        // The Router associated with this Screen.
        // Required by the IScreen interface.
        public RoutingState Router { get; } = new();

        internal IObservable<PlatformIo.PlatformIoState> Write(ConfigViewModel config)
        {
            StartWorking();
            config.Generate(Pio);
            var env = config.Microcontroller.Board.Environment;
            if (config.Microcontroller.Board.HasUsbmcu) env += "_usb";

            if (NewDevice)
            {
                env = env.Replace("_8", "");
                env = env.Replace("_16", "");
            }

            var output = new StringBuilder();
            Programming = true;
            var command = Pio.RunPlatformIo(env, new[] { "run", "--target", "upload" },
                "Writing",
                0, 90, config.Device);
            command.ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
                {
                    UpdateProgress(s);
                    if (s.Log != null) output.Append(s.Log + "\n");
                }, _ =>
                {
                    ProgressbarColor = "red";
                    config.ShowIssueDialog.Handle((output.ToString(), config)).Subscribe(s => Programming = false);
                },
                () => { Programming = false; });
            return command.OnErrorResumeNext(Observable.Return(command.Value));
        }

        public void Complete(int total)
        {
            Working = false;
            Message = "Done";
            Progress = total;
        }

        private void StartWorking()
        {
            Working = true;
            ProgressbarColor = "#FF0078D7";
        }

        private void UpdateProgress(PlatformIo.PlatformIoState state)
        {
            Progress = state.Percentage;
            Message = state.Message;
        }

#pragma warning disable VSTHRD100
        private async void DevicePoller_Tick(object? sender, ElapsedEventArgs e)
#pragma warning restore VSTHRD100
        {
            var drives = DriveInfo.GetDrives();
            _currentDrivesTemp.UnionWith(_currentDrives);
            foreach (var drive in drives)
            {
                if (_currentDrivesTemp.Remove(drive.RootDirectory.FullName)) continue;

                var uf2 = Path.Combine(drive.RootDirectory.FullName, "INFO_UF2.txt");
                if (drive.IsReady)
                    if (File.Exists(uf2) && File.ReadAllText(uf2).Contains("RPI-RP2"))
                        AvailableDevices.Add(new PicoDevice(Pio, drive.RootDirectory.FullName));

                _currentDrives.Add(drive.RootDirectory.FullName);
            }

            // We removed all valid devices above, so anything left in currentDrivesSet is no longer valid
            AvailableDevices.RemoveMany(AvailableDevices.Items.Where(x =>
                x is PicoDevice pico && _currentDrivesTemp.Contains(pico.GetPath())));
            _currentDrives.RemoveMany(_currentDrivesTemp);

            var existingPorts = _currentPorts.ToHashSet();
            var ports = await Pio.GetPortsAsync();
            if (ports != null)
            {
                foreach (var port in ports)
                {
                    if (existingPorts.Contains(port.Port)) continue;
                    _currentPorts.Add(port.Port);
                    var arduino = new Arduino(Pio, port);
                    _ = Task.Delay(1000).ContinueWith(_ =>
                    {
                        if (arduino.Board.IsGeneric())
                        {
                            // If a device is generic, then we have no real way of detecting it and must send a detection packet to work out what it is
                            var santroller = new Santroller(Pio, port);
                            if (santroller.Valid)
                            {
                                AvailableDevices.Add(santroller);
                            }
                            else
                            {
                                AvailableDevices.Add(arduino);
                            }
                        }
                        else
                        {
                            AvailableDevices.Add(arduino);
                        }
                    }, TaskScheduler.Default);
                }

                var currentSerialPorts = ports.Select(port => port.Port).ToHashSet();
                _currentPorts.RemoveMany(_currentPorts.Where(port => !currentSerialPorts.Contains(port)));
                AvailableDevices.RemoveMany(AvailableDevices.Items.Where(device =>
                    device is Arduino arduino && !currentSerialPorts.Contains(arduino.GetSerialPort())));
                AvailableDevices.RemoveMany(AvailableDevices.Items.Where(device =>
                    device is Santroller santroller && santroller.GetSerialPort().Any() &&
                    !currentSerialPorts.Contains(santroller.GetSerialPort())));
            }

            ReadyToConfigure = null != SelectedDevice && Installed;
            _timer.Start();
        }


        private void OnDeviceNotify(object? sender, DeviceNotifyEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (e.DeviceType != DeviceType.DeviceInterface) return;
                if (e.EventType == EventType.DeviceArrival)
                {
                    var vid = e.Device.IdVendor;
                    var pid = e.Device.IdProduct;
                    if (vid == Dfu.DfuVid && (pid == Dfu.DfuPid16U2 || pid == Dfu.DfuPid8U2))
                    {
                        AvailableDevices.Add(new Dfu(e));
                    }
                    else if (e.Device.Open(out var dev))
                    {
#if Windows
                        UsbRegistry r = dev.UsbRegistryInfo;
                        var product = "";
                        if (e.Device.Name.Contains(WindowsDeviceNotifierAvalonia.ArdwiinoGuid.ToString().ToLower()))
                        {
                            product = "Ardwiino";
                        } else if (e.Device.Name.Contains(WindowsDeviceNotifierAvalonia.SantrollerGuid.ToString().ToLower()))
                        {
                            product = "Santroller";
                        }

                        var revision = (ushort)0;
                        var serial = "";
#else
                        var revision = (ushort) dev.Info.Descriptor.BcdDevice;
                        var product = dev.Info.ProductString?.Split(new[] {'\0'}, 2)[0];
                        var serial = dev.Info.SerialString?.Split(new[] {'\0'}, 2)[0] ?? "";
#endif
                        switch (product)
                        {
                            case "Santroller" when Programming && !IsPico:
                                return;
                            case "Santroller":
                                AvailableDevices.Add(new Santroller(Pio, e.Device.Name, dev, product, serial,
                                    revision));
                                break;
                            case "Ardwiino" when Programming:
                            case "Ardwiino" when revision == Ardwiino.SerialArdwiinoRevision:
                                return;
                            case "Ardwiino":
                                AvailableDevices.Add(new Ardwiino(Pio, e.Device.Name, dev, product, serial, revision));
                                break;
                            default:
                                dev.Close();
                                break;
                        }
                    }
                }
                else
                {
                    AvailableDevices.RemoveMany(
                        AvailableDevices.Items.Where(device => device.IsSameDevice(e.Device.Name)));
                }
            });
        }

        private static bool CheckDependencies()
        {
            // Call check dependencies on startup, and pop up a dialog saying drivers are missing would you like to install if they are missing
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var info = new ProcessStartInfo(Path.Combine(windowsDir, "pnputil.exe"));
                info.ArgumentList.Add("-e");
                info.RedirectStandardOutput = true;
                info.CreateNoWindow = true;
                var process = Process.Start(info);
                if (process == null) return false;
                var output = process.StandardOutput.ReadToEnd();
                // Check if the driver exists (we install this specific version of the driver so its easiest to check for it.)
                return output.Contains("Atmel USB Devices") && output.Contains("Atmel Corporation") &&
                       output.Contains("10/02/2010 1.2.2.0");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return File.Exists(UdevPath);

            return true;
        }

        private static async Task InstallDependenciesAsync()
        {
            if (CheckDependencies()) return;
            //TODO: pop open a dialog before doing this
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
                var appdataFolder = AssetUtils.GetAppDataFolder();
                var driverFolder = Path.Combine(appdataFolder, "drivers");
                await AssetUtils.ExtractXzAsync("dfu.7z", driverFolder);

                var info = new ProcessStartInfo(Path.Combine(windowsDir, "pnputil.exe"));
                info.ArgumentList.AddRange(new[] { "-i", "-a", Path.Combine(driverFolder, "atmel_usb_dfu.inf") });
                info.UseShellExecute = true;
                info.CreateNoWindow = true;
                info.Verb = "runas";
                Process.Start(info);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Just copy the file to install it, using pkexec for admin
                var appdataFolder = AssetUtils.GetAppDataFolder();
                var rules = Path.Combine(appdataFolder, UdevFile);
                await AssetUtils.ExtractFileAsync(UdevFile, rules);
                var info = new ProcessStartInfo("pkexec");
                info.ArgumentList.AddRange(new[] { "cp", rules, UdevPath });
                info.UseShellExecute = true;
                Process.Start(info);
            }

            if (!CheckDependencies())
            {
                // Pop open a dialog that it failed and to try again
            }
        }

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
                return usbDevice != null && usbDevice.Open();
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
    }
}
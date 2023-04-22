using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
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
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;
using ReactiveUI;
using Timer = System.Timers.Timer;

namespace GuitarConfigurator.NetCore.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen, IDisposable
    {
        private static readonly string UdevFile = "99-santroller.rules";
        private static readonly string UdevPath = $"/etc/udev/rules.d/{UdevFile}";
        private ConfigurableUsbDeviceManager _manager;
        private readonly ObservableAsPropertyHelper<bool> _connected;

        private readonly List<string> _currentDrives = new();
        private readonly HashSet<string> _currentDrivesTemp = new();
        private readonly List<string> _currentPorts = new();

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

        public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel> ShowYesNoDialog
        {
            get;
        }

        private bool _working = true;

        private static Func<DeviceInputType, bool> CreateFilter(IConfigurableDevice? s)
        {
            return type => type != DeviceInputType.Rf || s?.IsGeneric() != true;
        }

        public MainWindowViewModel()
        {
            ShowYesNoDialog =
                new Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>();
            AssetUtils.InitNativeLibrary();
            _allDeviceInputTypes.AddRange(Enum.GetValues<DeviceInputType>());
            _allDeviceInputTypes
                .Connect()
                .Filter(this.WhenAnyValue(s => s.SelectedDevice).Select(CreateFilter))
                .Bind(out var deviceInputTypes).Subscribe();
            DeviceInputTypes = deviceInputTypes;
            _manager = new ConfigurableUsbDeviceManager(this);
            ConfigureCommand = ReactiveCommand.CreateFromObservable(
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
        }

        public void Begin()
        {
            _timer.Elapsed += DevicePoller_Tick;
            _timer.AutoReset = false;
            StartWorking();
            Pio.InitialisePlatformIo().Subscribe(UpdateProgress, ex =>
            {
                Complete(100);
                ProgressbarColor = "red";
                Message = ex.Message;
            }, () =>
            {
                Complete(100);
                Working = false;
                Installed = true;
                _manager.Register();
                _timer.Start();
            });

            _ = InstallDependenciesAsync();
        }
        
        public ReactiveCommand<Unit, IRoutableViewModel> ConfigureCommand { get; }

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


        // The Router associated with this Screen.
        // Required by the IScreen interface.
        public RoutingState Router { get; } = new();

        internal IObservable<PlatformIo.PlatformIoState> Write(ConfigViewModel config)
        {
            StartWorking();
            config.Generate(Pio);
            var environment = config.Microcontroller.Board.Environment;
            if (config.UsingBluetooth() && config.IsPico)
            {
                environment = "picow";
            }

            var envs = new[] {environment};


            if (NewDevice)
            {
                envs[0] = envs[0].Replace("_8", "");
                envs[0] = envs[0].Replace("_16", "");
            }

            if (config.Microcontroller.Board.HasUsbmcu)
            {
                envs = new[] {envs[0] + "_usb", envs[0]};
            }

            ;
            var state = Observable.Return(new PlatformIo.PlatformIoState(0, "", null));
            var currentPercentage = 0;
            int endingPercentage = 90;
            if (config.Device.IsMini())
            {
                endingPercentage = 100;
            }

            var stepPercentage = endingPercentage / envs.Length;
            foreach (var env in envs)
            {
                Programming = true;
                var command = Pio.RunPlatformIo(env, new[] {"run", "--target", "upload"},
                    "Writing",
                    currentPercentage, currentPercentage + stepPercentage, config.Device);
                state = state.Concat(command);
                currentPercentage += stepPercentage;
            }

            var output = new StringBuilder();
            var behaviorSubject =
                new BehaviorSubject<PlatformIo.PlatformIoState>(new PlatformIo.PlatformIoState(0, "", null));
            state.ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
                {
                    behaviorSubject.OnNext(s);
                    UpdateProgress(s);
                    if (s.Log != null) output.Append(s.Log + "\n");
                }, _ =>
                {
                    ProgressbarColor = "red";
                    config.ShowIssueDialog.Handle((output.ToString(), config)).Subscribe(s => Programming = false);
                },
                () =>
                {
                    Programming = false;
                    if (config.Device.IsMini())
                    {
                        Working = false;
                    }
                });

            return state.OnErrorResumeNext(Observable.Return(behaviorSubject.Value));
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

        private async void DevicePoller_Tick(object? sender, ElapsedEventArgs e)
        {
            var drives = DriveInfo.GetDrives();
            _currentDrivesTemp.UnionWith(_currentDrives);
            foreach (var drive in drives)
            {
                if (_currentDrivesTemp.Remove(drive.RootDirectory.FullName)) continue;

                try
                {
                    var uf2 = Path.Combine(drive.RootDirectory.FullName, "INFO_UF2.txt");
                    if (drive.IsReady)
                        if (File.Exists(uf2) && (await File.ReadAllTextAsync(uf2)).Contains("RPI-RP2"))
                            AvailableDevices.Add(new PicoDevice(Pio, drive.RootDirectory.FullName));
                }
                catch (IOException)
                {
                    // Expected if the pico is unplugged   
                }

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
                    await Task.Delay(1000);
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


        private static bool CheckDependencies()
        {
            // Call check dependencies on startup, and pop up a dialog saying drivers are missing would you like to install if they are missing
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return DriverStore.ExistingDrivers.Any(s => s.Contains("atmel_usb_dfu"));
            }

            return !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || File.Exists(UdevPath);
        }

        private async Task InstallDependenciesAsync()
        {
            if (CheckDependencies()) return;
            var yesNo = await ShowYesNoDialog.Handle(("Install", "Skip",
                "There are some drivers missing, would you like to install them?")).ToTask();
            if (!yesNo.Response)
            {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var appdataFolder = AssetUtils.GetAppDataFolder();
                var driverFolder = Path.Combine(appdataFolder, "drivers");
                await AssetUtils.ExtractXzAsync("dfu.tar.xz", appdataFolder, _ => { });

                var info = new ProcessStartInfo(Path.Combine(windowsDir, "pnputil.exe"));
                info.ArgumentList.AddRange(new[] {"-i", "-a", Path.Combine(driverFolder, "atmel_usb_dfu.inf")});
                info.UseShellExecute = true;
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
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
                info.ArgumentList.AddRange(new[] {"cp", rules, UdevPath});
                info.UseShellExecute = true;
                Process.Start(info);
                
                // And then reload rules and trigger
                info = new ProcessStartInfo("pkexec");
                info.ArgumentList.AddRange(new[] {"udevadm", "control", "--reload-rules"});
                info.UseShellExecute = true;
                Process.Start(info);
                
                info = new ProcessStartInfo("pkexec");
                info.ArgumentList.AddRange(new[] {"udevadm", "trigger"});
                info.UseShellExecute = true;
                Process.Start(info);
            }

            if (!CheckDependencies())
            {
                // Pop open a dialog that it failed and to try again
            }
        }


        public void Dispose()
        {
            _manager.Dispose();
        }
    }
}
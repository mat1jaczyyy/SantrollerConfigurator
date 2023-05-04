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
using DynamicData;
using GuitarConfigurator.NetCore.Devices;
using Nefarius.Utilities.DeviceManagement.Drivers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Timer = System.Timers.Timer;

namespace GuitarConfigurator.NetCore.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen, IDisposable
    {
        private static readonly string UdevFile = "99-santroller.rules";
        private static readonly string UdevPath = $"/etc/udev/rules.d/{UdevFile}";
        private ConfigurableUsbDeviceManager _manager;

        private readonly List<string> _currentDrives = new();
        private readonly HashSet<string> _currentDrivesTemp = new();
        private readonly List<string> _currentPorts = new();

        private readonly Timer _timer = new();
        public bool Programming { get; private set; }


        private SourceList<DeviceInputType> _allDeviceInputTypes = new();
        public ReadOnlyObservableCollection<DeviceInputType> DeviceInputTypes { get; }

        public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel> ShowYesNoDialog
        {
            get;
        }

        private static Func<DeviceInputType, bool> CreateFilter(IConfigurableDevice? s)
        {
            return type => type != DeviceInputType.Rf || s?.IsGeneric() != true;
        }

        public MainWindowViewModel()
        {
            Message = "Connected";
            ProgressbarColor = "#FF0078D7";
            Working = true;
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
            Router.CurrentViewModel
                .Select(s => s is ConfigViewModel)
                .ToPropertyEx(this, s => s.HasSidebar);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s?.MigrationSupported != false)
                .ToPropertyEx(this, s => s.MigrationSupported);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s != null)
                .ToPropertyEx(this, s => s.Connected);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s?.IsPico() == true)
                .ToPropertyEx(this, s => s.IsPico);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Arduino arduino && arduino.Is32U4())
                .ToPropertyEx(this, s => s.Is32U4);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Dfu || (s is Arduino arduino && (arduino.IsUno() || arduino.IsMega())))
                .ToPropertyEx(this, s => s.IsUnoMega);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Arduino arduino && arduino.IsUno())
                .ToPropertyEx(this, s => s.IsUno);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Arduino arduino && arduino.IsMega())
                .ToPropertyEx(this, s => s.IsMega);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is Dfu)
                .ToPropertyEx(this, s => s.IsDfu);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s?.IsGeneric() == true)
                .ToPropertyEx(this, s => s.IsGeneric);
            this.WhenAnyValue(x => x.SelectedDevice)
                .Select(s => s is not (null or Ardwiino or Santroller))
                .ToPropertyEx(this, s => s.NewDevice);
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

        // ReSharper disable UnassignedGetOnlyAutoProperty
        [ObservableAsProperty] public bool MigrationSupported { get; }
        [ObservableAsProperty] public bool IsPico { get; }
        [ObservableAsProperty] public bool Is32U4 { get; }
        [ObservableAsProperty] public bool IsUno { get; }
        [ObservableAsProperty] public bool IsUnoMega { get; }
        [ObservableAsProperty] public bool IsMega { get; }
        [ObservableAsProperty] public bool IsDfu { get; }
        [ObservableAsProperty] public bool IsGeneric { get; }
        [ObservableAsProperty] public bool NewDevice { get; }

        [ObservableAsProperty] public bool Connected { get; }

        [ObservableAsProperty] public bool HasSidebar { get; }
        // ReSharper enable UnassignedGetOnlyAutoProperty

        public IEnumerable<Arduino32U4Type> Arduino32U4Types => Enum.GetValues<Arduino32U4Type>();
        public IEnumerable<MegaType> MegaTypes => Enum.GetValues<MegaType>();
        public IEnumerable<UnoMegaType> UnoMegaTypes => Enum.GetValues<UnoMegaType>();
        public IEnumerable<AvrType> AvrTypes => Enum.GetValues<AvrType>();
        public IEnumerable<Board> PicoTypes => Board.Rp2040Boards;

        [Reactive] public AvrType AvrType { get; set; }

        [Reactive] public UnoMegaType UnoMegaType { get; set; }

        [Reactive] public MegaType MegaType { get; set; }

        [Reactive] public DeviceInputType DeviceInputType { get; set; }

        [Reactive] public Arduino32U4Type Arduino32U4Type { get; set; }

        [Reactive] public Board PicoType { get; set; } = Board.Rp2040Boards[0];

        private IConfigurableDevice? _selectedDevice;

        public IConfigurableDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (value is Arduino arduino)
                {
                    UnoMegaType = arduino.Board.ArdwiinoName switch
                    {
                        "uno" => UnoMegaType.Uno,
                        "mega2560" => UnoMegaType.Mega,
                        "megaadk" => UnoMegaType.MegaAdk,
                        _ => UnoMegaType.Uno
                    };
                }

                this.RaiseAndSetIfChanged(ref _selectedDevice, value);
            }
        }


        [Reactive] public bool Working { get; set; }

        [Reactive] public bool Installed { get; set; }

        [Reactive] public string ProgressbarColor { get; set; }

        [Reactive] public bool ReadyToConfigure { get; set; }

        [Reactive] public double Progress { get; set; }

        [Reactive] public string Message { get; set; }

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

            if (config.Device is not (Ardwiino or Santroller))
            {
                environment = environment.Replace("_8", "");
                environment = environment.Replace("_16", "");
            }

            if (config.Microcontroller.Board.HasUsbmcu)
            {
                environment += "_usb";
            }

            ;
            var state = Observable.Return(new PlatformIo.PlatformIoState(0, "", null));
            int endingPercentage = 90;
            if (config.Device.IsMini())
            {
                endingPercentage = 100;
            }

            var env = environment;
            Programming = true;
            var command = Pio.RunPlatformIo(env, new[] {"run", "--target", "upload"},
                "Writing",
                0, endingPercentage, config.Device);
            state = state.Concat(command);


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
            if (!Working) return;
            ProgressbarColor = state.Message.Contains("Please unplug your device") ? "#FFd7cb00" : "#FF0078D7";
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
                            santroller.Disconnect();
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
                var process = Process.Start(info);
                if (process == null) return;
                await process.WaitForExitAsync();
                // And then reload rules and trigger
                info = new ProcessStartInfo("pkexec");
                info.ArgumentList.AddRange(new[] {"udevadm", "control", "--reload-rules"});
                info.UseShellExecute = true;
                process = Process.Start(info);
                if (process == null) return;
                await process.WaitForExitAsync();

                info = new ProcessStartInfo("pkexec");
                info.ArgumentList.AddRange(new[] {"udevadm", "trigger"});
                info.UseShellExecute = true;
                process = Process.Start(info);
                if (process == null) return;
                await process.WaitForExitAsync();
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
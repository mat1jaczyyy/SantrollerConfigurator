using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Media;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using ProtoBuf;
using ReactiveUI;
using CommunityToolkit.Mvvm.Input;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Other;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.ViewModels;

public partial class ConfigViewModel : ReactiveObject, IRoutableViewModel
{
    public static readonly string Apa102SpiType = "APA102";
    public static readonly string UsbHostPinTypeDm = "DM";
    public static readonly string UsbHostPinTypeDp = "DP";
    public static readonly string UnoPinTypeTx = "Uno Serial Tx Pin";
    public static readonly string UnoPinTypeRx = "Uno Serial Rx Pin";
    public static readonly int UnoPinTypeRxPin = 0;
    public static readonly int UnoPinTypeTxPin = 1;
    public IConfigurableDevice Device { get; private set; }

    public ReadOnlyObservableCollection<Output> Outputs { get; }


    private SpiConfig? _apa102SpiConfig;

    private DeviceControllerType _deviceControllerType;

    private EmulationType _emulationType;

    private LedType _ledType;

    private DirectPinConfig? _rfCe;

    private DirectPinConfig? _rfCsn;

    private DirectPinConfig? _usbHostDm;
    private DirectPinConfig? _usbHostDp;

    private SpiConfig? _rfSpiConfig;

    private RhythmType _rhythmType;

    private bool _usbHostEnabled;

    public ConfigViewModel(MainWindowViewModel screen, IConfigurableDevice device)
    {
        Device = device;
        Main = screen;
        Main.AvailableDevices.Connect().Subscribe(s =>
        {
            foreach (var change in s)
            {
                switch (change.Reason)
                {
                    case ListChangeReason.Add:
                        AddDevice(change.Item.Current);
                        break;
                    case ListChangeReason.Remove:
                        RemoveDevice(change.Item.Current);
                        break;
                }
            }
        });
        if (device is Santroller santroller)
        {
            LocalAddress = santroller.GetBluetoothAddress();
        }
        else
        {
            LocalAddress = "Write config to retrieve address";
        }

        HostScreen = screen;
        Microcontroller = device.GetMicrocontroller(this);
        ShowIssueDialog = new Interaction<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?>();
        ShowUnoShortDialog = new Interaction<Arduino, ShowUnoShortWindowViewModel?>();
        ShowYesNoDialog =
            new Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>();
        ShowBindAllDialog =
            new Interaction<(ConfigViewModel model, Output output, DirectInput
                input), BindAllWindowViewModel>();
        BindAllCommand = ReactiveCommand.CreateFromTask(BindAllAsync);

        WriteConfigCommand = ReactiveCommand.CreateFromObservable(Write,
            this.WhenAnyValue(x => x.Main.Working, x => x.Main.Connected, x => x.HasError)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: true, Item3: false}));
        GoBackCommand = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel?>(Main.GoBack.Execute,
            this.WhenAnyValue(x => x.Main.Working).Select(s => !s));

        this.WhenAnyValue(x => x.HasError)
            .Select(s => s ? "There are errors in your configuration" : null).ToPropertyEx(this, s => s.WriteToolTip);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Advanced)
            .ToPropertyEx(this, x => x.IsAdvancedMode);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Standard)
            .ToPropertyEx(this, x => x.IsStandardMode);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Core)
            .ToPropertyEx(this, x => x.IsRetailMode);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => x is EmulationType.RfController or EmulationType.RfKeyboardMouse)
            .ToPropertyEx(this, x => x.IsRf);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => x is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)
            .ToPropertyEx(this, x => x.IsBluetooth);
        this.WhenAnyValue(x => x.DeviceType)
            .Select(x => x is DeviceControllerType.Drum or DeviceControllerType.Guitar)
            .ToPropertyEx(this, x => x.IsRhythm);
        this.WhenAnyValue(x => x.DeviceType)
            .Select(x => x is DeviceControllerType.LiveGuitar or DeviceControllerType.Guitar)
            .ToPropertyEx(this, x => x.IsGuitar);
        this.WhenAnyValue(x => x.DeviceType)
            .Select(x => x is DeviceControllerType.StageKit)
            .ToPropertyEx(this, x => x.IsStageKit);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => GetSimpleEmulationTypeFor(x) is EmulationType.Controller)
            .ToPropertyEx(this, x => x.IsController);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => GetSimpleEmulationTypeFor(x) is EmulationType.KeyboardMouse)
            .ToPropertyEx(this, x => x.IsKeyboard);
        this.WhenAnyValue(x => x.LedType)
            .Select(x => x is LedType.APA102_BGR or LedType.APA102_BRG or LedType.APA102_GBR or LedType.APA102_GRB
                or LedType.APA102_RBG or LedType.APA102_RGB)
            .ToPropertyEx(this, x => x.IsApa102);
        Bindings.Connect()
            .Bind(out var outputs)
            .Subscribe();
        Outputs = outputs;

        if (!screen.SelectedDevice!.LoadConfiguration(this)) SetDefaults();
        if (Main is {IsUno: false, IsMega: false}) return;
        Microcontroller.AssignPin(new DirectPinConfig(this, UnoPinTypeRx, UnoPinTypeRxPin, DevicePinMode.Output));
        Microcontroller.AssignPin(new DirectPinConfig(this, UnoPinTypeTx, UnoPinTypeTxPin, DevicePinMode.Output));
    }

    public Interaction<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?> ShowIssueDialog { get; }

    public Interaction<Arduino, ShowUnoShortWindowViewModel?> ShowUnoShortDialog { get; }

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel> ShowYesNoDialog { get; }

    public Interaction<(ConfigViewModel model, Output output, DirectInput input),
            BindAllWindowViewModel>
        ShowBindAllDialog { get; }

    public ICommand BindAllCommand { get; }

    public MainWindowViewModel Main { get; }

    public IEnumerable<DeviceControllerType> DeviceControllerTypes => Enum.GetValues<DeviceControllerType>();
    public IEnumerable<RfPowerLevel> RfPowerLevels => Enum.GetValues<RfPowerLevel>();
    public IEnumerable<RfDataRate> RfDataRates => Enum.GetValues<RfDataRate>();

    public IEnumerable<RhythmType> RhythmTypes => Enum.GetValues<RhythmType>();
    public IEnumerable<ModeType> ModeTypes => Enum.GetValues<ModeType>();

    // Mini only supports RF
    // Only Pico supports bluetooth
    public IEnumerable<EmulationType> EmulationTypes => Enum.GetValues<EmulationType>()
        .Where(type =>
            Device.IsMini() && type is EmulationType.RfController or EmulationType.RfKeyboardMouse ||
            Device.IsPico() ||
            !Device.IsMini() && !Device.IsPico() &&
            type is not (EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse));

    public IEnumerable<LedType> LedTypes => Enum.GetValues<LedType>();

    public IEnumerable<MouseMovementType> MouseMovementTypes => Enum.GetValues<MouseMovementType>();

    //TODO: actually read and write this as part of the config
    public bool KvEnabled { get; set; } = false;
    public int[] KvKey1 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();
    public int[] KvKey2 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();

    public ICommand WriteConfigCommand { get; }

    public ICommand GoBackCommand { get; }

    public string LocalAddress { get; }

    [Reactive] public MouseMovementType MouseMovementType { get; set; }

    [Reactive] public ModeType Mode { get; set; }

    [Reactive] public int Debounce { get; set; }

    [Reactive] public int StrumDebounce { get; set; }

    [Reactive] public int PollRate { get; set; }

    public int Apa102Mosi
    {
        get => _apa102SpiConfig?.Mosi ?? 0;
        set => _apa102SpiConfig!.Mosi = value;
    }

    public int Apa102Sck
    {
        get => _apa102SpiConfig?.Sck ?? 0;
        set => _apa102SpiConfig!.Sck = value;
    }

    public int RfMosi
    {
        get => _rfSpiConfig?.Mosi ?? 0;
        set => _rfSpiConfig!.Mosi = value;
    }

    public int RfMiso
    {
        get => _rfSpiConfig?.Miso ?? 0;
        set => _rfSpiConfig!.Miso = value;
    }

    public int RfSck
    {
        get => _rfSpiConfig?.Sck ?? 0;
        set => _rfSpiConfig!.Sck = value;
    }

    public int RfCe
    {
        get => _rfCe?.Pin ?? 0;
        set => _rfCe!.Pin = value;
    }

    public int RfCsn
    {
        get => _rfCsn?.Pin ?? 0;
        set => _rfCsn!.Pin = value;
    }

    [Reactive] public RfPowerLevel PowerLevel { get; set; }

    [Reactive] public RfDataRate DataRate { get; set; }

    [Reactive] public bool RfModuleDetected { get; set; }

    [Reactive] public bool Connected { get; set; }


    public int UsbHostDm
    {
        get => _usbHostDm?.Pin ?? 0;
        set
        {
            _usbHostDm!.Pin = value;
            _usbHostDp!.Pin = value - 1;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UsbHostDp));
        }
    }

    public int UsbHostDp
    {
        get => _usbHostDp?.Pin ?? 0;
        set
        {
            _usbHostDp!.Pin = value;
            _usbHostDm!.Pin = value + 1;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UsbHostDm));
        }
    }

    [Reactive] public byte LedCount { get; set; }

    [Reactive] public byte WtSensitivity { get; set; }


    [Reactive] public byte RfId { get; set; }

    [Reactive] public byte RfChannel { get; set; }


    [Reactive] public bool HasError { get; set; }

    [Reactive] public bool Finalised { get; set; }

    public LedType LedType
    {
        get => _ledType;
        set
        {
            if (value == LedType.None)
            {
                Microcontroller.UnAssignPins(Apa102SpiType);
            }
            else if (_ledType == LedType.None)
            {
                var pins = Microcontroller.FreeSpiPins(Apa102SpiType);
                var mosi = pins.First(pair => pair.Value is SpiPinType.Mosi).Key;
                var sck = pins.First(pair => pair.Value is SpiPinType.Sck).Key;
                _apa102SpiConfig = Microcontroller.AssignSpiPins(this, Apa102SpiType, mosi, -1, sck, true, true,
                    true,
                    Math.Min(Microcontroller.Board.CpuFreq / 2, 12000000))!;
                this.RaisePropertyChanged(nameof(Apa102Mosi));
                this.RaisePropertyChanged(nameof(Apa102Sck));
                UpdateErrors();
            }

            this.RaiseAndSetIfChanged(ref _ledType, value);
        }
    }

    [Reactive] public bool XInputOnWindows { get; set; }

    public bool UsbHostEnabled
    {
        get => _usbHostEnabled;
        set
        {
            if (!IsPico) return;
            this.RaiseAndSetIfChanged(ref _usbHostEnabled, value);
            if (value)
            {
                // These pins get handled by the usb host lib, but we need them defined regardless
                _usbHostDp = Microcontroller.GetOrSetPin(this, UsbHostPinTypeDp, AvailablePinsDp.First(),
                    DevicePinMode.Skip);
                _usbHostDm = Microcontroller.GetOrSetPin(this, UsbHostPinTypeDm, AvailablePinsDm.First(),
                    DevicePinMode.Skip);
                this.RaisePropertyChanged(nameof(UsbHostDp));
                this.RaisePropertyChanged(nameof(UsbHostDm));
            }
            else
            {
                if (_usbHostDp != null)
                {
                    Microcontroller.UnAssignPins(_usbHostDp.Type);
                    _usbHostDp = null;
                }

                if (_usbHostDm == null) return;
                Microcontroller.UnAssignPins(_usbHostDm.Type);
                _usbHostDm = null;
            }

            UpdateErrors();
        }
    }


    public DeviceControllerType DeviceType
    {
        get => _deviceControllerType;
        set
        {
            this.RaiseAndSetIfChanged(ref _deviceControllerType, value);
            UpdateBindings();
        }
    }

    public EmulationType EmulationType
    {
        get => _emulationType;
        set => _ = SetDefaultBindingsAsync(value);
    }

    public RhythmType RhythmType
    {
        get => _rhythmType;
        set
        {
            this.RaiseAndSetIfChanged(ref _rhythmType, value);
            UpdateBindings();
        }
    }

    public Microcontroller Microcontroller { get; }

    public SourceList<Output> Bindings { get; } = new();
    public bool BindableSpi => IsPico;

    // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool IsStandardMode { get; }
    [ObservableAsProperty] public bool IsAdvancedMode { get; }
    [ObservableAsProperty] public bool IsRetailMode { get; }

    [ObservableAsProperty] public bool IsRhythm { get; }
    [ObservableAsProperty] public bool IsGuitar { get; }
    [ObservableAsProperty] public bool IsStageKit { get; }
    [ObservableAsProperty] public bool IsController { get; }
    [ObservableAsProperty] public bool IsKeyboard { get; }
    [ObservableAsProperty] public bool IsApa102 { get; }
    [ObservableAsProperty] public bool IsRf { get; }
    [ObservableAsProperty] public bool IsBluetooth { get; }

    [ObservableAsProperty] public string? WriteToolTip { get; }
    // ReSharper enable UnassignedGetOnlyAutoProperty

    public List<int> AvailableApaMosiPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Mosi)
        .Select(s => s.Key).ToList();

    public List<int> AvailableApaMisoPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Miso)
        .Select(s => s.Key).ToList();

    public List<int> AvailableApaSckPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Sck)
        .Select(s => s.Key).ToList();

    public List<int> AvailableRfMosiPins => Microcontroller.SpiPins(RfRxOutput.SpiType)
        .Where(s => s.Value is SpiPinType.Mosi)
        .Select(s => s.Key).ToList();

    public List<int> AvailableRfMisoPins => Microcontroller.SpiPins(RfRxOutput.SpiType)
        .Where(s => s.Value is SpiPinType.Miso)
        .Select(s => s.Key).ToList();

    public List<int> AvailableRfSckPins => Microcontroller.SpiPins(RfRxOutput.SpiType)
        .Where(s => s.Value is SpiPinType.Sck)
        .Select(s => s.Key).ToList();

    public List<int> AvailablePins => Microcontroller.GetAllPins(false);

    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => AvailablePins.Skip(1).ToList();
    public List<int> AvailablePinsDp => AvailablePins.SkipLast(1).ToList();

    public IEnumerable<PinConfig> PinConfigs =>
        new PinConfig?[] {_apa102SpiConfig, _rfSpiConfig, _usbHostDm, _usbHostDp}.Where(s => s != null)
            .Cast<PinConfig>();

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString()[..5];

    public IScreen HostScreen { get; }
    public bool IsPico => Device.IsPico();

    public void SetDeviceTypeAndRhythmTypeWithoutUpdating(DeviceControllerType type, RhythmType rhythmType,
        EmulationType emulationType)
    {
        this.RaiseAndSetIfChanged(ref _deviceControllerType, type, nameof(DeviceType));
        this.RaiseAndSetIfChanged(ref _rhythmType, rhythmType, nameof(RhythmType));
        this.RaiseAndSetIfChanged(ref _emulationType, emulationType, nameof(EmulationType));
        if (_rfSpiConfig == null && IsRf)
        {
            var pins = Microcontroller.SpiPins(RfRxOutput.SpiType);
            var mosi = pins.First(pair => pair.Value is SpiPinType.Mosi).Key;
            var miso = pins.First(pair => pair.Value is SpiPinType.Miso).Key;
            var sck = pins.First(pair => pair.Value is SpiPinType.Sck).Key;
            _rfSpiConfig = Microcontroller.AssignSpiPins(this, RfRxOutput.SpiType, mosi, miso, sck, true, true,
                true,
                4000000);
            this.RaisePropertyChanged(nameof(RfMiso));
            this.RaisePropertyChanged(nameof(RfMosi));
            this.RaisePropertyChanged(nameof(RfSck));
            var first = Microcontroller.GetAllPins(false).First();
            _rfCe = Microcontroller.GetOrSetPin(this, RfRxOutput.SpiType + "_ce", first, DevicePinMode.PullUp);
            _rfCsn = Microcontroller.GetOrSetPin(this, RfRxOutput.SpiType + "_csn", first, DevicePinMode.Output);
        }
    }

    public void UpdateBindings()
    {
        foreach (var binding in Bindings.Items) binding.UpdateBindings();
        InstrumentButtonTypeExtensions.ConvertBindings(Bindings, this);

        var (extra, types) =
            ControllerEnumConverter.FilterValidOutputs(_deviceControllerType, _rhythmType, Bindings.Items);
        Bindings.RemoveMany(extra);

        // If the user has a ps2 or wii combined output mapped, they don't need the default bindings
        if (Bindings.Items.Any(s => s is WiiCombinedOutput or Ps2CombinedOutput or RfRxOutput)) return;

        if (_deviceControllerType is not (DeviceControllerType.Guitar or DeviceControllerType.Drum))
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode {Type: EmulationModeType.Wii}));
        }

        if (_deviceControllerType is DeviceControllerType.Turntable)
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode
            {
                Type: EmulationModeType.Ps4Or5 or EmulationModeType.XboxOne
            }));
        }

        if (_deviceControllerType == DeviceControllerType.Drum)
        {
            IEnumerable<DrumAxisType> difference = DrumAxisTypeMethods.GetDifferenceFor(_rhythmType).ToHashSet();
            Bindings.RemoveMany(Bindings.Items.Where(s => s is DrumAxis axis && difference.Contains(axis.Type)));
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is DrumAxis));
        }

        if (_deviceControllerType is DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar)
        {
            IEnumerable<GuitarAxisType> difference = GuitarAxisTypeMethods
                .GetDifferenceFor(_rhythmType, _deviceControllerType).ToHashSet();
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarAxis axis && difference.Contains(axis.Type)));
        }
        else
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarAxis));
        }

        if (_deviceControllerType is not DeviceControllerType.Guitar && _rhythmType is not RhythmType.RockBand)
        {
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode {Type: EmulationModeType.Wii}));
        }

        if (_deviceControllerType is not (DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar))
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarButton));

        if (_deviceControllerType == DeviceControllerType.Turntable)
            if (!Bindings.Items.Any(s => s is DjCombinedOutput))
                Bindings.Add(new DjCombinedOutput(this));

        foreach (var type in types)
            switch (type)
            {
                case StandardButtonType buttonType:
                    Bindings.Add(new ControllerButton(this,
                        new DirectInput(Microcontroller.GetFirstDigitalPin(), DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, buttonType));
                    break;
                case InstrumentButtonType buttonType:
                    Bindings.Add(new GuitarButton(this,
                        new DirectInput(Microcontroller.GetFirstDigitalPin(), DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, buttonType));
                    break;
                case StandardAxisType axisType:
                    Bindings.Add(new ControllerAxis(this,
                        new DirectInput(Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue,
                        0, axisType));
                    break;
                case GuitarAxisType axisType:
                    Bindings.Add(new GuitarAxis(this, new DirectInput(Microcontroller.GetFirstAnalogPin(),
                            DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue,
                        0, axisType));
                    break;
                case DrumAxisType axisType:
                    Bindings.Add(new DrumAxis(this,
                        new DirectInput(Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue,
                        0, 64, 10, axisType));
                    break;
                case DjAxisType axisType:
                    if (axisType is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity) continue;
                    Bindings.Add(new DjAxis(this,
                        new DirectInput(Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue,
                        0, axisType));
                    break;
            }
    }


    public IObservable<PlatformIo.PlatformIoState> Write()
    {
        return Main.Write(this);
    }

    public void SetDefaults()
    {
        ClearOutputs();
        LedType = LedType.None;
        _deviceControllerType = DeviceControllerType.Gamepad;
        WtSensitivity = 30;
        _usbHostEnabled = false;
        PollRate = 0;
        StrumDebounce = 0;
        Debounce = 5;
        if (Device.IsMini())
        {
            _emulationType = EmulationType.RfController;
            if (_rfSpiConfig == null)
            {
                var pins = Microcontroller.SpiPins(RfRxOutput.SpiType);
                var mosi = pins.First(pair => pair.Value is SpiPinType.Mosi).Key;
                var miso = pins.First(pair => pair.Value is SpiPinType.Miso).Key;
                var sck = pins.First(pair => pair.Value is SpiPinType.Sck).Key;
                _rfSpiConfig = Microcontroller.AssignSpiPins(this, RfRxOutput.SpiType, mosi, miso, sck, true, true,
                    true,
                    4000000);
                this.RaisePropertyChanged(nameof(RfMiso));
                this.RaisePropertyChanged(nameof(RfMosi));
                this.RaisePropertyChanged(nameof(RfSck));
                var first = Microcontroller.GetAllPins(false).First();
                _rfCe = Microcontroller.GetOrSetPin(this, RfRxOutput.SpiType + "_ce", first, DevicePinMode.PullUp);
                _rfCsn = Microcontroller.GetOrSetPin(this, RfRxOutput.SpiType + "_csn", first, DevicePinMode.Output);
            }
        }

        _rhythmType = RhythmType.GuitarHero;
        this.RaisePropertyChanged(nameof(DeviceType));
        this.RaisePropertyChanged(nameof(EmulationType));
        this.RaisePropertyChanged(nameof(RhythmType));
        XInputOnWindows = true;
        MouseMovementType = MouseMovementType.Relative;
        switch (Main.DeviceInputType)
        {
            case DeviceInputType.Direct:
                _ = SetDefaultBindingsAsync(EmulationType);
                break;
            case DeviceInputType.Wii:
                Bindings.Add(new WiiCombinedOutput(this));
                break;
            case DeviceInputType.Ps2:
                Bindings.Add(new Ps2CombinedOutput(this));
                break;
            case DeviceInputType.Rf:
                Bindings.Add(new RfRxOutput(this, 0, 1, RfPowerLevel.Min, RfDataRate.One));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (Main.IsUno || Main.IsMega)
        {
            Write();
            _ = ShowUnoShortDialog.Handle((Arduino) Device).ToTask();
            return;
        }

        UpdateBindings();
        UpdateErrors();

        Write();
    }

    private async Task SetDefaultBindingsAsync(EmulationType emulationType)
    {
        if (emulationType is EmulationType.RfController or EmulationType.RfKeyboardMouse)
        {
            if (_rfSpiConfig == null)
            {
                var pins = Microcontroller.SpiPins(RfRxOutput.SpiType);
                var mosi = pins.First(pair => pair.Value is SpiPinType.Mosi).Key;
                var miso = pins.First(pair => pair.Value is SpiPinType.Miso).Key;
                var sck = pins.First(pair => pair.Value is SpiPinType.Sck).Key;
                _rfSpiConfig = Microcontroller.AssignSpiPins(this, RfRxOutput.SpiType, mosi, miso, sck, true, true,
                    true,
                    4000000);
                var first = Microcontroller.GetAllPins(false).First();
                _rfCe = Microcontroller.GetOrSetPin(this, RfRxOutput.SpiType + "_ce", first, DevicePinMode.PullUp);
                _rfCsn = Microcontroller.GetOrSetPin(this, RfRxOutput.SpiType + "_csn", first, DevicePinMode.Output);

                this.RaisePropertyChanged(nameof(RfMiso));
                this.RaisePropertyChanged(nameof(RfMosi));
                this.RaisePropertyChanged(nameof(RfSck));
                this.RaisePropertyChanged(nameof(RfCe));
                this.RaisePropertyChanged(nameof(RfCsn));
            }
        }
        else
        {
            if (_rfSpiConfig != null)
            {
                Microcontroller.UnAssignPins(_rfSpiConfig.Type);
                _rfSpiConfig = null;
            }

            if (_rfCe != null)
            {
                Microcontroller.UnAssignPins(_rfCe.Type);
                _rfCe = null;
            }

            if (_rfCsn != null)
            {
                Microcontroller.UnAssignPins(_rfCsn.Type);
                _rfCsn = null;
            }
        }

        // If going from say bluetooth controller to standard controller, the pin bindings can stay
        if (GetSimpleEmulationTypeFor(EmulationType) == GetSimpleEmulationTypeFor(emulationType))
        {
            _emulationType = emulationType;
            this.RaisePropertyChanged(nameof(EmulationType));
            UpdateErrors();
            return;
        }

        if (Bindings.Items.Any())
        {
            var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
                "The following action will clear all your bindings, are you sure you want to do this?")).ToTask();
            if (!yesNo.Response)
            {
                var last = _emulationType;
                _emulationType = emulationType;
                this.RaisePropertyChanged(nameof(EmulationType));
                _emulationType = last;
                this.RaisePropertyChanged(nameof(EmulationType));
                return;
            }
        }

        _emulationType = emulationType;
        this.RaisePropertyChanged(nameof(EmulationType));
        ClearOutputs();

        if (GetSimpleEmulationType() is EmulationType.KeyboardMouse)
        {
            return;
        }

        foreach (var type in Enum.GetValues<StandardAxisType>())
        {
            if (!ControllerEnumConverter.GetAxisText(_deviceControllerType, type).Any()) continue;
            if (DeviceType == DeviceControllerType.Turntable &&
                type is StandardAxisType.LeftStickX or StandardAxisType.LeftStickY) continue;
            Bindings.Add(new ControllerAxis(this,
                new DirectInput(Microcontroller.GetFirstAnalogPin(), DevicePinMode.Analog, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), short.MinValue, short.MaxValue, 0,
                type));
        }

        foreach (var type in Enum.GetValues<StandardButtonType>())
        {
            if (ControllerEnumConverter.GetButtonText(_deviceControllerType, type) ==
                null) continue;
            Bindings.Add(new ControllerButton(this,
                new DirectInput(Microcontroller.GetFirstDigitalPin(), DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 1, type));
        }

        UpdateErrors();
    }

    public void Finalise()
    {
        Finalised = true;
    }

    public void Generate(PlatformIo pio)
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.Outputs.Items).ToList();
        var inputs = outputs.Select(binding => binding.Input.InnermostInput()).ToList();
        var directInputs = inputs.OfType<DirectInput>().ToList();
        var configFile = Path.Combine(pio.FirmwareDir, "include", "config_data.h");
        var lines = new List<string>();

        using (var outputStream = new MemoryStream())
        {
            using (var compressStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
            {
                Serializer.Serialize(compressStream, new SerializedConfiguration(this));
            }

            lines.Add(
                $"#define CONFIGURATION {{{string.Join(",", outputStream.ToArray().Select(b => "0x" + b.ToString("X")))}}}");
            lines.Add($"#define CONFIGURATION_LEN {outputStream.ToArray().Length}");
        }


        lines.Add($"#define WINDOWS_USES_XINPUT {XInputOnWindows.ToString().ToLower()}");
        lines.Add($"#define USB_HOST_STACK {UsbHostEnabled.ToString().ToLower()}");
        lines.Add($"#define USB_HOST_DP_PIN {UsbHostDp}");

        lines.Add(
            $"#define ABSOLUTE_MOUSE_COORDS {(MouseMovementType == MouseMovementType.Absolute).ToString().ToLower()}");

        lines.Add($"#define TICK_SHARED {GenerateTick(ConfigField.Shared)}");
        lines.Add($"#define TICK_DETECTION {GenerateTick(ConfigField.Detection)}");

        lines.Add($"#define TICK_PS3 {GenerateTick(ConfigField.Ps3)}");

        lines.Add($"#define TICK_PS4 {GenerateTick(ConfigField.Ps4)}");

        lines.Add($"#define TICK_XINPUT {GenerateTick(ConfigField.Xbox360)}");

        lines.Add($"#define TICK_XBOX_ONE {GenerateTick(ConfigField.XboxOne)}");

        var nkroTick = GenerateTick(ConfigField.Keyboard);
        if (nkroTick.Any()) lines.Add($"#define TICK_NKRO {nkroTick}");

        var consumerTick = GenerateTick(ConfigField.Consumer);
        if (consumerTick.Any()) lines.Add($"#define TICK_CONSUMER {consumerTick}");

        var mouseTick = GenerateTick(ConfigField.Mouse);
        if (mouseTick.Any()) lines.Add($"#define TICK_MOUSE {mouseTick}");


        lines.Add($"#define DIGITAL_COUNT {CalculateDebounceTicks()}");
        lines.Add($"#define LED_COUNT {LedCount}");
        lines.Add($"#define WT_SENSITIVITY {WtSensitivity}");

        lines.Add($"#define LED_TYPE {GetLedType()}");

        if (IsApa102)
        {
            lines.Add($"#define {Apa102SpiType.ToUpper()}_SPI_PORT {_apa102SpiConfig!.Definition}");

            lines.Add($"#define TICK_LED {GenerateLedTick()}");
        }

        if (IsRf)
        {
            lines.Add($"#define RF_DEVICE_ID {RfId}");
            lines.Add($"#define RF_CHANNEL {RfChannel}");
            lines.Add("#define RF_TX");
            lines.Add($"#define RADIO_CE {_rfCe!.Pin}");
            lines.Add($"#define RADIO_CSN {_rfCsn!.Pin}");
            lines.Add($"#define RF_POWER_LEVEL {(byte) PowerLevel}");
            lines.Add($"#define RF_DATA_RATE {(byte) DataRate}");
            if (BindableSpi)
            {
                lines.Add($"#define RADIO_MOSI {_rfSpiConfig!.Mosi}");
                lines.Add($"#define RADIO_MISO {_rfSpiConfig!.Miso}");
                lines.Add($"#define RADIO_SCK {_rfSpiConfig!.Sck}");
            }
        }

        lines.Add($"#define HANDLE_AUTH_LED {GenerateTick(ConfigField.AuthLed)}");

        var offLed = GenerateTick(ConfigField.OffLed);
        if (offLed.Any())
        {
            lines.Add($"#define HANDLE_LED_RUMBLE_OFF {offLed}");
        }

        lines.Add($"#define HANDLE_PLAYER_LED {GenerateTick(ConfigField.PlayerLed)}");

        lines.Add($"#define HANDLE_LIGHTBAR_LED {GenerateTick(ConfigField.LightBarLed)}");

        lines.Add($"#define HANDLE_RUMBLE {GenerateTick(ConfigField.RumbleLed)}");

        lines.Add($"#define HANDLE_KEYBOARD_LED {GenerateTick(ConfigField.KeyboardLed)}");

        lines.Add($"#define CONSOLE_TYPE {GetEmulationType()}");

        lines.Add($"#define DEVICE_TYPE {(byte) DeviceType}");

        lines.Add($"#define POLL_RATE {PollRate}");

        lines.Add($"#define RHYTHM_TYPE {(byte) RhythmType}");
        if (EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)
        {
            lines.Add(
                $"#define BLUETOOTH_TX {(EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse).ToString().ToLower()}");
        }

        if (KvEnabled)
        {
            lines.Add(
                $"#define KV_KEY_1 {{{string.Join(",", KvKey1.ToArray().Select(b => "0x" + b.ToString("X")))}}}");
            lines.Add(
                $"#define KV_KEY_2 {{{string.Join(",", KvKey2.ToArray().Select(b => "0x" + b.ToString("X")))}}}");
        }

        lines.Add(Ps2Input.GeneratePs2Pressures(inputs));
        
        // Sort by pin index, and then map to adc number and turn into an array
        var analogPins = directInputs.Where(s => s.IsAnalog).OrderBy(s => s.PinConfig.Pin)
            .Select(s => Microcontroller.GetChannel(s.PinConfig.Pin, false).ToString()).Distinct().ToList();
        // Format as a c array
        lines.Add($"#define ADC_PINS {{{string.Join(",", analogPins)}}}");
        
        // And also store a count
        lines.Add($"#define ADC_COUNT {analogPins.Count}");
        lines.Add($"#define PIN_INIT {Microcontroller.GenerateInit()}");

        lines.Add(Microcontroller.GenerateDefinitions());

        lines.Add($"#define ARDWIINO_BOARD \"{Microcontroller.Board.ArdwiinoName}\"");
        lines.Add(string.Join("\n",
            inputs.SelectMany(input => input.RequiredDefines()).Distinct().Select(define => $"#define {define}")));

        File.WriteAllLines(configFile, lines);
    }

    private byte GetEmulationType()
    {
        return (byte) GetSimpleEmulationType();
    }

    private EmulationType GetSimpleEmulationTypeFor(EmulationType type)
    {
        switch (type)
        {
            case EmulationType.Bluetooth:
            case EmulationType.Controller:
            case EmulationType.RfController:
                return EmulationType.Controller;
            case EmulationType.KeyboardMouse:
            case EmulationType.BluetoothKeyboardMouse:
            case EmulationType.RfKeyboardMouse:
                return EmulationType.KeyboardMouse;
            default:
                return EmulationType;
        }
    }

    public EmulationType GetSimpleEmulationType()
    {
        return GetSimpleEmulationTypeFor(EmulationType);
    }

    private int GetLedType()
    {
        switch (LedType)
        {
            case LedType.APA102_RGB:
            case LedType.APA102_RBG:
            case LedType.APA102_GRB:
            case LedType.APA102_GBR:
            case LedType.APA102_BRG:
            case LedType.APA102_BGR:
                return 1;
            case LedType.None:
            default:
                return 0;
        }
    }

    private async Task BindAllAsync()
    {
        foreach (var binding in Bindings.Items)
        {
            if (binding.Input.InnermostInput() is not DirectInput direct) continue;
            var response = await ShowBindAllDialog.Handle((this, binding, direct)).ToTask();
            if (!response.Response) return;
        }
    }

    public void RemoveOutput(Output output)
    {
        if (Bindings.Remove(output))
        {
            output.Dispose();
            UpdateErrors();
            return;
        }

        foreach (var binding in Bindings.Items) binding.Outputs.Remove(output);
        output.Dispose();

        UpdateErrors();
    }

    [RelayCommand]
    public void ClearOutputs()
    {
        foreach (var binding in Bindings.Items) binding.Dispose();

        Bindings.Clear();
        UpdateErrors();
    }

    public async Task ClearOutputsWithConfirmationAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
            "The following action will clear all your inputs, are you sure you want to do this?")).ToTask();
        if (!yesNo.Response) return;

        foreach (var binding in Bindings.Items) binding.Dispose();

        Bindings.Clear();
        UpdateErrors();
    }

    [RelayCommand]
    public void ExpandAll()
    {
        foreach (var binding in Bindings.Items) binding.Expanded = true;
    }

    [RelayCommand]
    public void CollapseAll()
    {
        foreach (var binding in Bindings.Items) binding.Expanded = false;
    }

    [RelayCommand]
    public async Task ResetWithConfirmationAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
            "The following action will clear all your inputs, are you sure you want to do this?")).ToTask();
        if (!yesNo.Response) return;

        foreach (var binding in Bindings.Items) binding.Dispose();

        Bindings.Clear();
        UpdateBindings();
        UpdateErrors();
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Reset", "Cancel",
                "The following action will revert your device back to an Arduino, are you sure you want to do this?"))
            .ToTask();
        if (!yesNo.Response) return;
        //TODO: actually revert the device to an arduino
    }

    [RelayCommand]
    public void AddOutput()
    {
        if (IsController)
            Bindings.Add(new EmptyOutput(this));
        else if (IsKeyboard)
            Bindings.Add(new KeyboardButton(this, new DirectInput(0, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 1, Key.Space));

        UpdateErrors();
    }

    private string GenerateLedTick()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        if (_ledType == LedType.None ||
            !outputs.Any(s => s.LedIndices.Any())) return "";
        var ledMax = outputs.SelectMany(output => output.LedIndices).Max();
        var ret =
            "spi_transfer(APA102_SPI_PORT, 0x00);spi_transfer(APA102_SPI_PORT, 0x00);spi_transfer(APA102_SPI_PORT, 0x00);spi_transfer(APA102_SPI_PORT, 0x00);";
        for (var i = 0; i < ledMax; i++)
            ret +=
                $"spi_transfer(APA102_SPI_PORT, 0xff);spi_transfer(APA102_SPI_PORT, ledState[{i}].r);spi_transfer(APA102_SPI_PORT, ledState[{i}].g);spi_transfer(APA102_SPI_PORT, ledState[{i}].b);";

        for (var i = 0; i <= ledMax; i += 16) ret += "spi_transfer(APA102_SPI_PORT, 0xff);";

        return GenerateTick(ConfigField.StrobeLed) + ret.Replace('\r', ' ').Replace('\n', ' ');
    }

    private string GenerateTick(ConfigField mode)
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        var groupedOutputs = outputs
            .SelectMany(s =>
                s.Input.Inputs().Zip(Enumerable.Repeat(s, s.Input.Inputs().Count)))
            .GroupBy(s => s.First.InnermostInput().GetType()).ToList();
        var combined = DeviceType is DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar &&
                       StrumDebounce > 0;
        Dictionary<string, int> debounces = new();
        var strumIndices = new List<int>();

        // Pass 1: work out debounces and map inputs to debounces
        var inputs = new Dictionary<string, List<int>>();
        var macros = new List<Output>();
        foreach (var groupedOutput in groupedOutputs)
        {
            foreach (var (input, output) in groupedOutput)
            {
                var generatedInput = input.Generate(mode);
                if (output is not OutputButton and not DrumAxis and not EmulationMode) continue;

                if (output.Input.InnermostInput() is MacroInput)
                {
                    if (!debounces.ContainsKey(generatedInput))
                        debounces[generatedInput] = debounces.Count;

                    macros.Add(output);
                }
                else
                {
                    if (!debounces.ContainsKey(generatedInput)) debounces[generatedInput] = debounces.Count;
                }

                if (combined && output is GuitarButton
                    {
                        Type: InstrumentButtonType.StrumUp or InstrumentButtonType.StrumDown
                    })
                {
                    strumIndices.Add(debounces[generatedInput]);
                }

                if (!inputs.ContainsKey(generatedInput)) inputs[generatedInput] = new List<int>();

                inputs[generatedInput].Add(debounces[generatedInput]);
            }
        }

        var seen = new HashSet<Output>();
        var debouncesRelatedToLed = new Dictionary<byte, List<(Output, List<int>)>>();
        // Handle most mappings
        // Sort in a way that any digital to analog based groups are last. This is so that seenAnalog will be filled in when necessary.
        var ret = groupedOutputs.OrderByDescending(s => s.Count(s2 => s2.Second.Input is DigitalToAnalog))
            .Aggregate("", (current, group) =>
            {
                // we need to ensure that DigitalToAnalog is last
                return current + group
                    .First().First.InnermostInput()
                    .GenerateAll(Bindings.Items.ToList(), group
                        .OrderByDescending(s => s.First is DigitalToAnalog ? 0 : 1)
                        .Select(s =>
                        {
                            var input = s.First;
                            var output = s.Second;
                            var generatedInput = input.Generate(mode);
                            var index = new List<int> {0};
                            if (output is OutputButton or DrumAxis or EmulationMode)
                            {
                                index = new List<int> {debounces[generatedInput]};
                                if (output.Input is MacroInput)
                                {
                                    if (mode == ConfigField.Shared)
                                    {
                                        output = output.Serialize().Generate(this);
                                        output.Input = input;
                                    }
                                    else
                                    {
                                        if (seen.Contains(output)) return new Tuple<Input, string>(input, "");
                                        seen.Add(output);
                                        index = output.Input.Inputs()
                                            .Select(input1 => debounces[input1.Generate(mode)])
                                            .ToList();
                                    }
                                }

                                foreach (var led in output.LedIndices)
                                {
                                    if (!debouncesRelatedToLed.ContainsKey(led))
                                    {
                                        debouncesRelatedToLed[led] = new List<(Output, List<int>)>();
                                    }

                                    debouncesRelatedToLed[led].Add((output, index));
                                }
                            }

                            var generated = output.Generate(mode, index, "", "", strumIndices);
                            return new Tuple<Input, string>(input, generated);
                        })
                        .Where(s => !string.IsNullOrEmpty(s.Item2))
                        .Distinct().ToList(), mode);
            });
        // Flick off intersecting outputs when multiple buttons are pressed
        if (mode == ConfigField.Shared)
        {
            foreach (var output in macros)
            {
                var generatedInput = output.Input.Generate(mode);
                var ifStatement = string.Join(" && ",
                    output.Input.Inputs().Select(input =>
                        $"debounce[{debounces[generatedInput]}]"));
                var sharedReset = output.Input.Inputs().Aggregate("",
                    (current, input) => current + string.Join("",
                        inputs[input.Generate(mode)].Select(s => $"debounce[{s}]=0;").Distinct()));
                ret += @$"if ({ifStatement}) {{{sharedReset}}}";
            }

            if (LedType is not LedType.None)
            {
                // Handle leds, including when multiple leds are assigned to a single output.
                foreach (var (led, relatedOutputs) in debouncesRelatedToLed)
                {
                    ret += $"if (ledState[{led - 1}].select == 0) {{";
                    ret += string.Join(" else ", relatedOutputs.Select(tuple =>
                    {
                        var ifStatement = string.Join(" && ", tuple.Item2.Select(x => $"debounce[{x}]"));
                        return @$"if ({ifStatement}) {{
                                        {LedType.GetLedAssignment(tuple.Item1.LedOn, led)}
                                       }}";
                    }));
                    ret += $@" else {{
                        {LedType.GetLedAssignment(relatedOutputs.First().Item1.LedOff, led)}
                    }}
                }}";
                }
            }
        }

        return ret.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }

    private int CalculateDebounceTicks()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        var groupedOutputs = outputs
            .SelectMany(s =>
                s.Input.Inputs().Zip(Enumerable.Repeat(s, s.Input.Inputs().Count)))
            .GroupBy(s => s.First.InnermostInput().GetType()).ToList();
        var combined = DeviceType == DeviceControllerType.Guitar && StrumDebounce > 0;

        Dictionary<string, int> debounces = new();
        if (combined)
            foreach (var output in outputs.Where(output => output.IsStrum))
                debounces[output.LocalisedName] = debounces.Count;

        foreach (var groupedOutput in groupedOutputs)
        foreach (var (input, output) in groupedOutput)
        {
            var generatedInput = input.Generate(ConfigField.Xbox360);
            if (output is not OutputButton and not DrumAxis and not EmulationMode) continue;

            if (output.Input is MacroInput)
            {
                if (!debounces.ContainsKey(generatedInput))
                    debounces[generatedInput] = debounces.Count;
            }
            else
            {
                if (!debounces.ContainsKey(generatedInput)) debounces[generatedInput] = debounces.Count;
            }
        }

        return debounces.Count;
    }

    public bool IsCombinedChild(Output output)
    {
        return !Bindings.Items.Contains(output);
    }

    public Dictionary<string, List<int>> GetPins(string type)
    {
        var pins = new Dictionary<string, List<int>>();
        foreach (var binding in Bindings.Items)
        {
            var configs = binding.GetPinConfigs();
            //Exclude digital or analog pins (which use a guid containing a -
            if (configs.Any(s => s.Type == type || (type.Contains("-") && s.Type.Contains("-")))) continue;
            if (!pins.ContainsKey(binding.LocalisedName)) pins[binding.LocalisedName] = new List<int>();

            foreach (var pinConfig in configs) pins[binding.LocalisedName].AddRange(pinConfig.Pins);
        }

        if (IsApa102 && _apa102SpiConfig != null) pins["APA102"] = _apa102SpiConfig.Pins.ToList();
        if (Main.IsUno || Main.IsMega)
        {
            pins[UnoPinTypeTx] = new List<int> {UnoPinTypeTxPin};
            pins[UnoPinTypeRx] = new List<int> {UnoPinTypeRxPin};
        }

        if (UsbHostEnabled)
        {
            pins["USB Host"] = new List<int> {UsbHostDm, UsbHostDp};
        }

        if (IsRf)
        {
            pins["RF"] = new List<int> {RfMiso, RfMosi, RfCe, RfSck, RfCsn};
        }

        return pins;
    }

    public void UpdateErrors()
    {
        var foundError = false;
        foreach (var output in Bindings.Items)
        {
            output.UpdateErrors();
            if (!string.IsNullOrEmpty(output.ErrorText)) foundError = true;
        }

        HasError = foundError;
    }

    private void AddDevice(IConfigurableDevice device)
    {
        if (device is Santroller santroller)
        {
            if (Device is Santroller santrollerold)
            {
                if (santrollerold.Serial == santroller.Serial)
                {
                    Main.Complete(100);
                    Device = device;
                    santroller.StartTicking(this);
                }
            }
            else if (Main is {Programming: false, Working: true})
            {
                Main.Complete(100);
                Device = device;
                santroller.StartTicking(this);
            }
        }

        Device.DeviceAdded(device);
    }

    public bool UsingBluetooth()
    {
        return IsBluetooth || Bindings.Items.Any(s => s is BluetoothOutput);
    }

    private void RemoveDevice(IConfigurableDevice device)
    {
    }

    public void Update(byte[] rfRaw, byte[] btRaw)
    {
        if (IsRf && rfRaw.Any())
        {
            Connected = rfRaw[0] != 0;
            RfModuleDetected = rfRaw[1] != 0;
        }

        if (IsBluetooth && btRaw.Any())
        {
            Connected = btRaw[0] != 0;
        }
    }
}
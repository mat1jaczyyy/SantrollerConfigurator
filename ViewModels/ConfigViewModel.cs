using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using GuitarConfigurator.NetCore.Configuration.Conversions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Other;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.Configuration.Outputs.Combined;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.Devices;
using ProtoBuf;
using ReactiveUI;
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

    private bool _allExpanded;


    private SpiConfig? _apa102SpiConfig;

    private DeviceControllerType _deviceControllerType;

    private EmulationType _emulationType;

    private LedType _ledType;

    private RhythmType _rhythmType;

    private readonly DirectPinConfig? _unoRx;
    private readonly DirectPinConfig? _unoTx;

    private readonly DirectPinConfig _usbHostDm;
    private readonly DirectPinConfig _usbHostDp;

    public ConfigViewModel(MainWindowViewModel screen, IConfigurableDevice device)
    {
        Device = device;
        Main = screen;
        if (device is Santroller santroller)
            LocalAddress = santroller.GetBluetoothAddress();
        else
            LocalAddress = "Write config to retrieve address";

        HostScreen = screen;
        Microcontroller = device.GetMicrocontroller(this);
        BindAllCommand = ReactiveCommand.CreateFromTask(BindAllAsync);

        WriteConfigCommand = ReactiveCommand.CreateFromObservable(() => Main.Write(this, true),
            this.WhenAnyValue(x => x.Main.Working, x => x.Main.Connected, x => x.HasError)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: true, Item3: false}));
        ResetCommand = ReactiveCommand.CreateFromTask(ResetAsync,
            this.WhenAnyValue(x => x.Main.Working, x => x.Main.Connected)
                .ObserveOn(RxApp.MainThreadScheduler).Select(x => x is {Item1: false, Item2: true}));
        GoBackCommand = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel?>(Main.GoBack.Execute,
            this.WhenAnyValue(x => x.Main.Working).Select(s => !s));

        SaveConfigCommand = ReactiveCommand.CreateFromObservable(() => SaveConfig.Handle(this));

        LoadConfigCommand = ReactiveCommand.CreateFromObservable(() => LoadConfig.Handle(this));
        this.WhenAnyValue(x => x.Deque, x => x.PollRate)
            .Select(GeneratePollRateLabel)
            .ToPropertyEx(this, x => x.PollRateLabel);
        this.WhenAnyValue(x => x.HasError)
            .Select(s => s ? "There are errors in your configuration" : null).ToPropertyEx(this, s => s.WriteToolTip);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Advanced)
            .ToPropertyEx(this, x => x.IsAdvancedMode);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Standard)
            .ToPropertyEx(this, x => x.IsStandardMode);
        this.WhenAnyValue(x => x.Mode).Select(x => x is ModeType.Core)
            .ToPropertyEx(this, x => x.IsRetailMode);
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
        _strumDebounceDisplay = this.WhenAnyValue(x => x.StrumDebounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.StrumDebounceDisplay);
        _debounceDisplay = this.WhenAnyValue(x => x.Debounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.DebounceDisplay);
        _deviceControllerRhythmType = this.WhenAnyValue(x => x.DeviceType, x => x.RhythmType)
            .Select(DeviceControllerRhythmTypeExtensions.FromDeviceRhythm)
            .ToProperty(this, x => x.DeviceControllerRhythmType);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => GetSimpleEmulationTypeFor(x) is EmulationType.Controller)
            .ToPropertyEx(this, x => x.IsController);
        this.WhenAnyValue(x => x.EmulationType)
            .Select(x => GetSimpleEmulationTypeFor(x) is EmulationType.KeyboardMouse)
            .ToPropertyEx(this, x => x.IsKeyboard);
        this.WhenAnyValue(x => x.LedType)
            .Select(x => x is LedType.Apa102Bgr or LedType.Apa102Brg or LedType.Apa102Gbr or LedType.Apa102Grb
                or LedType.Apa102Rbg or LedType.Apa102Rgb)
            .ToPropertyEx(this, x => x.IsApa102);
        Bindings.Connect()
            .Bind(out var outputs)
            .Subscribe();
        Outputs = outputs;
        Bindings.Connect().Filter(x => x is UsbHostCombinedOutput || x.Input.InputType is InputType.UsbHostInput).Any()
            .ToPropertyEx(this, x => x.UsbHostEnabled);
        SupportsReset = !device.IsMini() && !device.IsEsp32();

        _usbHostDm = new DirectPinConfig(this, UsbHostPinTypeDm, -1, DevicePinMode.Skip);
        _usbHostDp = new DirectPinConfig(this, UsbHostPinTypeDp, -1, DevicePinMode.Skip);
        if (!device.LoadConfiguration(this))
        {
            SetDefaults();
        }

        if (Main is {IsUno: false, IsMega: false}) return;
        _unoRx = new DirectPinConfig(this, UnoPinTypeRx, UnoPinTypeRxPin, DevicePinMode.Output);
        _unoTx = new DirectPinConfig(this, UnoPinTypeTx, UnoPinTypeTxPin, DevicePinMode.Output);
    }

    private static string GeneratePollRateLabel((bool dequeue, int rate) arg)
    {
        var rate = Math.Floor((1f / Math.Max(arg.rate, 1)) * 1000);
        return arg.dequeue ? $"Dequeue Rate ({rate}+ fps required)" : $"Poll Rate (0 for unlimited) ({rate}hz)";
    }

    public IConfigurableDevice Device { get; private set; }

    public ReadOnlyObservableCollection<Output> Outputs { get; }

    public bool ShowUnoDialog { get; }

    public bool SupportsReset { get; }

    private readonly ObservableAsPropertyHelper<DeviceControllerRhythmType> _deviceControllerRhythmType;

    private readonly ObservableAsPropertyHelper<float> _debounceDisplay;
    private readonly ObservableAsPropertyHelper<float> _strumDebounceDisplay;

    public DeviceControllerRhythmType DeviceControllerRhythmType
    {
        get => _deviceControllerRhythmType.Value;
        set
        {
            var (device, rhythm) = value.ToDeviceRhythm();
            DeviceType = device;
            RhythmType = rhythm;
        }
    }

    public float DebounceDisplay
    {
        get => _debounceDisplay.Value;
        set => Debounce = (int) (value * 10);
    }

    public float StrumDebounceDisplay
    {
        get => _strumDebounceDisplay.Value;
        set => StrumDebounce = (int) (value * 10);
    }

    [Reactive] public bool CombinedStrumDebounce { get; set; }
    [Reactive] public string? RfErrorText { get; set; }
    [Reactive] public string? UsbHostErrorText { get; set; }
    [Reactive] public string? Apa102ErrorText { get; set; }

    public bool AllExpanded
    {
        get => _allExpanded;
        set
        {
            this.RaiseAndSetIfChanged(ref _allExpanded, value);
            if (value)
                ExpandAll();
            else
                CollapseAll();
        }
    }

    public Interaction<(string _platformIOText, ConfigViewModel), RaiseIssueWindowViewModel?>
        ShowIssueDialog { get; } =
        new();

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowYesNoDialog { get; } = new();

    public Interaction<(string yesText, string noText, string text), AreYouSureWindowViewModel>
        ShowUnpluggedDialog { get; } =
        new();

    public Interaction<ConfigViewModel, Unit> SaveConfig { get; } = new();
    public Interaction<ConfigViewModel, Unit> LoadConfig { get; } = new();

    public Interaction<(ConfigViewModel model, Output output, DirectInput input),
            BindAllWindowViewModel>
        ShowBindAllDialog { get; } = new();

    public ICommand BindAllCommand { get; }

    public MainWindowViewModel Main { get; }

    public IEnumerable<DeviceControllerRhythmType> DeviceControllerRhythmTypes =>
        Enum.GetValues<DeviceControllerRhythmType>();

    public IEnumerable<RhythmType> RhythmTypes => Enum.GetValues<RhythmType>();
    public IEnumerable<ModeType> ModeTypes => Enum.GetValues<ModeType>();

    // Only Pico supports bluetooth
    public IEnumerable<EmulationType> EmulationTypes => Enum.GetValues<EmulationType>()
        .Where(type =>
            Device.IsPico() ||
            type is not (EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse));

    public IEnumerable<LedType> LedTypes => Enum.GetValues<LedType>();

    public IEnumerable<MouseMovementType> MouseMovementTypes => Enum.GetValues<MouseMovementType>();

    //TODO: actually read and write this as part of the config
    public bool KvEnabled { get; set; } = false;
    public int[] KvKey1 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();
    public int[] KvKey2 { get; set; } = Enumerable.Repeat(0x00, 16).ToArray();

    public ICommand WriteConfigCommand { get; }

    public ICommand SaveConfigCommand { get; }
    public ICommand LoadConfigCommand { get; }
    public ICommand ResetCommand { get; }

    public ICommand GoBackCommand { get; }

    public string LocalAddress { get; }

    [Reactive] public MouseMovementType MouseMovementType { get; set; }

    [Reactive] public ModeType Mode { get; set; }

    [Reactive] public int Debounce { get; set; }

    private bool _deque;

    public bool Deque
    {
        get => _deque;
        set
        {
            this.RaiseAndSetIfChanged(ref _deque, value);
            if (value)
            {
                // If we have enabled deque, then make sure the poll rate and debounce are above the min
                PollRate = Math.Max(1, PollRate);
                Debounce = Math.Max(5, Debounce);
            }
            else
            {
                // If we have disabled deque, then round to the nearest whole debounce
                Debounce = (int) (Math.Round(Debounce / 10.0f) * 10);
            }
        }
    }

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

    [Reactive] public bool Connected { get; set; }


    public int UsbHostDm
    {
        get => _usbHostDm.Pin;
        set
        {
            _usbHostDm.Pin = value;
            _usbHostDp.Pin = value - 1;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UsbHostDp));
            UpdateErrors();
        }
    }

    public int UsbHostDp
    {
        get => _usbHostDp.Pin;
        set
        {
            _usbHostDp.Pin = value;
            _usbHostDm.Pin = value + 1;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(UsbHostDm));
            UpdateErrors();
        }
    }

    [Reactive] public byte LedCount { get; set; }

    [Reactive] public byte WtSensitivity { get; set; }


    [Reactive] public bool HasError { get; set; }

    public LedType LedType
    {
        get => _ledType;
        set
        {
            if (value == LedType.None)
            {
                _apa102SpiConfig = null;
            }
            else if (_ledType == LedType.None)
            {
                _apa102SpiConfig = Microcontroller.AssignSpiPins(this, Apa102SpiType, false, -1, -1, -1, true, true,
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

    public Microcontroller Microcontroller { get; private set; }

    public SourceList<Output> Bindings { get; } = new();
    public bool BindableSpi => IsPico;

    public IDisposable RegisterConnections()
    {
        return
            Main.AvailableDevices.Connect().ObserveOn(RxApp.MainThreadScheduler).Subscribe(s =>
            {
                foreach (var change in s)
                    switch (change.Reason)
                    {
                        case ListChangeReason.Add:
                            AddDevice(change.Item.Current);
                            break;
                        case ListChangeReason.Remove:
                            RemoveDevice(change.Item.Current);
                            break;
                    }
            });
        ;
    } // ReSharper disable UnassignedGetOnlyAutoProperty
    [ObservableAsProperty] public bool IsStandardMode { get; }
    [ObservableAsProperty] public bool IsAdvancedMode { get; }
    [ObservableAsProperty] public bool IsRetailMode { get; }

    [ObservableAsProperty] public bool IsRhythm { get; }
    [ObservableAsProperty] public bool IsGuitar { get; }
    [ObservableAsProperty] public bool IsStageKit { get; }
    [ObservableAsProperty] public bool IsController { get; }
    [ObservableAsProperty] public bool IsKeyboard { get; }
    [ObservableAsProperty] public bool IsApa102 { get; }
    [ObservableAsProperty] public bool IsBluetooth { get; }

    [ObservableAsProperty] public string? WriteToolTip { get; }

    [ObservableAsProperty] public string? PollRateLabel { get; }

    [ObservableAsProperty] public bool UsbHostEnabled { get; }

    // ReSharper enable UnassignedGetOnlyAutoProperty

    public List<int> AvailableApaMosiPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Mosi)
        .Select(s => s.Key).ToList();

    public List<int> AvailableApaSckPins => Microcontroller.SpiPins(Apa102SpiType)
        .Where(s => s.Value is SpiPinType.Sck)
        .Select(s => s.Key).ToList();

    public List<int> AvailablePins => Microcontroller.GetAllPins(false);

    // Since DM and DP need to be next to eachother, you cannot use pins at the far ends
    public List<int> AvailablePinsDm => AvailablePins.Skip(1).ToList();
    public List<int> AvailablePinsDp => AvailablePins.SkipLast(1).ToList();

    public IEnumerable<PinConfig> PinConfigs =>
        new PinConfig?[] {_apa102SpiConfig, _usbHostDm, _usbHostDp, _unoRx, _unoTx}.Where(s => s != null)
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
    }

    public void UpdateBindings()
    {
        foreach (var binding in Bindings.Items) binding.UpdateBindings();
        InstrumentButtonTypeExtensions.ConvertBindings(Bindings, this, false);
        if (!(IsRhythm && IsController)) _rhythmType = RhythmType.GuitarHero;
        if (!IsGuitar)
        {
            Deque = false;
        }

        var (extra, types) =
            ControllerEnumConverter.FilterValidOutputs(_deviceControllerType, _rhythmType, Bindings.Items);
        Bindings.RemoveMany(extra);

        // If the user has a ps2 or wii combined output mapped, they don't need the default bindings
        if (Bindings.Items.Any(s =>
                s is WiiCombinedOutput or Ps2CombinedOutput or UsbHostCombinedOutput)) return;


        if (_deviceControllerType == DeviceControllerType.Turntable)
            if (!Bindings.Items.Any(s => s is DjCombinedOutput))
            {
                var dj = new DjCombinedOutput(this);
                dj.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(dj);
            }
        if (_deviceControllerType is not (DeviceControllerType.Guitar or DeviceControllerType.Drum))
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode {Type: EmulationModeType.Wii}));

        if (_deviceControllerType is DeviceControllerType.Turntable)
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode
            {
                Type: EmulationModeType.Ps4Or5 or EmulationModeType.XboxOne
            }));

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
            Bindings.RemoveMany(Bindings.Items.Where(s => s is EmulationMode {Type: EmulationModeType.Wii}));

        if (_deviceControllerType is not (DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar))
            Bindings.RemoveMany(Bindings.Items.Where(s => s is GuitarButton));

        foreach (var type in types)
            switch (type)
            {
                case StandardButtonType buttonType:
                    Bindings.Add(new ControllerButton(this,
                        new DirectInput(-1, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, buttonType, false));
                    break;
                case InstrumentButtonType buttonType:
                    Bindings.Add(new GuitarButton(this,
                        new DirectInput(-1, false, DevicePinMode.PullUp, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, buttonType, false));
                    break;
                case StandardAxisType axisType:
                    Bindings.Add(new ControllerAxis(this,
                        new DirectInput(-1, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, axisType, false));
                    break;
                case GuitarAxisType.Slider:
                    Bindings.Add(new GuitarAxis(this, new GhWtTapInput(GhWtInputType.TapAll, this,
                            -1,
                            -1, -1,
                            -1),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, GuitarAxisType.Slider, false));
                    break;
                case GuitarAxisType axisType:
                    Bindings.Add(new GuitarAxis(this, new DirectInput(-1,
                            false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, axisType, false));
                    break;
                case DrumAxisType axisType:
                    Bindings.Add(new DrumAxis(this,
                        new DirectInput(-1, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue,
                        0, 64, 10, axisType, false));
                    break;
                case DjAxisType.EffectsKnob:
                    Bindings.Add(new DjAxis(this,
                        new DirectInput(-1, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), 1, DjAxisType.EffectsKnob,
                        false));
                    break;
                case DjAxisType axisType:
                    if (axisType is DjAxisType.LeftTableVelocity or DjAxisType.RightTableVelocity) continue;
                    Bindings.Add(new DjAxis(this,
                        new DirectInput(-1, false, DevicePinMode.Analog, this),
                        Colors.Black, Colors.Black, Array.Empty<byte>(), ushort.MinValue, ushort.MaxValue, 0, axisType,
                        false));
                    break;
            }
    }

    public void SetDefaults()
    {
        Main.Message = "Building";
        Main.Progress = 0;
        ClearOutputs();
        Deque = false;
        LedType = LedType.None;
        _deviceControllerType = DeviceControllerType.Gamepad;
        CombinedStrumDebounce = false;
        WtSensitivity = 30;
        PollRate = 0;
        StrumDebounce = 0;
        Debounce = 10;

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
                var output = new WiiCombinedOutput(this)
                {
                    Expanded = true
                };
                output.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(output);
                break;
            case DeviceInputType.Ps2:
                var ps2Output = new Ps2CombinedOutput(this)
                {
                    Expanded = true
                };
                ps2Output.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(ps2Output);
                break;
            case DeviceInputType.Usb:
                var usbOutput = new UsbHostCombinedOutput(this)
                {
                    Expanded = true
                };
                usbOutput.SetOutputsOrDefaults(Array.Empty<Output>());
                Bindings.Add(usbOutput);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        UpdateBindings();
        UpdateErrors();
        // Write the full config
        Main.Write(this, false);
    }

    private async Task SetDefaultBindingsAsync(EmulationType emulationType)
    {
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

        if (GetSimpleEmulationType() is EmulationType.KeyboardMouse) return;

        foreach (var type in Enum.GetValues<StandardAxisType>())
        {
            if (!ControllerEnumConverter.GetAxisText(_deviceControllerType, type).Any()) continue;
            var isTrigger = type is StandardAxisType.LeftTrigger or StandardAxisType.RightTrigger;
            Bindings.Add(new ControllerAxis(this,
                new DirectInput(-1, false, DevicePinMode.Analog, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), isTrigger ? ushort.MinValue : short.MinValue,
                isTrigger ? ushort.MaxValue : short.MaxValue, 0,
                type, false));
        }

        foreach (var type in Enum.GetValues<StandardButtonType>())
        {
            if (!ControllerEnumConverter.GetButtonText(_deviceControllerType, type).Any()) continue;
            Bindings.Add(new ControllerButton(this,
                new DirectInput(-1, false, DevicePinMode.PullUp, this),
                Colors.Black, Colors.Black, Array.Empty<byte>(), 1, type, false));
        }

        UpdateErrors();
    }

    public void Generate(PlatformIo pio, bool generate)
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.Outputs.Items).ToList();
        var inputs = outputs.Select(binding => binding.Input.InnermostInput()).ToList();
        var directInputs = inputs.OfType<DirectInput>().ToList();
        var configFile = Path.Combine(pio.FirmwareDir, "include", "config_data.h");
        var lines = new List<string>();
        // Always include the current config - even if its invalid (aka, in the case of initial configs for wii and such)
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

        // Settings that are always written
        lines.Add($"#define WINDOWS_USES_XINPUT {XInputOnWindows.ToString().ToLower()}");
        lines.Add(
            $"#define ABSOLUTE_MOUSE_COORDS {(MouseMovementType == MouseMovementType.Absolute).ToString().ToLower()}");
        lines.Add($"#define ARDWIINO_BOARD \"{Microcontroller.Board.ArdwiinoName}\"");
        lines.Add($"#define CONSOLE_TYPE {GetEmulationType()}");
        lines.Add($"#define DEVICE_TYPE {(byte) DeviceType}");
        lines.Add($"#define POLL_RATE {PollRate}");
        lines.Add($"#define RHYTHM_TYPE {(byte) RhythmType}");

        lines.Add(Ps2Input.GeneratePs2Pressures(inputs));

        // Actually write the config as configured
        if (generate)
        {
            lines.Add($"#define USB_HOST_STACK {UsbHostEnabled.ToString().ToLower()}");
            lines.Add($"#define USB_HOST_DP_PIN {UsbHostDp}");

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

            lines.Add($"#define HANDLE_AUTH_LED {GenerateTick(ConfigField.AuthLed)}");

            var offLed = GenerateTick(ConfigField.OffLed);
            if (offLed.Any()) lines.Add($"#define HANDLE_LED_RUMBLE_OFF {offLed}");

            lines.Add($"#define HANDLE_PLAYER_LED {GenerateTick(ConfigField.PlayerLed)}");

            lines.Add($"#define HANDLE_LIGHTBAR_LED {GenerateTick(ConfigField.LightBarLed)}");

            if (Deque)
            {
                lines.Add("#define INPUT_QUEUE");
            }

            lines.Add($"#define HANDLE_RUMBLE {GenerateTick(ConfigField.RumbleLed)}");

            lines.Add($"#define HANDLE_KEYBOARD_LED {GenerateTick(ConfigField.KeyboardLed)}");
            if (EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse)
                lines.Add(
                    $"#define BLUETOOTH_TX {(EmulationType is EmulationType.Bluetooth or EmulationType.BluetoothKeyboardMouse).ToString().ToLower()}");

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
            lines.Add($"#define PIN_INIT {GenerateInit()}");

            // Copy in any specific config for the different pin configs (like spi and twi config on the pico)
            lines.Add(GetPinConfigs().Distinct().Aggregate("", (current, config) => current + config.Generate()));
            lines.Add(string.Join("\n",
                inputs.SelectMany(input => input.RequiredDefines()).Distinct().Select(define => $"#define {define}")));
        }
        else
        {
            // Write an empty config - the config at this point is likely invalid and won't compile
            lines.Add($"#define USB_HOST_STACK false");
            lines.Add($"#define USB_HOST_DP_PIN 0");

            lines.Add($"#define TICK_SHARED");
            lines.Add($"#define TICK_DETECTION");

            lines.Add($"#define TICK_PS3");

            lines.Add($"#define TICK_PS4");

            lines.Add($"#define TICK_XINPUT");

            lines.Add($"#define TICK_XBOX_ONE");

            lines.Add($"#define DIGITAL_COUNT 0");
            lines.Add($"#define LED_COUNT 0");
            lines.Add($"#define WT_SENSITIVITY {WtSensitivity}");

            lines.Add($"#define LED_TYPE 0");

            lines.Add($"#define HANDLE_AUTH_LED");

            lines.Add($"#define HANDLE_PLAYER_LED");

            lines.Add($"#define HANDLE_LIGHTBAR_LED");

            lines.Add($"#define HANDLE_RUMBLE");

            lines.Add($"#define HANDLE_KEYBOARD_LED");
            lines.Add("#define ADC_PINS {}");
            lines.Add($"#define ADC_COUNT 0");
            lines.Add($"#define PIN_INIT");
        }

        File.WriteAllLines(configFile, lines);
    }

    private string GenerateInit()
    {
        var ret = Microcontroller.GenerateInit(this);
        foreach (var output in Outputs)
        {
            if (output is not Led {Inverted: true} led) continue;
            if (led.UsesPwm)
            {
                ret += Microcontroller.GenerateAnalogWrite(led.Pin, "255") + ";";
            }
            else
            {
                ret += Microcontroller.GenerateDigitalWrite(led.Pin, true) + ";"; 
            }
        }
        return ret;
    }

    public PinConfig[] UsbHostPinConfigs()
    {
        return UsbHostEnabled ? new PinConfig[] {_usbHostDm, _usbHostDp} : Array.Empty<PinConfig>();
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
                return EmulationType.Controller;
            case EmulationType.KeyboardMouse:
            case EmulationType.BluetoothKeyboardMouse:
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
            case LedType.Apa102Rgb:
            case LedType.Apa102Rbg:
            case LedType.Apa102Grb:
            case LedType.Apa102Gbr:
            case LedType.Apa102Brg:
            case LedType.Apa102Bgr:
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
            UpdateErrors();
            return;
        }

        foreach (var binding in Bindings.Items) binding.Outputs.Remove(output);

        UpdateErrors();
    }

    [RelayCommand]
    public void ClearOutputs()
    {
        Bindings.Clear();
        UpdateErrors();
    }

    public async Task ClearOutputsWithConfirmationAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Clear", "Cancel",
            "The following action will clear all your inputs, are you sure you want to do this?")).ToTask();
        if (!yesNo.Response) return;

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
        var yesNo = await ShowYesNoDialog.Handle(("Reset", "Cancel",
            "The following action will clear all your inputs, are you sure you want to do this?")).ToTask();
        if (!yesNo.Response) return;

        Bindings.Clear();
        UpdateBindings();
        UpdateErrors();
    }

    private async Task ResetAsync()
    {
        var yesNo = await ShowYesNoDialog.Handle(("Revert", "Cancel",
                "The following action will revert your device back to an Arduino, are you sure you want to do this?"))
            .ToTask();
        if (!yesNo.Response) return;
        if (Device is not Santroller device) return;
        await Main.RevertCommand.Execute(device);
    }

    [RelayCommand]
    public void AddOutput()
    {
        if (IsController)
            Bindings.Add(new EmptyOutput(this));
        else if (IsKeyboard)
            Bindings.Add(new KeyboardButton(this, new DirectInput(0, false, DevicePinMode.PullUp, this),
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
        var outputsByType = outputs
            .GroupBy(s => s.Input.InnermostInput().GetType()).ToList();
        var combined = DeviceType is DeviceControllerType.Guitar or DeviceControllerType.LiveGuitar &&
                       CombinedStrumDebounce;
        Dictionary<string, int> debounces = new();
        var strumIndices = new List<int>();

        // Pass 1: work out debounces and map inputs to debounces
        var inputs = new Dictionary<string, List<int>>();
        var macros = new Dictionary<string, List<(int, Input)>>();
        foreach (var outputByType in outputsByType)
        {
            foreach (var output in outputByType)
            {
                var generatedInput = output.Input.Generate();
                if (output is not OutputButton and not DrumAxis and not EmulationMode) continue;

                if (output.Input is MacroInput)
                {
                    foreach (var input in output.Input.Inputs())
                    {
                        var gen = input.Generate();
                        macros.TryAdd(gen, new List<(int, Input)>());
                        macros[gen].AddRange(output.Input.Inputs().Where(s => s != input).Select(s => (0, s)));
                    }
                }

                debounces.TryAdd(generatedInput, debounces.Count);

                if (combined && output is GuitarButton
                    {
                        IsStrum: true
                    })
                    strumIndices.Add(debounces[generatedInput]);

                if (!inputs.ContainsKey(generatedInput)) inputs[generatedInput] = new List<int>();

                inputs[generatedInput].Add(debounces[generatedInput]);
            }
        }

        foreach (var (key, value) in macros)
        {
            var list2 = new List<(int, Input)>();
            foreach (var (_, input) in value)
            {
                var gen = input.Generate();
                if (debounces.TryGetValue(gen, out var debounce))
                {
                    list2.Add((debounce, input));
                }

                macros[key] = list2;
            }
        }

        var debouncesRelatedToLed = new Dictionary<byte, List<(Output, int)>>();
        var analogRelatedToLed = new Dictionary<byte, List<OutputAxis>>();
        // Handle most mappings
        var ret = outputsByType
            .Aggregate("", (current, group) =>
            {
                // we need to ensure that DigitalToAnalog is last
                return current + group
                    .First().Input.InnermostInput()
                    .GenerateAll(group
                        // DigitalToAnalog and MacroInput need to be handled last
                        .OrderByDescending(s => s.Input is DigitalToAnalog or MacroInput ? 0 : 1)
                        .Select(s =>
                        {
                            var input = s.Input;
                            var output = s;
                            var generatedInput = input.Generate();
                            var index = 0;
                            if (output is OutputButton or DrumAxis or EmulationMode)
                            {
                                index = debounces[generatedInput];

                                foreach (var led in output.LedIndices)
                                {
                                    if (!debouncesRelatedToLed.ContainsKey(led))
                                        debouncesRelatedToLed[led] = new List<(Output, int)>();

                                    debouncesRelatedToLed[led].Add((output, index));
                                }
                            }

                            if (output is OutputAxis axis)
                            {
                                foreach (var led in output.LedIndices)
                                {
                                    if (!analogRelatedToLed.ContainsKey(led))
                                        analogRelatedToLed[led] = new List<OutputAxis>();

                                    analogRelatedToLed[led].Add(axis);
                                }
                            }

                            var generated = output.Generate(mode, index, "", "", strumIndices, macros);
                            return new Tuple<Input, string>(input, generated);
                        })
                        .Where(s => !string.IsNullOrEmpty(s.Item2))
                        .Distinct().ToList(), mode);
            });
        if (mode == ConfigField.Shared && LedType is not LedType.None)
        {
            // Handle leds, including when multiple leds are assigned to a single output.
            foreach (var (led, relatedOutputs) in debouncesRelatedToLed)
            {
                var analog = "";
                if (analogRelatedToLed.TryGetValue(led, out var analogLedOutputs))
                {
                    foreach (var analogLedOutput in analogLedOutputs)
                    {
                        var ledRead = analogLedOutput.GenerateAssignment(ConfigField.Ps3, false, true, false);
                        // Now we have the value, calibrated as a uint8_t
                        // Only apply analog colours if non zero when conflicting with digital, so that the digital off states override
                        analog +=
                            @$"led_tmp = {ledRead};
                                   if(led_tmp) {{
                                        {LedType.GetLedAssignment(led, analogLedOutput.LedOn, analogLedOutput.LedOff, "led_tmp")}
                                   }} else {{
                                        {LedType.GetLedAssignment(relatedOutputs.First().Item1.LedOff, led)}
                                   }}";
                    }
                }

                if (!analog.Any())
                {
                    analog = LedType.GetLedAssignment(relatedOutputs.First().Item1.LedOff, led);
                }

                ret += $"if (ledState[{led - 1}].select == 0) {{";
                ret += string.Join(" else ", relatedOutputs.Select(tuple =>
                {
                    var ifStatement = $"debounce[{tuple.Item2}]";
                    return @$"if ({ifStatement}) {{
                                        {LedType.GetLedAssignment(tuple.Item1.LedOn, led)}
                                       }}";
                }));
                ret += $@" else {{
                        {analog}
                    }}
                }}";
            }

            foreach (var (led, analogLedOutputs) in analogRelatedToLed)
            {
                if (debouncesRelatedToLed.ContainsKey(led)) continue;
                ret += $"if (ledState[{led - 1}].select == 0) {{";
                foreach (var analogLedOutput in analogLedOutputs)
                {
                    var ledRead = analogLedOutput.GenerateAssignment(ConfigField.Ps3, false, true, false);
                    // Now we have the value, calibrated as a uint8_t
                    ret +=
                        $"led_tmp = {ledRead};{LedType.GetLedAssignment(led, analogLedOutput.LedOn, analogLedOutput.LedOff, "led_tmp")}";
                }

                ret += "}";
            }
        }

        return ret.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }

    private int CalculateDebounceTicks()
    {
        var outputs = Bindings.Items.SelectMany(binding => binding.ValidOutputs()).ToList();
        var outputsByType = outputs
            .GroupBy(s => s.Input.InnermostInput().GetType()).ToList();
        Dictionary<string, int> debounces = new();

        foreach (var outputByType in outputsByType)
        {
            foreach (var output in outputByType)
            {
                var generatedInput = output.Input.Generate();
                if (output is not OutputButton and not DrumAxis and not EmulationMode) continue;


                debounces.TryAdd(generatedInput, debounces.Count);
            }
        }

        return debounces.Count;
    }

    public List<PinConfig> GetPinConfigs()
    {
        return Bindings.Items.SelectMany(s => s.GetPinConfigs()).Concat(PinConfigs).Distinct().ToList();
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

        if (Main.IsUno || Main.IsMega)
        {
            pins[UnoPinTypeTx] = new List<int> {UnoPinTypeTxPin};
            pins[UnoPinTypeRx] = new List<int> {UnoPinTypeRxPin};
        }

        if (IsApa102 && _apa102SpiConfig != null && type != Apa102SpiType)
            pins["APA102"] = _apa102SpiConfig.Pins.ToList();

        if (UsbHostEnabled && type != UsbHostPinTypeDm && type != UsbHostPinTypeDp)
            pins["USB Host"] = new List<int> {UsbHostDm, UsbHostDp};

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

        if (IsApa102 && Microcontroller.SpiAssignable)
        {
            var error = _apa102SpiConfig!.ErrorText;
            if (error != null)
            {
                foundError = true;
            }

            Apa102ErrorText = error;
        }
        else
        {
            Apa102ErrorText = "";
        }

        HasError = foundError;
    }

    public void AddDevice(IConfigurableDevice device)
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            Trace.WriteLine($"Add called, current device: {Device},  new device: {device}");
            Trace.Flush();
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
                else
                {
                    Main.Complete(100);
                    Device = device;
                    Microcontroller = device.GetMicrocontroller(this);
                    santroller.StartTicking(this);
                }
            }

            Device.DeviceAdded(device);
        });
    }

    public bool UsingBluetooth()
    {
        return IsBluetooth || Bindings.Items.Any(s => s is BluetoothOutput);
    }

    public void RemoveDevice(IConfigurableDevice device)
    {
        if (!Main.Working && device == Device)
            ShowUnpluggedDialog.Handle(("", "", "")).ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(s => Main.GoBack.Execute(new Unit()));
    }

    public void Update(byte[] btRaw)
    {
        if (IsBluetooth && btRaw.Any())
        {
            Connected = btRaw[0] != 0;
        }
    }

    public TwiConfig? GetTwiForType(string twiType)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
            .Select(configs => configs.OfType<TwiConfig>().FirstOrDefault(s => s.Type == twiType))
            .FirstOrDefault(found => found != null);
    }

    public SpiConfig? GetSpiForType(string spiType)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
            .Select(configs => configs.OfType<SpiConfig>().FirstOrDefault(s => s.Type == spiType))
            .FirstOrDefault(found => found != null);
    }
    public DirectPinConfig GetPinForType(string pinType, int fallbackPin, DevicePinMode fallbackMode)
    {
        return Bindings.Items.Select(binding => binding.GetPinConfigs())
            .Select(configs => configs.OfType<DirectPinConfig>().FirstOrDefault(s => s.Type == pinType))
            .FirstOrDefault(found => found != null) ?? new DirectPinConfig(this,pinType, fallbackPin, fallbackMode);
    }

    public void MoveUp(Output output)
    {
        var index = Bindings.Items.IndexOf(output);
        Bindings.Move(index, index - 1);
    }

    public void MoveDown(Output output)
    {
        var index = Bindings.Items.IndexOf(output);
        Bindings.Move(index, index + 1);
    }

    // Capture any input events (such as pointer or keyboard) - used for detecting the last input
    public readonly Subject<object> KeyOrPointerEvent = new();

    public void OnKeyEvent(KeyEventArgs args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(PointerEventArgs args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(PointerUpdateKind args)
    {
        KeyOrPointerEvent.OnNext(args);
    }

    public void OnMouseEvent(Point args)
    {
        KeyOrPointerEvent.OnNext(args);
    }
}
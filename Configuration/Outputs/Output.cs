using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Configuration.Wii;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class Output : ReactiveObject, IDisposable
{
    protected readonly ConfigViewModel Model;

    private Input? _input;

    public Input? Input
    {
        get => _input;
        set => this.RaiseAndSetIfChanged(ref _input, value);
    }

    private readonly ObservableAsPropertyHelper<Bitmap?> _image;
    public Bitmap? Image => _image.Value;

    public string Name { get; }

    public InputType? SelectedInputType
    {
        get => Input?.InputType;
        set => SetInput(value, null, null, null, null, null);
    }

    public WiiInputType WiiInputType
    {
        get => (Input?.InnermostInput() as WiiInput)?.Input ?? WiiInputType.ClassicA;
        set => SetInput(SelectedInputType, value, null, null, null, null);
    }

    public Ps2InputType Ps2InputType
    {
        get => (Input?.InnermostInput() as Ps2Input)?.Input ?? Ps2InputType.Cross;
        set => SetInput(SelectedInputType, null, value, null, null, null);
    }

    public Gh5NeckInputType Gh5NeckInputType
    {
        get => (Input?.InnermostInput() as Gh5NeckInput)?.Input ?? Gh5NeckInputType.Green;
        set => SetInput(SelectedInputType, null, null, null, value, null);
    }

    public DjInputType DjInputType
    {
        get => (Input?.InnermostInput() as DjInput)?.Input ?? DjInputType.LeftGreen;
        set => SetInput(SelectedInputType, null, null, null, null, value);
    }

    public GhWtInputType GhWtInputType
    {
        get => (Input?.InnermostInput() as GhWtTapInput)?.Input ?? GhWtInputType.TapGreen;
        set => SetInput(SelectedInputType, null, null, value, null, null);
    }

    public IEnumerable<GhWtInputType> GhWtInputTypes => Enum.GetValues<GhWtInputType>();

    public IEnumerable<Gh5NeckInputType> Gh5NeckInputTypes => Enum.GetValues<Gh5NeckInputType>();

    public IEnumerable<Ps2InputType> Ps2InputTypes => Enum.GetValues<Ps2InputType>();

    public IEnumerable<WiiInputType> WiiInputTypes => Enum.GetValues<WiiInputType>();

    public IEnumerable<DjInputType> DjInputTypes => Enum.GetValues<DjInputType>();

    public IEnumerable<InputType> InputTypes => Enum.GetValues<InputType>();

    private readonly ObservableAsPropertyHelper<bool> _isDj;
    private readonly ObservableAsPropertyHelper<bool> _isWii;
    private readonly ObservableAsPropertyHelper<bool> _isPs2;
    private readonly ObservableAsPropertyHelper<bool> _isGh5;
    private readonly ObservableAsPropertyHelper<bool> _isWt;
    private readonly ObservableAsPropertyHelper<bool> _areLedsEnabled;
    private readonly ObservableAsPropertyHelper<string?> _localisedName;

    public string? LocalisedName => _localisedName.Value;
    public bool IsDj => _isDj.Value;
    public bool IsWii => _isWii.Value;
    public bool IsPs2 => _isPs2.Value;
    public bool IsGh5 => _isGh5.Value;
    public bool IsWt => _isWt.Value;
    public bool AreLedsEnabled => _areLedsEnabled.Value;

    public abstract bool IsCombined { get; }

    private Color _ledOn;
    private Color _ledOff;
    private byte? _ledIndex;

    public byte? LedIndex
    {
        get => _ledIndex;
        set => this.RaiseAndSetIfChanged(ref _ledIndex, value);
    }

    public Color LedOn
    {
        get => _ledOn;
        set => this.RaiseAndSetIfChanged(ref _ledOn, value);
    }

    public Color LedOff
    {
        get => _ledOff;
        set => this.RaiseAndSetIfChanged(ref _ledOff, value);
    }

    private readonly ObservableAsPropertyHelper<double> _imageOpacity;

    public double ImageOpacity => _imageOpacity.Value;

    private readonly ObservableAsPropertyHelper<int> _valueRaw;
    public int ValueRaw => _valueRaw.Value;

    protected Output(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte? ledIndex, string name)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        LedIndex = ledIndex;
        Name = name;
        Model = model;
        _image = this.WhenAnyValue(x => x.Model.DeviceType).Select(GetImage).ToProperty(this, x => x.Image);
        _isDj = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is DjInput)
            .ToProperty(this, x => x.IsDj);
        _isWii = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is WiiInput)
            .ToProperty(this, x => x.IsWii);
        _isGh5 = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is Gh5NeckInput)
            .ToProperty(this, x => x.IsGh5);
        _isPs2 = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is Ps2Input)
            .ToProperty(this, x => x.IsPs2);
        _isWt = this.WhenAnyValue(x => x.Input).Select(x => x?.InnermostInput() is GhWtTapInput)
            .ToProperty(this, x => x.IsWt);
        _areLedsEnabled = this.WhenAnyValue(x => x.Model.LedType).Select(x => x is LedType.Apa102)
            .ToProperty(this, x => x.AreLedsEnabled);
        _localisedName = this.WhenAnyValue(x => x.Model.DeviceType, x => x.Model.RhythmType)
            .Select(x => GetLocalisedName())
            .ToProperty(this, x => x.LocalisedName);
        _valueRaw = this.WhenAnyValue(x => x.Input!.RawValue).ToProperty(this, x => x.ValueRaw);
        _imageOpacity = this.WhenAnyValue(x => x.ValueRaw, x => x.Input, x => x.IsCombined)
            .Select(s => (s.Item3 || s.Item2?.IsAnalog == true) ? 1 : ((s.Item1 == 0 ? 0 : 0.35) + 0.65))
            .ToProperty(this, s => s.ImageOpacity);
    }


    public abstract string? GetLocalisedName();
    public abstract bool IsStrum { get; }

    private void SetInput(InputType? inputType, WiiInputType? wiiInput, Ps2InputType? ps2InputType,
        GhWtInputType? ghWtInputType, Gh5NeckInputType? gh5NeckInputType, DjInputType? djInputType)
    {
        Input input;
        var lastPin = 0;
        var pinMode = DevicePinMode.PullUp;
        if (Input?.InnermostInput() is DirectInput direct)
        {
            lastPin = direct.Pin;
            if (!direct.IsAnalog)
            {
                pinMode = direct.PinMode;
            }
        }

        switch (inputType)
        {
            case InputType.AnalogPinInput:
                input = new DirectInput(lastPin, DevicePinMode.Analog, Model.MicroController!);
                break;
            case InputType.DigitalPinInput:
                input = new DirectInput(lastPin, pinMode, Model.MicroController!);
                break;
            case InputType.TurntableInput when Input?.InnermostInput() is not DjInput:
                djInputType ??= DjInputType.LeftGreen;
                input = new DjInput(djInputType.Value, Model.MicroController!);
                break;
            case InputType.TurntableInput when Input?.InnermostInput() is DjInput dj:
                djInputType ??= DjInputType.LeftGreen;
                input = new DjInput(djInputType.Value, Model.MicroController!, dj.Sda, dj.Scl);
                break;
            case InputType.Gh5NeckInput when Input?.InnermostInput() is not Gh5NeckInput:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model.MicroController!);
                break;
            case InputType.Gh5NeckInput when Input?.InnermostInput() is Gh5NeckInput gh5:
                gh5NeckInputType ??= Gh5NeckInputType.Green;
                input = new Gh5NeckInput(gh5NeckInputType.Value, Model.MicroController!, gh5.Sda, gh5.Scl);
                break;
            case InputType.WtNeckInput when Input?.InnermostInput() is not GhWtTapInput:
                ghWtInputType ??= GhWtInputType.TapGreen;
                input = new GhWtTapInput(ghWtInputType.Value, Model.MicroController!);
                break;
            case InputType.WtNeckInput when Input?.InnermostInput() is GhWtTapInput wt:
                ghWtInputType ??= GhWtInputType.TapGreen;
                input = new GhWtTapInput(ghWtInputType.Value, Model.MicroController!, wt.Pin);
                break;
            case InputType.WiiInput when Input?.InnermostInput() is not WiiInput:
                wiiInput ??= WiiInputType.ClassicA;
                input = new WiiInput(wiiInput.Value, Model.MicroController!);
                break;
            case InputType.WiiInput when Input?.InnermostInput() is WiiInput wii:
                wiiInput ??= WiiInputType.ClassicA;
                input = new WiiInput(wiiInput.Value, Model.MicroController!, wii.Sda, wii.Scl);
                break;
            case InputType.Ps2Input when Input?.InnermostInput() is not Ps2Input:
                ps2InputType ??= Ps2InputType.Cross;
                input = new Ps2Input(ps2InputType.Value, Model.MicroController!);
                break;
            case InputType.Ps2Input when Input?.InnermostInput() is Ps2Input ps2:
                ps2InputType ??= Ps2InputType.Cross;
                input = new Ps2Input(ps2InputType.Value, Model.MicroController!, ps2.Miso, ps2.Mosi, ps2.Sck, ps2.Att,
                    ps2.Ack);
                break;
            default:
                return;
        }

        switch (input.IsAnalog)
        {
            case true when this is OutputAxis:
            case false when this is OutputButton:
                Input = input;
                break;
            case true when this is OutputButton:
                var oldThreshold = 0;
                if (Input is AnalogToDigital atd)
                {
                    oldThreshold = atd.Threshold;
                }

                Input = new AnalogToDigital(input, AnalogToDigitalType.JoyHigh, oldThreshold);
                break;
            case false when this is OutputAxis:
                var oldValue = 0;
                if (Input is DigitalToAnalog dta)
                {
                    oldValue = dta.Value;
                }

                Input = new DigitalToAnalog(input, oldValue);
                break;
        }

        this.RaisePropertyChanged(nameof(WiiInputType));
        this.RaisePropertyChanged(nameof(Ps2InputType));
        this.RaisePropertyChanged(nameof(GhWtInputType));
        this.RaisePropertyChanged(nameof(Gh5NeckInputType));
        this.RaisePropertyChanged(nameof(DjInputType));
    }


    public abstract SerializedOutput Serialize();

    private Bitmap? GetImage(DeviceControllerType type)
    {
        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        string? bitmap = null;
        if (this is EmptyOutput)
        {
            bitmap = "Generic.png";
        }
        else if (IsCombined)
        {
            bitmap = $"Combined/{Name}.png";
        }
        else
        {
            switch (type)
            {
                case DeviceControllerType.Guitar:
                    bitmap = $"GH/{Name}.png";
                    break;
                case DeviceControllerType.Gamepad:
                    bitmap = $"Others/Xbox360/360_{Name}.png";
                    break;
            }
        }

        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        try
        {
            var asset = assets!.Open(new Uri($"avares://{assemblyName}/Assets/Icons/{bitmap}"));
            return new Bitmap(asset);
        }
        catch (FileNotFoundException)
        {
            var asset = assets!.Open(new Uri($"avares://{assemblyName}/Assets/Icons/None.png"));
            return new Bitmap(asset);
        }
    }

    public abstract string Generate(bool xbox, bool shared, int debounceIndex, bool combined);
    public abstract string GenerateLedUpdate(int debounceIndex, bool xbox);

    public virtual AvaloniaList<Output> Outputs => new() {this};

    public void Remove()
    {
        Model.RemoveOutput(this);
    }

    public virtual void Dispose()
    {
        Input?.Dispose();
    }

    public bool IsCombinedChild => Model.IsCombinedChild(this);

    public List<PinConfig> GetPinConfigs() => Outputs
        .SelectMany(s => s.Outputs).SelectMany(s => (s.Input?.PinConfigs ?? new()))
        .Distinct().ToList();
    public List<DevicePin> GetPins() => Outputs
        .SelectMany(s => s.Outputs).SelectMany(s => (s.Input?.Pins ?? new()))
        .Distinct().ToList();

    public virtual void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType)
    {
        foreach (var output in Outputs)
        {
            output.Input?.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw,
                ghWtRaw,
                ps2ControllerType, wiiControllerType);
        }
    }
}
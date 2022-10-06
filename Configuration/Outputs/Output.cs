using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Json;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Neck;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.Configuration.Wii;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
public abstract class Output : ReactiveObject
{
    protected readonly ConfigViewModel Model;

    private Input? _input;

    public Input? Input
    {
        get => _input;
        set => this.RaiseAndSetIfChanged(ref _input, value);
    }

    //TODO: probably want to load these ahead of time and cache, or atleast load them in the background
    private readonly ObservableAsPropertyHelper<Bitmap?> _image;
    public Bitmap? Image => _image.Value;

    public string Name { get; }

    private InputType? _inputType;

    public InputType? SelectedInputType
    {
        get => _inputType;
        set => this.RaiseAndSetIfChanged(ref _inputType, value);
    }

    private WiiInputType _wiiInputType;

    public WiiInputType WiiInputType
    {
        get => _wiiInputType;
        set => this.RaiseAndSetIfChanged(ref _wiiInputType, value);
    }

    private Ps2InputType _ps2InputType;

    public Ps2InputType Ps2InputType
    {
        get => _ps2InputType;
        set => this.RaiseAndSetIfChanged(ref _ps2InputType, value);
    }

    private Gh5NeckInputType _gh5NeckInputType;

    public Gh5NeckInputType Gh5NeckInputType
    {
        get => _gh5NeckInputType;
        set => this.RaiseAndSetIfChanged(ref _gh5NeckInputType, value);
    }

    private DjInputType _djInputType;

    public DjInputType DjInputType
    {
        get => _djInputType;
        set => this.RaiseAndSetIfChanged(ref _djInputType, value);
    }

    private GhWtInputType _ghWtInputType;

    public GhWtInputType GhWtInputType
    {
        get => _ghWtInputType;
        set => this.RaiseAndSetIfChanged(ref _ghWtInputType, value);
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
    public bool IsDj => _isDj.Value;
    public bool IsWii => _isWii.Value;
    public bool IsPs2 => _isPs2.Value;
    public bool IsGh5 => _isGh5.Value;
    public bool IsWt => _isWt.Value;
    public bool AreLedsEnabled => _areLedsEnabled.Value;

    public abstract bool IsCombined { get; }

    private Color _ledOn;
    private Color _ledOff;

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

    public Output(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, string name)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Name = name;
        this.Model = model;
        _image = this.WhenAnyValue(x => x.Model.DeviceType).Select(GetImage).ToProperty(this, x => x.Image);
        _isDj = this.WhenAnyValue(x => x.SelectedInputType).Select(x => x is InputType.TurntableInput)
            .ToProperty(this, x => x.IsDj);
        _isWii = this.WhenAnyValue(x => x.SelectedInputType).Select(x => x is InputType.WiiInput)
            .ToProperty(this, x => x.IsWii);
        _isGh5 = this.WhenAnyValue(x => x.SelectedInputType).Select(x => x is InputType.GH5NeckInput)
            .ToProperty(this, x => x.IsGh5);
        _isPs2 = this.WhenAnyValue(x => x.SelectedInputType).Select(x => x is InputType.PS2Input)
            .ToProperty(this,  x => x.IsPs2);
        _isWt = this.WhenAnyValue(x => x.SelectedInputType).Select(x => x is InputType.WTNeckInput)
            .ToProperty(this, x => x.IsWt);
        _areLedsEnabled = this.WhenAnyValue(x => x.Model.LedType).Select(x => x is LedType.APA102)
            .ToProperty(this, x => x.AreLedsEnabled);
    }


    public void ClearInput()
    {
        Input = null;
    }

    public void SetInput()
    {
        Input input;
        switch (SelectedInputType)
        {
            case InputType.AnalogPinInput:
                input = new DirectInput(0, DevicePinMode.Analog, Model.MicroController!);
                break;
            case InputType.DigitalPinInput:
                input = new DirectInput(0, DevicePinMode.PullUp, Model.MicroController!);
                break;
            case InputType.TurntableInput:
                input = new DjInput(_djInputType, Model.MicroController!);
                break;
            case InputType.GH5NeckInput:
                input = new Gh5NeckInput(_gh5NeckInputType, Model.MicroController!);
                break;
            case InputType.WTNeckInput:
                input = new GhWtTapInput(_ghWtInputType, Model.MicroController!);
                break;
            case InputType.WiiInput:
                input = new WiiInput(_wiiInputType, Model.MicroController!);
                break;
            case InputType.PS2Input:
                input = new Ps2Input(_ps2InputType, Model.MicroController!);
                break;
            default:
                return;
        }

        switch (input.IsAnalog)
        {
            case true when this is OutputAxis:
            case false when this is OutputButton:
                this.Input = input;
                break;
            case true when this is OutputButton:
                this.Input = new AnalogToDigital(input, AnalogToDigitalType.JoyHigh, 0);
                break;
            case false when this is OutputAxis:
                this.Input = new DigitalToAnalog(input, 0);
                break;
        }
    }


    public abstract JsonOutput GetJson();
    private Bitmap? GetImage(DeviceControllerType type)
    {
        string assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        string? bitmap = null;
        if (IsCombined)
        {
            bitmap = $"Combined/{Name}.png";
        }
        else
        {
            switch (type)
            {
                case DeviceControllerType.Guitar:
                    bitmap = $"GH/{this.Name}.png";
                    break;
                case DeviceControllerType.Gamepad:
                    bitmap = $"Icons/Others/Xbox360/360_{this.Name}.png";
                    break;
            }
        }

        if (bitmap == null) return null;
        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        try
        {
            var asset = assets!.Open(new Uri($"avares://{assemblyName}/Assets/Icons/{bitmap}"));
            return new Bitmap(asset);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

    }

    public abstract string Generate(bool xbox);
    [JsonIgnore]
    public virtual IReadOnlyList<Output> Outputs => new []{this};
}
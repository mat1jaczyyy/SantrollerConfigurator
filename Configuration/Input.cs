using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Collections;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public abstract class Input : ReactiveObject, IDisposable
{
    protected ConfigViewModel Model { get; }

    protected Input(ConfigViewModel model)
    {
        Model = model;
        _imageOpacity = this.WhenAnyValue(x => x.RawValue).Select(s => (s == 0?0:0.25) + 0.75).ToProperty(this, s => s.ImageOpacity);
    }
    public abstract IReadOnlyList<string> RequiredDefines();
    public abstract string Generate(bool xbox);

    public abstract SerializedInput Serialise();

    public abstract bool IsAnalog { get; }
    public abstract bool IsUint { get; }

    private int _rawValue;
    public int RawValue
    {
        get => _rawValue;
        set => this.RaiseAndSetIfChanged(ref _rawValue, value);
    }
    
    private readonly ObservableAsPropertyHelper<double> _imageOpacity;

    public double ImageOpacity => _imageOpacity.Value;

    public virtual Input InnermostInput()
    {
        return this;
    }

    public abstract IList<DevicePin> Pins { get; }
    public abstract IList<PinConfig> PinConfigs { get; }
    public abstract InputType? InputType { get; }

    public abstract void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType);

    public abstract string GenerateAll(List<Output> allBindings, List<Tuple<Input, string>> bindings);

    public abstract void Dispose();
}
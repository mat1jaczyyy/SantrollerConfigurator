using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Configuration.Serialization;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GuitarConfigurator.NetCore.Configuration.Inputs;

public abstract class Input : ReactiveObject, IDisposable
{

    private Bitmap? _image;

    protected Input(ConfigViewModel model)
    {
        Model = model;
    }

    protected ConfigViewModel Model { get; }

    public Bitmap Image => GetImage();

    [Reactive] public bool IsAnalog { get; set; }
    [Reactive] public int RawValue { get; set; }
    public abstract bool IsUint { get; }


    public abstract IList<DevicePin> Pins { get; }
    public abstract IList<PinConfig> PinConfigs { get; }
    public abstract InputType? InputType { get; }

    public abstract void Dispose();

    public abstract IReadOnlyList<string> RequiredDefines();
    public abstract string Generate(ConfigField mode);

    public abstract SerializedInput Serialise();

    public virtual Input InnermostInput()
    {
        return this;
    }

    public virtual IList<Input> Inputs()
    {
        return new List<Input> {this};
    }

    public abstract void Update(Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw, byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType,
        byte[] wiiControllerType);

    public abstract string GenerateAll(List<Tuple<Input, string>> bindings,
        ConfigField mode);

    public abstract string GetImagePath();

    public abstract string Title { get; }

    private Bitmap GetImage()
    {
        if (_image != null) return _image;

        var assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;
        try
        {
            var asset = AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/Icons/{GetImagePath()}"));
            _image = new Bitmap(asset);
        }
        catch (FileNotFoundException)
        {
            var asset = AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/Icons/Generic.png"));
            _image = new Bitmap(asset);
        }

        return _image;
    }
}
using System;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedOutput : Output
{
    protected CombinedOutput(ConfigViewModel model, Input? input, string name) : base(model, input, Colors.Transparent, Colors.Transparent, Array.Empty<byte>(), name)
    {
    }

    public override string Generate(bool xbox, bool shared, int debounceIndex, bool combined)
    {
        return "";
    }
    public override bool IsCombined => true;
    public override bool IsStrum => false;
    public override bool IsKeyboard => false;
    public override bool IsController => true;
    public override bool IsMidi => false;
    public override string? GetLocalisedName() => Name;
}
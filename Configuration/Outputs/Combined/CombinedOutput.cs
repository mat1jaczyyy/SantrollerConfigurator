using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedOutput : Output
{
    protected CombinedOutput(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, string name) : base(model, input, ledOn, ledOff, name)
    {
    }

    public override string Generate(bool xbox, bool shared, int debounceIndex, bool combined)
    {
        return "";
    }

    public override string GenerateLedUpdate(int debounceIndex, bool xbox)
    {
        return "";
    }

    public override bool IsCombined => true;
    public override bool IsStrum => false;
    public override string? GetLocalisedName() => Name;
}
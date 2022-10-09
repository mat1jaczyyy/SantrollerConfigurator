using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class OutputAxis : Output
{
    protected OutputAxis(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, float multiplier, int offset,
        int deadzone, string name) : base(model, input, ledOn, ledOff, name)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Multiplier = multiplier;
        Offset = offset;
        Deadzone = deadzone;
    }

    public float Multiplier { get; set; }
    public int Offset { get; set; }
    public int Deadzone { get; set; }
    public abstract bool Trigger { get; }

    public abstract string GenerateOutput(bool xbox);
    public override bool IsCombined => false;

    public override string Generate(bool xbox, int debounceIndex)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        return $"{GenerateOutput(xbox)} = {Input.Generate()}";
    }

}
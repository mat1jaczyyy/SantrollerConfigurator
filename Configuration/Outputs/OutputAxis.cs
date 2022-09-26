using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class OutputAxis : Output
{
    protected OutputAxis(ConfigViewModel model, IInput? input, Color ledOn, Color ledOff, float multiplier, int offset,
        int deadzone) : base(model, input, ledOn, ledOff)
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

    public override string Generate(bool xbox, Microcontroller.Microcontroller microcontroller)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        return $"{GenerateOutput(xbox)} = {Input.Generate(xbox, microcontroller)}";
    }
}
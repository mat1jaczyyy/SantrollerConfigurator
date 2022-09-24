using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Output;

public abstract class OutputAxis : IOutput
{
    public abstract string Name { get; }
    public abstract string Image { get; }

    public IInput? Input { get; set; }
    public Color LedOn { get; set; }
    public Color LedOff { get; set; }

    protected OutputAxis(IInput? input, Color ledOn, Color ledOff, float multiplier, int offset, int deadzone,
        bool trigger)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Multiplier = multiplier;
        Offset = offset;
        Deadzone = deadzone;
        Trigger = trigger;
    }

    public float Multiplier { get; set; }
    public int Offset { get; set; }
    public int Deadzone { get; set; }
    public bool Trigger { get; set; }

    public abstract string GenerateOutput(bool xbox);

    public string Generate(bool xbox, Microcontroller.Microcontroller microcontroller)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        return $"{GenerateOutput(xbox)} = {Input.Generate(xbox, microcontroller)}";
    }
}
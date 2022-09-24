using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;

namespace GuitarConfiguratorSharp.NetCore.Configuration;

public interface IOutput
{
    public string Name { get; }

    public string Image { get; }

    public IInput? Input { get; set; }
    public Color LedOn { get; set; }
    public Color LedOff { get; set; }
    public string Generate(bool xbox, Microcontroller.Microcontroller microcontroller);
}

public abstract class OutputButton : IOutput
{
    protected OutputButton(IInput? input, Color ledOn, Color ledOff, int debounce)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Debounce = debounce;
    }

    public abstract string Name { get; }
    public abstract string Image { get; }
    public IInput? Input { get; set; }
    public Color LedOn { get; set; }
    public Color LedOff { get; set; }
    public int Debounce { get; set; }
    public abstract string GenerateIndex(bool xbox);

    public abstract string GenerateOutput(bool xbox);

    public abstract bool IsStrum();


    public string Generate(bool xbox, Microcontroller.Microcontroller microcontroller)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        var outputVar = GenerateOutput(xbox);
        var outputBit = GenerateIndex(xbox);
        if (Debounce == 0)
        {
            return $"if (({Input.Generate(xbox, microcontroller)})) {{{outputVar} |= (1 << {outputBit});}}";
        }
        else
        {
            return
                $"if (({Input.Generate(xbox, microcontroller)})) {{debounce[i] = {Debounce};{outputVar} |= (1 << {outputBit});}} else if (debounce[i]) {{ debounce[i]--; {outputVar} |= (1 << {outputBit});}}";
        }
    }
}
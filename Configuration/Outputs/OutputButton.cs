using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class OutputButton : Output
{
    protected OutputButton(ConfigViewModel model, IInput? input, Color ledOn, Color ledOff, int debounce): base(model, input, ledOn, ledOff)
    {
        Debounce = debounce;
    }
    public int Debounce { get; set; }
    public abstract string GenerateIndex(bool xbox);

    public abstract string GenerateOutput(bool xbox);

    public abstract bool IsStrum();


    public override string Generate(bool xbox, Microcontroller.Microcontroller microcontroller)
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
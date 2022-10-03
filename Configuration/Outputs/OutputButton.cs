using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class OutputButton : Output
{
    protected OutputButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, int debounce, string name): base(model, input, ledOn, ledOff, name)
    {
        Debounce = debounce;
    }
    public int Debounce { get; set; }
    public abstract string GenerateIndex(bool xbox);

    public abstract string GenerateOutput(bool xbox);

    public abstract bool IsStrum { get; }


    public override bool IsCombined => false;

    public override string Generate(bool xbox)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        var outputVar = GenerateOutput(xbox);
        var outputBit = GenerateIndex(xbox);
        if (Debounce == 0)
        {
            return $"if (({Input.Generate()})) {{{outputVar} |= (1 << {outputBit});}}";
        }
        else
        {
            return
                $"if (({Input.Generate()})) {{debounce[i] = {Debounce};{outputVar} |= (1 << {outputBit});}} else if (debounce[i]) {{ debounce[i]--; {outputVar} |= (1 << {outputBit});}}";
        }
    }
}
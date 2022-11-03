using System.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class OutputButton : Output
{
    protected OutputButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, int? ledIndex, int debounce, string name): base(model, input, ledOn, ledOff, ledIndex, name)
    {
        Debounce = debounce;
    }
    public int Debounce { get; set; }
    public abstract string GenerateIndex(bool xbox);

    public abstract string GenerateOutput(bool xbox);



    public override bool IsCombined => false;

    public string GenerateDebounceUpdate(int debounceIndex, bool xbox)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        var outputVar = GenerateOutput(xbox);
        var outputBit = GenerateIndex(xbox);
        return string.IsNullOrEmpty(outputBit) ? "" : $"if (debounce[{debounceIndex}]) {{ debounce[{debounceIndex}]--; {outputVar} |= (1 << {outputBit});}}";
    } 
    public override string Generate(bool xbox, bool shared, int debounceIndex, bool combined)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        var outputBit = GenerateIndex(xbox);
        if (string.IsNullOrEmpty(outputBit) || !shared) return "";
        if (combined && IsStrum)
        {
            var otherIndex = debounceIndex == 1 ? 0 : 1;
            return $"if (({Input.Generate()}) && (!debounce[{otherIndex}])) {{debounce[{debounceIndex}] = {Debounce+1};}}";
        }
        return $"if (({Input.Generate()})) {{debounce[{debounceIndex}] = {Debounce+1};}}";
    }

    public override string GenerateLedUpdate(int debounceIndex, bool xbox)
    {
        if (!AreLedsEnabled || !LedIndex.HasValue) return "";
        return @$"
            if (debounce[{debounceIndex}]) {{
                spi_transfer(APA102_SPI_PORT, 0xff);
                {string.Join("\n", Model.LedOrder.GetColors(LedOn).Select(b => $"spi_transfer(APA102_SPI_PORT, {b});"))}
            }} else {{
                spi_transfer(APA102_SPI_PORT, 0xff);
                {string.Join("\n", Model.LedOrder.GetColors(LedOff).Select(b => $"spi_transfer(APA102_SPI_PORT, {b});"))}
            }}";
    }
}
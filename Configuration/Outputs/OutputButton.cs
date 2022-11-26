using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class OutputButton : Output
{
    protected OutputButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte ledIndex, byte debounce,
        string name) : base(model, input, ledOn, ledOff, ledIndex, name)
    {
        Debounce = debounce;
    }

    public byte Debounce { get; set; }
    protected abstract string GenerateIndex(bool xbox);

    protected abstract string GenerateOutput(bool xbox);


    public override bool IsCombined => false;

    public override string Generate(bool xbox, bool shared, int debounceIndex, bool combined)
    {
        if (Input == null) throw new IncompleteConfigurationException(Name + " missing configuration");
        var outputBit = GenerateIndex(xbox);
        if (string.IsNullOrEmpty(outputBit)) return "";
        if (!shared)
        {
            var outputVar = GenerateOutput(xbox);
            return
                $"if (debounce[{debounceIndex}]) {{ debounce[{debounceIndex}]--; {outputVar} |= (1 << {outputBit});}}";
        }

        var led = "";
        if (AreLedsEnabled && LedIndex != 0)
        {
            led = $@"
            if (!ledState[{LedIndex - 1}].select) {{
                if (debounce[{debounceIndex}]) {{
                    {string.Join("\n", Model.LedType.GetColors(LedOn).Zip(new[] {'r', 'g', 'b'}).Select(b => $"ledState[{LedIndex - 1}].{b.Second} = {b.First};"))}
                }} else {{
                    {string.Join("\n", Model.LedType.GetColors(LedOff).Zip(new[] {'r', 'g', 'b'}).Select(b => $"ledState[{LedIndex - 1}].{b.Second} = {b.First};"))}
                }}
            }}";
        }

        if (combined && IsStrum)
        {
            var otherIndex = debounceIndex == 1 ? 0 : 1;
            return
                $"if (({Input.Generate(xbox)}) && (!debounce[{otherIndex}])) {{debounce[{debounceIndex}] = {Debounce + 1};}} {led}";
        }

        return $"if (({Input.Generate(xbox)})) {{debounce[{debounceIndex}] = {Debounce + 1};}} {led}";
    }
}
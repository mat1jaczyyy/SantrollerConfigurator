using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public abstract class OutputButton : Output
{
    protected OutputButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce,
        string name) : base(model, input, ledOn, ledOff, ledIndices, name)
    {
        Debounce = debounce;
    }

    public byte Debounce { get; set; }

    public abstract string GenerateOutput(DeviceEmulationMode mode);


    public override bool IsCombined => false;

    /// <summary>
    /// Generate bindings
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="debounceIndex"></param>
    /// <param name="combined"></param>
    /// <param name="extra">Used to provide extra statements that are called if the button is pressed</param>
    /// <returns></returns>
    /// <exception cref="IncompleteConfigurationException"></exception>
    public override string Generate(DeviceEmulationMode mode,  List<int> debounceIndex, bool combined, string extra)
    {
        if (Input==null) throw new IncompleteConfigurationException("Missing input!");
       
        var ifStatement = string.Join(" && ", debounceIndex.Select(x => $"debounce[{x}]"));
        var decrement = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]--;");
        var reset = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]={Debounce+1};");
        if (mode != DeviceEmulationMode.Shared)
        {
            var outputVar = GenerateOutput(mode);
            if (!outputVar.Any()) return "";
            var leds = "";
            if (AreLedsEnabled && LedIndices.Any())
            {
                leds += $@"if (!{ifStatement}) {{
                        {LedIndices.Aggregate("", (s, index) => s + @$"if (ledState[{index}].select == 1) {{
                            ledState[{index}].select = 0; 
                            {string.Join("\n", Model.LedType.GetColors(LedOff).Zip(new[] {'r', 'g', 'b'}).Select(b => $"ledState[{index}].{b.Second} = {b.First};"))};
                        }}")}
                    }}";
            }
            return
                @$"if ({ifStatement}) {{ 
                    {decrement} 
                    {outputVar} = true; 
                    {leds}
                }}";
        }

        var led = "";
        var led2 = "";
        if (AreLedsEnabled)
        {
            foreach (var index in LedIndices)
            {
                led += $@"
                if (ledState[{index}].select == 0 && {ifStatement}) {{
                    ledState[{index}].select = 1;
                    {string.Join("\n", Model.LedType.GetColors(LedOn).Zip(new[] {'r', 'g', 'b'}).Select(b => $"ledState[{index}].{b.Second} = {b.First};"))}
                }}";
                led2 += $@"
                if (!{ifStatement} && ledState[{index}].select == 1) {{
                    ledState[{index}].select = 1;
                    {string.Join("\n", Model.LedType.GetColors(LedOn).Zip(new[] {'r', 'g', 'b'}).Select(b => $"ledState[{index}].{b.Second} = {b.First};"))}
                }}
            ";
            }
        }

        if (combined && IsStrum)
        {
            var otherIndex = debounceIndex[0] == 1 ? 0 : 1;
            return
                $"if (({Input.Generate(mode)}) && (!debounce[{otherIndex}])) {{ {led2}; {reset}; {extra};}} {led}";
        }

        return $"if (({Input.Generate(mode)})) {{ {led2}; {reset}; {extra}; }} {led}";
    }

    public override void UpdateBindings()
    {
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.Conversions;
using GuitarConfiguratorSharp.NetCore.Configuration.Exceptions;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

public abstract class OutputButton : Output
{
    protected OutputButton(ConfigViewModel model, Input? input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce,
        string name) : base(model, input, ledOn, ledOff, ledIndices, name)
    {
        Debounce = debounce;
    }

    public byte Debounce { get; set; }
    public abstract string GenerateIndex(bool xbox);

    public abstract string GenerateOutput(bool xbox);


    public override bool IsCombined => false;

    public override string Generate(bool xbox, bool shared, List<int> debounceIndex, bool combined, string extra)
    {
        if (Input==null) throw new IncompleteConfigurationException("Missing input!");
        var outputBit = GenerateIndex(xbox);
        if (string.IsNullOrEmpty(outputBit)) return "";
        
        var ifStatement = string.Join(" && ", debounceIndex.Select(x => $"debounce[{x}]"));
        var decrement = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]--;");
        var reset = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]={Debounce+1};");
        if (!shared)
        {
            var outputVar = GenerateOutput(xbox);
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
                    {outputVar} |= (1 << {outputBit}); 
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
                $"if (({Input.Generate(xbox)}) && (!debounce[{otherIndex}])) {{ {led2}; {reset};}} {led}";
        }

        return $"if (({Input.Generate(xbox)})) {{ {led2}; {reset}; }} {led}";
    }

    public override void UpdateBindings()
    {
    }
}
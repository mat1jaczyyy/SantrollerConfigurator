using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public abstract class OutputButton : Output
{
    protected OutputButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce) : base(model, input, ledOn, ledOff, ledIndices)
    {
        Debounce = debounce;
    }

    public byte Debounce { get; set; }


    public override bool IsCombined => false;
    public override string LedOnLabel => "Pressed LED Colour";
    public override string LedOffLabel => "Released LED Colour";

    public abstract string GenerateOutput(ConfigField mode);

    /// <summary>
    ///     Generate bindings
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="debounceIndex"></param>
    /// <param name="extra">Used to provide extra statements that are called if the button is pressed</param>
    /// <param name="combinedExtra"></param>
    /// <param name="combinedDebounce"></param>
    /// <returns></returns>
    /// <exception cref="IncompleteConfigurationException"></exception>
    public override string Generate(ConfigField mode, List<int> debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce)
    {
        var ifStatement = string.Join(" && ", debounceIndex.Select(x => $"debounce[{x}]"));
        var extraStatement = "";
        if (mode == ConfigField.Shared && combinedExtra.Any())
        {
            extraStatement = " && " + combinedExtra;
        }

        var decrement = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]--;");
        var debounce = Debounce + 1;
        if (!Model.IsAdvancedMode)
        {
            debounce = Model.Debounce + 1;
        }

        var reset = debounceIndex.Aggregate("", (current1, input1) => current1 + $"debounce[{input1}]={debounce};");
        if (mode != ConfigField.Shared)
        {
            var outputVar = GenerateOutput(mode);
            if (!outputVar.Any()) return "";
            var leds = "";
            if (AreLedsEnabled && LedIndices.Any())
                leds += $@"if (!{ifStatement}) {{
                        {LedIndices.Aggregate("", (s, index) => s + @$"if (ledState[{index - 1}].select == 1) {{
                            ledState[{index - 1}].select = 0; 
                            {Model.LedType.GetLedAssignment(LedOff, index)}
                        }}")}
                    }}";

            return
                @$"if ({ifStatement}) {{ 
                    {decrement} 
                    {outputVar} = true; 
                    {leds}
                    {extra}
                }}";
        }

        var led = "";
        var led2 = "";
        if (AreLedsEnabled)
            foreach (var index in LedIndices)
            {
                led += $@"
                if (ledState[{index - 1}].select == 0 && {ifStatement}) {{
                    ledState[{index - 1}].select = 1;
                    {Model.LedType.GetLedAssignment(LedOn, index)}
                }}";
                led2 += $@"
                if (!{ifStatement} && ledState[{index - 1}].select == 1) {{
                    ledState[{index - 1}].select = 1;
                    {Model.LedType.GetLedAssignment(LedOn, index)}
                }}
            ";
            }

        return $"if (({Input.Generate(mode)} {extraStatement})) {{ {led2} {reset} {extra} }} {led}";
    }

    public override void UpdateBindings()
    {
    }
}
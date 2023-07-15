using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Exceptions;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Outputs;

public abstract class OutputButton : Output
{
    private readonly ObservableAsPropertyHelper<float> _debounceDisplay;
    protected OutputButton(ConfigViewModel model, Input input, Color ledOn, Color ledOff, byte[] ledIndices,
        byte debounce, bool childOfCombined) : base(model, input, ledOn, ledOff, ledIndices, childOfCombined)
    {
        Debounce = debounce;
        _debounceDisplay = this.WhenAnyValue(x => x.Debounce)
            .Select(x => x / 10.0f)
            .ToProperty(this, x => x.DebounceDisplay);
    }

    public byte Debounce { get; set; }

    public float DebounceDisplay
    {
        get => _debounceDisplay.Value;
        set => Debounce = (byte) (value * 10);
    }

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
    /// <param name="macros"></param>
    /// <returns></returns>
    /// <exception cref="IncompleteConfigurationException"></exception>
    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros)
    {
        var ifStatement = $"debounce[{debounceIndex}]";
        var extraStatement = "";
        if (mode == ConfigField.Shared && combinedExtra.Any()) extraStatement = " && " + combinedExtra;

        var debounce = Debounce;
        if (!Model.IsAdvancedMode)
        {
            if (this is GuitarButton {IsStrum: true} && Model.StrumDebounce > 0)
            {
                debounce = (byte) Model.StrumDebounce;
            }
            else
            {
                debounce = (byte) Model.Debounce;
            }
        }
        if (!Model.Deque)
        {
            // If we aren't using queue based inputs, then we want ms based inputs, not ones based on 0.1ms
            debounce /= 10;
        }
        debounce += 1;
        
        if (mode != ConfigField.Shared)
        {
            if (Model.Deque && this is GuitarButton {IsStrum: false})
            {
                ifStatement = $"{GenerateOutput(ConfigField.Shared).Replace("report->", "current_queue_report.")}";
            }
            var outputVar = GenerateOutput(mode);
            return outputVar.Any()
                ? @$"if ({ifStatement}) {{ 
                    {outputVar} = true; 
                    {extra}
                }}"
                : "";
        }
        
        var gen = Input.Generate();
        var reset = $"debounce[{debounceIndex}]={debounce};";

        if (Input is MacroInput)
        {
            foreach (var input in Input.Inputs())
            {
                var gen2 = input.Generate();
                if (!macros.TryGetValue(gen2, out var inputs2)) continue;
                if (Input.InnermostInput() is WiiInput wiiInput)
                {
                    extra += string.Join("\n",
                        inputs2.Where((s) =>
                                s.Item2 is WiiInput wiiInput2 &&
                                wiiInput2.WiiControllerType == wiiInput.WiiControllerType)
                            .Select(s => $"debounce[{s.Item1}]=0;"));
                }
                else
                {
                    extra += string.Join("\n", inputs2.Select(s => $"debounce[{s.Item1}]=0;"));
                }
            }
        }

        var queue = "";
        if (Model.Deque && this is GuitarButton {IsStrum: false}) {
            queue = $"if ({ifStatement}) {{{GenerateOutput(ConfigField.Shared).Replace("report->", "current_queue_report.")} = true;}}";
        }

        return $"if (({gen} {extraStatement})) {{ {reset} {extra} }} {queue}";
    }

    public override void UpdateBindings()
    {
    }
}
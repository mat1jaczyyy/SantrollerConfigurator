using Avalonia.Media;
using Dahomey.Json.Attributes;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Json;

[JsonDiscriminator("cb")]
public class JsonControllerButton : JsonOutput
{
    public override JsonInput? Input { get; }
    public override Color LedOn { get; }
    public override Color LedOff { get; }

    public int Debounce { get; }
    public StandardButtonType Type { get; }

    public JsonControllerButton(JsonInput? input, Color ledOn, Color ledOff, int debounce, StandardButtonType type)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Debounce = debounce;
        Type = type;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new ControllerButton(model, Input?.Generate(microcontroller), LedOn, LedOff, Debounce, Type);
    }
}
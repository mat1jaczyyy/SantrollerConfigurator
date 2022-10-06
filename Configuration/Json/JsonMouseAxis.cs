using Avalonia.Media;
using Dahomey.Json.Attributes;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Json;

[JsonDiscriminator("ma")]
public class JsonMouseAxis : JsonOutput
{
    public override JsonInput? Input { get; }
    public override Color LedOn { get; }
    public override Color LedOff { get; }
    public float Multiplier { get; }
    public int Offset { get; }
    public int Deadzone { get; }

    public MouseAxisType Type { get; }

    public JsonMouseAxis(JsonInput? input, MouseAxisType type, Color ledOn, Color ledOff, float multiplier, int offset,
        int deadzone)
    {
        Input = input;
        LedOn = ledOn;
        LedOff = ledOff;
        Multiplier = multiplier;
        Offset = offset;
        Deadzone = deadzone;
        Type = type;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new MouseAxis(model, Input?.Generate(microcontroller), LedOn, LedOff, Multiplier, Offset, Deadzone,
            Type);
    }
}
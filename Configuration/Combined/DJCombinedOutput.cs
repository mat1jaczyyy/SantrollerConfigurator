using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.Configuration.PS2;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Combined;
public class DjCombinedOutput : TwiOutput
{

    private static readonly Dictionary<DjInputType, StandardButtonType> Buttons = new()
    {
        {DjInputType.LeftAny, StandardButtonType.Lb},
        {DjInputType.LeftGreen, StandardButtonType.A},
        {DjInputType.LeftRed, StandardButtonType.B},
        {DjInputType.LeftBlue, StandardButtonType.X},
        {DjInputType.RightAny, StandardButtonType.Rb},
        {DjInputType.RightGreen, StandardButtonType.A},
        {DjInputType.RightRed, StandardButtonType.B},
        {DjInputType.RightBlue, StandardButtonType.X},
    };
    private readonly Microcontroller _microcontroller;

    public DjCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda = null, int? scl = null) : base(model, microcontroller, DjInput.DjTwiType, DjInput.DjTwiFreq, "DJ", sda, scl)
    {
        _microcontroller = microcontroller;
    }

    public override bool IsCombined => true;

    public override SerializedOutput GetJson()
    {
        return new SerializedDjCombinedOutput(LedOn, LedOff, Sda, Scl);
    }

    public override string Generate(bool xbox, int debounceIndex)
    {
        return "";
    }

    public override IReadOnlyList<Output> GetOutputs(IList<Output> bindings) => GetBindings(bindings);
    private IReadOnlyList<Output> GetBindings(IList<Output> bindings)
    {
        var inputs = bindings.Select(s => s.Input?.InnermostInput()).Where(s => s is DjInput).Cast<DjInput>()
            .Select(s => s.Input).ToHashSet();
        var outputs = (from pair in Buttons where !inputs.Contains(pair.Key) select new ControllerButton(Model, new DjInput(pair.Key, _microcontroller), Colors.Transparent, Colors.Transparent, 5, pair.Value)).Cast<Output>().ToList();

        if (!inputs.Contains(DjInputType.LeftTurntable))
        {
            outputs.Add(new ControllerAxis(Model, new DjInput(DjInputType.LeftTurntable, _microcontroller), Colors.Transparent,
                Colors.Transparent, 1,
                0, 0, StandardAxisType.LeftStickX));
        }
        if (!inputs.Contains(DjInputType.RightTurnable))
        {
            outputs.Add(new ControllerAxis(Model, new DjInput(DjInputType.RightTurnable, _microcontroller), Colors.Transparent,
                Colors.Transparent, 1,
                0, 0, StandardAxisType.LeftStickY));
        }

        return outputs;
    }
}
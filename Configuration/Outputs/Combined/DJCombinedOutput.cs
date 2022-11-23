using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media;
using GuitarConfiguratorSharp.NetCore.Configuration.DJ;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Serialization;
using GuitarConfiguratorSharp.NetCore.Configuration.Types;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Outputs.Combined;

public class DjCombinedOutput : CombinedTwiOutput
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
    private readonly AvaloniaList<Output> _outputs = new();

    public DjCombinedOutput(ConfigViewModel model, Microcontroller microcontroller, int? sda = null, int? scl = null,
        IReadOnlyCollection<Output>? outputs = null) :
        base(model, microcontroller, DjInput.DjTwiType, DjInput.DjTwiFreq, "DJ", sda, scl)
    {
        _microcontroller = microcontroller;
        if (outputs != null)
        {
            _outputs = new AvaloniaList<Output>(outputs);
        }
        else
        {
            CreateDefaults();
        }
    }

    public void CreateDefaults()
    {
        _outputs.Clear();
        _outputs.AddRange(Buttons.Select(pair => new ControllerButton(Model,
            new DjInput(pair.Key, Model, _microcontroller, combined: true),
            Colors.Transparent, Colors.Transparent, null, 5, pair.Value)));
        _outputs.Add(new ControllerAxis(Model, new DjInput(DjInputType.LeftTurntable, Model, _microcontroller, combined: true),
            Colors.Transparent,
            Colors.Transparent, null, 1,
            0, 0, StandardAxisType.LeftStickX));
        _outputs.Add(new ControllerAxis(Model, new DjInput(DjInputType.RightTurnable, Model, _microcontroller, combined: true),
            Colors.Transparent,
            Colors.Transparent, null, 1,
            0, 0, StandardAxisType.LeftStickY));
    }

    public override SerializedOutput Serialize()
    {
        return new SerializedDjCombinedOutput(Sda, Scl, _outputs.ToList());
    }

    private bool _detectedLeft;

    public bool DetectedLeft
    {
        get => _detectedLeft;
        set => this.RaiseAndSetIfChanged(ref _detectedLeft, value);
    }

    private bool _detectedRight;

    public bool DetectedRight
    {
        get => _detectedRight;
        set => this.RaiseAndSetIfChanged(ref _detectedRight, value);
    }

    public override void Update(List<Output> modelBindings, Dictionary<int, int> analogRaw,
        Dictionary<int, bool> digitalRaw, byte[] ps2Raw,
        byte[] wiiRaw, byte[] djLeftRaw,
        byte[] djRightRaw, byte[] gh5Raw, byte[] ghWtRaw, byte[] ps2ControllerType, byte[] wiiControllerType)
    {
        base.Update(modelBindings, analogRaw, digitalRaw, ps2Raw, wiiRaw, djLeftRaw, djRightRaw, gh5Raw, ghWtRaw,
            ps2ControllerType,
            wiiControllerType);
        DetectedLeft = djLeftRaw.Any();
        DetectedRight = djRightRaw.Any();
    }

    public override AvaloniaList<Output> Outputs => _outputs;
}
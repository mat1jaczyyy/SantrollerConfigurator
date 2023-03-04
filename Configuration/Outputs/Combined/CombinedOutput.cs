using System;
using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedOutput : Output
{
    protected CombinedOutput(ConfigViewModel model, Input input, string name) : base(model, input, Colors.Black,
        Colors.Black, Array.Empty<byte>(), name)
    {
    }

    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override bool IsCombined => true;
    public override bool IsStrum => false;
    public override bool IsKeyboard => false;
    public override bool IsController => true;


    public override bool Valid => true;

    public override string Generate(ConfigField mode, List<int> debounceIndex, bool combined, string extra)
    {
        return "";
    }

    public override string GetImagePath(DeviceControllerType type, RhythmType rhythmType)
    {
        return $"Combined/{Name}.png";
    }
}
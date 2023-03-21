using System;
using System.Collections.Generic;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedOutput : Output
{
    protected CombinedOutput(ConfigViewModel model, Input input) : base(model, input, Colors.Black,
        Colors.Black, Array.Empty<byte>())
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
}
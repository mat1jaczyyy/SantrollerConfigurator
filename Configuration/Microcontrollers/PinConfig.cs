using System;
using System.Collections.Generic;
using System.Linq;
using GuitarConfiguratorSharp.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;

public abstract class PinConfig : ReactiveObject
{
    protected PinConfig(ConfigViewModel model)
    {
        Model = model;
    }

    public abstract string Type { get; }
    public abstract string Definition { get; }
    public abstract string Generate();

    public abstract IEnumerable<int> Pins { get; }
    
    protected readonly ConfigViewModel Model;

    protected void Update()
    {
        Model.UpdateErrors();
    }

    public string? ErrorText => CalculateError();

    private string? CalculateError()
    {
        var configs = Model.GetPins(Type);

        var ret = (configs.Select(pinConfig => new {pinConfig, conflicting = pinConfig.Value.Intersect(Pins).ToList()})
            .Where(@t => @t.conflicting.Any())
            .Select(@t => $"{@t.pinConfig.Key}: {string.Join(", ", @t.conflicting.Select(s => Model.MicroController!.GetPinForMicrocontroller(s, true, true)))}")).ToList();

        return ret.Any() ? string.Join(", ", ret) : null;
    }
}
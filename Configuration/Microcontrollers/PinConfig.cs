using System.Collections.Generic;
using System.Linq;
using GuitarConfigurator.NetCore.ViewModels;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class PinConfig : ReactiveObject
{
    protected readonly ConfigViewModel Model;

    protected PinConfig(ConfigViewModel model)
    {
        Model = model;
    }

    public abstract string Type { get; }
    public abstract string Definition { get; }

    public abstract IEnumerable<int> Pins { get; }

    public string? ErrorText => CalculateError();
    protected virtual bool Reassignable => true;
    public abstract string Generate();

    protected void Update()
    {
        Model.UpdateErrors();
    }

    protected virtual string? CalculateError()
    {
        if (Pins.Any(x => x == -1) && Reassignable)
        {
            return Resources.ErrorPinConfigurationMissing;
        }
        var configs = Model.GetPins(Type);

        var ret = configs.Select(pinConfig => new {pinConfig, conflicting = pinConfig.Value.Intersect(Pins).ToList()})
            .Where(t => t.conflicting.Any())
            .Select(t =>
                $"{t.pinConfig.Key}: {string.Join(", ", t.conflicting.Select(s => Model.Microcontroller.GetPinForMicrocontroller(s, this is TwiConfig, this is SpiConfig)))}")
            .ToList();

        return ret.Any() ? string.Join(", ", ret) : null;
    }
}
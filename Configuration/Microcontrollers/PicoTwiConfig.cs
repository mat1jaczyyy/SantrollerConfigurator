using System.Linq;
using GuitarConfigurator.NetCore.Assets;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public class PicoTwiConfig : TwiConfig
{
    public PicoTwiConfig(ConfigViewModel model, string type, int sda, int scl, int clock) : base(model, type, sda, scl,
        clock)
    {
    }

    public int Index => Sda == -1 ? 0 : Pico.TwiIndexByPin[Sda];
    public override string Definition => $"TWI_{Index}";
    protected override bool Reassignable => true;

    protected override string? CalculateError()
    {
        var ret = base.CalculateError();
        if (ret != null) return ret;
        if (Pico.TwiIndexByPin[Sda] != Pico.TwiIndexByPin[Scl])
        {
            return Resources.DifferentI2CGroup;
        }
        var ret2 = Model.Bindings.Items
            .Where(output => output.GetPinConfigs().OfType<PicoTwiConfig>().Any(s => s != this && s.Index == Index))
            .Select(output => string.Format(Resources.I2CGroup, output.LocalisedName, Index))
            .ToList();
        return ret2.Any() ? string.Join(", ", ret2) : null;
    }
}
using System.Collections.Generic;
using GuitarConfiguratorSharp.NetCore.Utils;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller;

public class Pico : Microcontroller
{
    private const int GpioCount = 30;
    private const int PinA0 = 26;

    public int SpiRxPico { get; set; } = 0xff;
    public int SpiTxPico { get; set; } = 0xff;
    public int SpiCsnPico { get; set; } = 0xff;
    public int SpiSckPico { get; set; } = 0xff;
    public int I2CSdaPico { get; set; } = 0xff;
    public int I2CSclPico { get; set; } = 0xff;

    public override int SpiRx => SpiRxPico;

    public override int SpiTx => SpiTxPico;

    public override int SpiCSn => SpiCsnPico;
    public override int SpiSck => SpiSckPico;

    public override int I2CSda => I2CSdaPico;

    public override int I2CScl => I2CSclPico;

    public override Board Board {get;}

    public Pico(Board board) {
        this.Board = board;
    }

    public override string GenerateDigitalRead(int pin, bool pullUp)
    {
        // Invert on pullup
        if (pullUp)
        {
            return $"(sio_hw->gpio_in & (1 << {pin})) == 0";
        }

        return $"sio_hw->gpio_in & (1 << {pin})";
    }
    public override string GenerateAnalogRead(int pin, int index, int offset, float multiplier, int deadzone, bool xbox)
    {
        var function = xbox ? "adc_xbox" : "adc";
        return $"{function}({pin - PinA0}, {offset}, {(int)(multiplier * 64)}, {deadzone})";
    }

    public override string GenerateAnalogTriggerRead(int pin, int index, int offset, float multiplier, int deadzone, bool xbox)
    {
        var function = xbox ? "adc_trigger_xbox" : "adc_trigger";
        return $"{function}({pin - PinA0}, {offset}, {(int)(multiplier * 64)}, {deadzone})";
    }

    public override string GenerateSkip(bool spiEnabled, bool i2CEnabled)
    {
        List<int> skippedPins = new List<int>();
        if (spiEnabled)
        {
            skippedPins.Add(SpiCSn);
            skippedPins.Add(SpiRx);
            skippedPins.Add(SpiTx);
            skippedPins.Add(SpiSck);
        }
        if (i2CEnabled)
        {
            skippedPins.Add(I2CScl);
            skippedPins.Add(I2CSda);
        }
        int skip = 0;
        foreach (var pin in skippedPins)
        {
            if (pin != 0xFF)
            {
                skip |= 1 << pin;
            }
        }
        return skip.ToString();
    }

    public override string GenerateInit(IEnumerable<Binding> bindings)
    {
        IEnumerable<DirectDigital> buttons = bindings.FilterCast<Binding, DirectDigital>();
        IEnumerable<DirectAnalog> axes = bindings.FilterCast<Binding, DirectAnalog>();
        string ret = "";
        foreach (var button in buttons)
        {
            bool up = button.PinMode == DevicePinMode.BusKeep || button.PinMode == DevicePinMode.Ground;
            bool down = button.PinMode == DevicePinMode.BusKeep || button.PinMode == DevicePinMode.VCC;
            ret += $"gpio_init({button.Pin});";
            ret += $"gpio_set_dir({button.Pin},false);";
            ret += $"gpio_set_pulls({button.Pin},{up.ToString().ToLower()},{down.ToString().ToLower()});";
        }
        foreach (var axis in axes)
        {
            ret += $"adc_gpio_init({axis.Pin});";
        }
        return ret;
    }

    public override int GetChannel(int pin)
    {
        return pin;
    }

    public override string GetPin(int pin)
    {
        string ret = $"GP{pin}";
        if (pin >= 26)
        {
            ret += $" / ADC{pin - 26}";
        }
        return ret;
    }
}
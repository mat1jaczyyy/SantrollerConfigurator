using System.Collections.Generic;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller
{
    public abstract class Microcontroller
    {
        public abstract string GenerateDigitalRead(int pin, bool pullUp);
        public abstract string GenerateAnalogRead(int pin, int index, int offset, float multiplier, int deadzone, bool xbox);
        public abstract string GenerateAnalogTriggerRead(int pin, int index, int offset, float multiplier, int deadzone, bool xbox);
        public abstract string GenerateSkip(bool spiEnabled, bool i2CEnabled);

        public abstract int GetChannel(int pin);

        public abstract string GenerateInit(IEnumerable<Binding> bindings);
        public abstract int SpiRx { get; }

        public abstract string GetPin(int pin);

        public abstract int SpiTx { get; }
        public abstract int SpiSck { get; }
        public abstract int SpiCSn { get; }
        public abstract int I2CSda { get; }
        public abstract int I2CScl { get; }

        public abstract Board Board {get;}
        public string GenerateAnalogReadRaw(IEnumerable<Binding> bindings, int pin) {
            return $"adc_raw({pin})";
        }
    }
}
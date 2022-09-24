using System.Collections.Generic;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller
{
    public abstract class Microcontroller
    {
        public abstract string GenerateDigitalRead(int pin, bool pullUp);
        public abstract string GenerateSkip(bool spiEnabled, bool i2CEnabled);

        public abstract int GetChannel(int pin);

        public abstract string GenerateInit(List<IOutput> bindings);
        public abstract int SpiRx { get; }

        public abstract string GetPin(int pin);

        public abstract int SpiTx { get; }
        public abstract int SpiSck { get; }
        public abstract int SpiCSn { get; }
        public abstract int I2CSda { get; }
        public abstract int I2CScl { get; }

        public abstract Board Board {get;}
        public string GenerateAnalogRead(int pin) {
            return $"adc_raw({pin})";
        }
    }
}
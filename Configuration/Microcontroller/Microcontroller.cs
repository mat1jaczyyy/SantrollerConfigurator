using System.Collections.Generic;
using Avalonia.Collections;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontroller
{
    public abstract class Microcontroller
    {
        public abstract string GenerateDigitalRead(int pin, bool pullUp);
        public abstract string GenerateDefinitions();

        public abstract int GetChannel(int pin);

        public abstract string GenerateInit(List<Output> bindings);

        public abstract string GetPin(int pin);

        public readonly AvaloniaList<SpiConfig> SpiConfigs = new();
        public readonly AvaloniaList<TwiConfig> TwiConfigs = new();
        
        // TODO: call the below stuff in Inputs that use i2c or spi, and with APA102 stuff or RF stuff (eventually)
        public abstract SpiConfig? AssignSpiPins(string type, int mosi, int miso, int sck, bool cpol, bool cpha,
            bool msbfirst,
            int clock);
        public abstract TwiConfig? AssignTwiPins(string type, int sda, int scl, int clock);
        
        public abstract bool TwiPinsFree { get; }
        public abstract bool SpiPinsFree { get; }

        public TwiConfig? GetTwiForType(string type)
        {
            foreach (var config in TwiConfigs)
            {
                if (config.Type == type)
                {
                    return config;
                }
            }

            return null;
        }

        public SpiConfig? GetSpiForType(string type)
        {
            foreach (var config in SpiConfigs)
            {
                if (config.Type == type)
                {
                    return config;
                }
            }

            return null;
        }

        public abstract bool HasConfigurableSpiPins { get; }
        public abstract bool HasConfigurableTwiPins { get; }
        public abstract List<KeyValuePair<int, SpiPinType>> SpiPins(string type);
        public abstract List<KeyValuePair<int, TwiPinType>> TwiPins(string type);

        public abstract void UnAssignSPIPins(string type);
        public abstract void UnAssignTWIPins(string type);

        public abstract Board Board {get;}
        public string GenerateAnalogRead(int pin) {
            return $"adc_raw({pin})";
        }
    }
}
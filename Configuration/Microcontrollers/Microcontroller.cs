using System.Collections.Generic;
using Avalonia.Collections;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers
{
    public abstract class Microcontroller
    {
        public abstract string GenerateDigitalRead(int pin, bool pullUp);
        public abstract string GenerateDigitalWrite(int pin, bool val);
        public abstract string GenerateDefinitions();

        public abstract int GetChannel(int pin);

        public abstract string GenerateInit(List<Output> bindings);

        public abstract string GetPin(int pin);

        public readonly AvaloniaList<SpiConfig> SpiConfigs = new();
        public readonly AvaloniaList<TwiConfig> TwiConfigs = new();
        
        // TODO: call the below stuff for APA102 stuff or RF stuff (eventually)
        public abstract SpiConfig? AssignSpiPins(string type, int mosi, int miso, int sck, bool cpol, bool cpha,
            bool msbfirst,
            int clock);
        public abstract TwiConfig? AssignTwiPins(string type, int sda, int scl, int clock);

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

        public abstract string GenerateAckDefines(int ack);

        public abstract List<int> SupportedAckPins();

        public abstract List<KeyValuePair<int, SpiPinType>> SpiPins(string type);
        public abstract List<KeyValuePair<int, TwiPinType>> TwiPins(string type);
        
        public abstract void UnAssignSpiPins(string type);
        public abstract void UnAssignTwiPins(string type);

        public abstract Board Board {get;}
        public string GenerateAnalogRead(int pin) {
            return $"adc_raw({pin})";
        }

        public abstract string GeneratePulseRead(int pin, PulseMode mode, int timeout);
    }
}
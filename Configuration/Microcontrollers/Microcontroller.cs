using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers
{
    public abstract class Microcontroller
    {
        public abstract string GenerateDigitalRead(int pin, bool pullUp);
        public abstract string GenerateDigitalWrite(int pin, bool val);
        public string GenerateDefinitions()
        {
            return PinConfigs.Aggregate("", (current, config) => current + config.Generate());
        }

        public abstract int GetChannel(int pin);

        public abstract string GenerateInit();

        public abstract string GetPin(int pin);

        public readonly AvaloniaList<PinConfig> PinConfigs = new();
        
        // TODO: call the below stuff for APA102 stuff or RF stuff (eventually)
        public abstract SpiConfig? AssignSpiPins(string type, int mosi, int miso, int sck, bool cpol, bool cpha,
            bool msbfirst,
            int clock);
        public abstract TwiConfig? AssignTwiPins(string type, int sda, int scl, int clock);

        public TwiConfig? GetTwiForType(string type)
        {
            return (TwiConfig?)PinConfigs.FirstOrDefault(config => config is TwiConfig && config.Type == type);
        }

        public SpiConfig? GetSpiForType(string type)
        {
            return (SpiConfig?)PinConfigs.FirstOrDefault(config => config is SpiConfig && config.Type == type);
        }

        public abstract string GenerateAckDefines(int ack);

        public abstract List<int> SupportedAckPins();

        public abstract List<KeyValuePair<int, SpiPinType>> SpiPins(string type);
        public abstract List<KeyValuePair<int, TwiPinType>> TwiPins(string type);
        
        public abstract void UnAssignPins(string type);

        public void UnAssignAll()
        {
            PinConfigs.Clear();
        }

        public abstract Board Board {get;}
        public string GenerateAnalogRead(int pin) {
            return $"adc({pin})";
        }

        public abstract string GeneratePulseRead(int pin, PulseMode mode, int timeout);
        public abstract int GetFirstAnalogPin();
        public abstract void AssignPin(PinConfig pinConfig);

        public abstract List<int> GetFreePins();
        public abstract Dictionary<int, int> GetPortsForTicking(IEnumerable<DevicePin> digital);
        public abstract void PinsFromPortMask(int port, int mask, byte pins,
            Dictionary<int, bool> digitalRaw);

        public abstract int GetAnalogMask(DevicePin devicePin);
        
        public DirectPinConfig GetOrSetPin(string type, int pin, DevicePinMode devicePinMode)
        {
            var existing = PinConfigs.OfType<DirectPinConfig>().FirstOrDefault(s => s.Type == type);
            if (existing != null) return existing;
            var config = new DirectPinConfig(type, pin, devicePinMode);
            PinConfigs.Add(config);
            return config;
        }
    }
}
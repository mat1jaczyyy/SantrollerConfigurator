using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using GuitarConfigurator.NetCore.Configuration.Outputs;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Microcontrollers;

public abstract class Microcontroller
{
    public readonly AvaloniaList<PinConfig> PinConfigs = new();

    public abstract Board Board { get; }

    public abstract List<int> AnalogPins { get; }

    public abstract bool TwiAssignable { get; }
    public abstract bool SpiAssignable { get; }
    public abstract string GenerateDigitalRead(int pin, bool pullUp);
    public abstract string GenerateDigitalWrite(int pin, bool val);
    public abstract string GenerateAnalogWrite(int pin, string val);

    public string GenerateDefinitions()
    {
        return PinConfigs.Aggregate("", (current, config) => current + config.Generate());
    }

    public abstract int GetChannel(int pin, bool reconfigurePin);

    public abstract string GenerateInit();

    public string GetPin(int possiblePin, int selectedPin, IEnumerable<Output> outputs, bool twi, bool spi,
        IEnumerable<PinConfig> pinConfigs, ConfigViewModel model, bool addText)
    {
        var selectedConfig = pinConfigs.Where(s => s.Pins.Contains(selectedPin));
        var apa102 = PinConfigs.Where(s => s.Type == ConfigViewModel.Apa102SpiType && s.Pins.Contains(possiblePin))
            .Select(s => s.Type); 
        var unoMega = PinConfigs.Where(s => (s.Type == ConfigViewModel.UnoPinTypeRx || s.Type == ConfigViewModel.UnoPinTypeTx) && s.Pins.Contains(possiblePin))
            .Select(s => s.Type); 

        var output = string.Join(" - ",
            outputs.Where(o =>
                    o.GetPinConfigs().Except(selectedConfig).Any(s => s.Pins.Contains(possiblePin)))
                .Select(s => s.GetName(model.DeviceType, model.RhythmType)).Concat(apa102).Concat(unoMega));
        var ret = GetPinForMicrocontroller(possiblePin, twi, spi);
        if (!string.IsNullOrEmpty(output) && addText) return "* " + ret + " - " + output;

        return ret;
    }

    public abstract string GetPinForMicrocontroller(int pin, bool twi, bool spi);

    public abstract SpiConfig? AssignSpiPins(ConfigViewModel model, string type, int mosi, int miso, int sck, bool cpol,
        bool cpha,
        bool msbfirst,
        uint clock);

    public abstract TwiConfig? AssignTwiPins(ConfigViewModel model, string type, int sda, int scl, int clock);

    public TwiConfig? GetTwiForType(string type)
    {
        return (TwiConfig?) PinConfigs.FirstOrDefault(config => config is TwiConfig && config.Type == type);
    }

    public SpiConfig? GetSpiForType(string type)
    {
        return (SpiConfig?) PinConfigs.FirstOrDefault(config => config is SpiConfig && config.Type == type);
    }

    public abstract string GenerateAckDefines(int ack);

    public abstract List<int> SupportedAckPins();

    public abstract List<KeyValuePair<int, SpiPinType>> SpiPins(string type);
    public abstract List<KeyValuePair<int, TwiPinType>> TwiPins(string type);
    public abstract List<KeyValuePair<int, SpiPinType>> FreeSpiPins(string type);
    public abstract List<KeyValuePair<int, TwiPinType>> FreeTwiPins(string type);

    public abstract void UnAssignPins(string type);

    public void UnAssignAll()
    {
        PinConfigs.Clear();
    }

    public abstract string GenerateAnalogRead(int pin);

    public abstract int GetFirstAnalogPin();
    public abstract void AssignPin(PinConfig pinConfig);

    public abstract List<int> GetAllPins(bool isAnalog);
    public abstract List<int> GetPwmPins();

    public abstract Dictionary<int, int> GetPortsForTicking(IEnumerable<DevicePin> digital);

    public abstract void PinsFromPortMask(int port, int mask, byte pins,
        Dictionary<int, bool> digitalRaw);

    public abstract int GetAnalogMask(DevicePin devicePin);

    public abstract int GetFirstDigitalPin();

    public DirectPinConfig GetOrSetPin(ConfigViewModel model, string type, int pin, DevicePinMode devicePinMode)
    {
        var existing = PinConfigs.OfType<DirectPinConfig>().FirstOrDefault(s => s.Type == type);
        if (existing != null) return existing;
        var config = new DirectPinConfig(model, type, pin, devicePinMode);
        PinConfigs.Add(config);
        return config;
    }
}
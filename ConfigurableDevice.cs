using Device.Net;
using GuitarConfiguratorSharp.Utils;
public interface ConfigurableDevice {
    public bool IsSameDevice(IDevice device);
    public bool IsSameDevice(PlatformIOPort port);
    public bool IsSameDevice(string path);

    public bool MigrationSupported { get; }

    public delegate void DeviceInitialisedHandler(ConfigurableDevice device);

    public static event DeviceInitialisedHandler? DeviceInitialised;

    protected static void FinishedInitialising(ConfigurableDevice device) {
        ConfigurableDevice.DeviceInitialised?.Invoke(device);
    }
}
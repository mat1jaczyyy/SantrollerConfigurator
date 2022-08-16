using Device.Net;
using GuitarConfiguratorSharp.Configuration;
using GuitarConfiguratorSharp.Utils;
public interface ConfigurableDevice {
    public bool IsSameDevice(IDevice device);
    public bool IsSameDevice(PlatformIOPort port);
    public bool IsSameDevice(string serial_or_path);

    public bool MigrationSupported { get; }
    public DeviceConfiguration? Configuration {get;}

    public void bootloader();
    public void bootloaderUSB();
}
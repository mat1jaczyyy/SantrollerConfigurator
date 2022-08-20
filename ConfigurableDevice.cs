using System.Threading.Tasks;
using LibUsbDotNet;
using GuitarConfiguratorSharp.Configuration;
using GuitarConfiguratorSharp.Utils;
public interface ConfigurableDevice {
    public bool IsSameDevice(PlatformIOPort port);
    public bool IsSameDevice(string serial_or_path);

    public bool MigrationSupported { get; }
    public DeviceConfiguration? Configuration {get;}

    public void Bootloader();
    public void BootloaderUSB();

    public void DeviceAdded(ConfigurableDevice device);

    public Task<string?> getUploadPort();
}
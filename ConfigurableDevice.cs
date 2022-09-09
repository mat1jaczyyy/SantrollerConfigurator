using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Configuration;
using GuitarConfiguratorSharp.NetCore.Utils;

namespace GuitarConfiguratorSharp.NetCore;

public interface IConfigurableDevice {
    public bool IsSameDevice(PlatformIoPort port);
    public bool IsSameDevice(string serialOrPath);

    public bool MigrationSupported { get; }
    public DeviceConfiguration? Configuration {get;}

    public void Bootloader();
    public void BootloaderUsb();

    public bool DeviceAdded(IConfigurableDevice device);

    public Task<string?> GetUploadPort();
}
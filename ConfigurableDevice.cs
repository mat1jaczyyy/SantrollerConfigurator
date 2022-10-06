using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore;

public interface IConfigurableDevice {
    public bool IsSameDevice(PlatformIoPort port);
    public bool IsSameDevice(string serialOrPath);

    public bool MigrationSupported { get; }

    public void Bootloader();
    public void BootloaderUsb();

    public bool DeviceAdded(IConfigurableDevice device);

    public void LoadConfiguration(ConfigViewModel model);

    public Task<string?> GetUploadPort();

    public bool IsAVR();
}
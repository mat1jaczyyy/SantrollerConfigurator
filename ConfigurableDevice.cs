using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore;

public interface IConfigurableDevice {
    public bool IsSameDevice(PlatformIoPort port);
    public bool IsSameDevice(string serialOrPath);

    public bool MigrationSupported { get; }

    public void Bootloader();
    public void BootloaderUsb();

    public bool DeviceAdded(IConfigurableDevice device);

    public void LoadConfiguration(ConfigViewModel model);

    public Task<string?> GetUploadPort();

    public bool IsAvr();
}
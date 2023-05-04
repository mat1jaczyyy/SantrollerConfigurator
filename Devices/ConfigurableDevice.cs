using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Configuration.Microcontrollers;
using GuitarConfigurator.NetCore.Utils;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Devices;

public interface IConfigurableDevice
{
    public bool MigrationSupported { get; }
    public bool IsSameDevice(PlatformIoPort port);
    public bool IsSameDevice(string serialOrPath);

    public void Bootloader();
    public void BootloaderUsb();

    public void DeviceAdded(IConfigurableDevice device);

    public Microcontroller GetMicrocontroller(ConfigViewModel model);

    public bool LoadConfiguration(ConfigViewModel model);

    public Task<string?> GetUploadPortAsync();
    public bool IsAvr();
    public bool IsGeneric();
    public bool IsPico();
    public bool IsMini();
    public bool IsESP32();
    void Reconnect();
    void Revert();
    bool HasDfuMode();
    bool Is32U4();
}
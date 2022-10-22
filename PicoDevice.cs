using System.Threading.Tasks;
using GuitarConfiguratorSharp.NetCore.Utils;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore;

public class PicoDevice : IConfigurableDevice
{
    private readonly string _path;

    public bool MigrationSupported => true;

    public PicoDevice(PlatformIo pio, string path)
    {
        _path = path;
    }

    public bool IsSameDevice(PlatformIoPort port)
    {
        return false;
    }

    public bool IsSameDevice(string serialOrPath)
    {
        return serialOrPath == _path;
    }

    public string GetPath()
    {
        return _path;
    }

    public override string ToString()
    {
        return $"Pico ({_path})";
    }

    public void Bootloader()
    {
    }

    public void BootloaderUsb()
    {
    }

    bool IConfigurableDevice.DeviceAdded(IConfigurableDevice device)
    {
        return false;
    }

    public async Task LoadConfiguration(ConfigViewModel model)
    {
        await model.SetDefaults(Board.FindMicrocontroller(Board.FindBoard("pico",0)));
    }

    public Task<string?> GetUploadPort()
    {
        return Task.FromResult((string?)_path);
    }

    public bool IsAvr()
    {
        return false;
    }
}
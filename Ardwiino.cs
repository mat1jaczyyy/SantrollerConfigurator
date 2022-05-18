
using Device.Net;
using Usb.Net;
using System;
using GuitarConfiguratorSharp.Utils;

public class Ardwiino : ConfigurableDevice 
{
    private IUsbDevice device;

    public Ardwiino(IDevice device)
    {
        this.device = (IUsbDevice)device;
        //TODO: legacy ardwiino, we will have to read the info from it and decode

    }

    public bool IsSameDevice(IDevice device) {
        return this.device == device;
    }
    public bool IsSameDevice(PlatformIOPort port) {
        return false;
    }

    public bool IsSameDevice(string path) {
        return false;
    }
    
    public override String ToString()
    {
        return $"Ardwiino";
    }
}
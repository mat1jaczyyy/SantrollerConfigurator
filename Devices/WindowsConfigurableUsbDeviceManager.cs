using System;
using System.Globalization;
using System.Linq;
using Avalonia.Threading;
using DynamicData;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.Main;
using LibUsbDotNet.WinUsb;
using Nefarius.Drivers.WinUSB;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;
using Version = SemanticVersioning.Version;

namespace GuitarConfigurator.NetCore.Devices;

#if Windows
public class ConfigurableUsbDeviceManager
{
    private readonly DeviceNotificationListener _deviceNotificationListener = new DeviceNotificationListener();
    private MainWindowViewModel _model;
    private const string RevisionString = "REV_";


    public ConfigurableUsbDeviceManager(MainWindowViewModel model)
    {
        _model = model;
        
    }

    public void Register()
    {
        _deviceNotificationListener.DeviceArrived += DeviceArrived;
        _deviceNotificationListener.DeviceRemoved += DeviceRemoved;

        _deviceNotificationListener.StartListen(DeviceInterfaceIds.UsbDevice);
        var instance = 0;
        while (Devcon.FindByInterfaceGuid(DeviceInterfaceIds.UsbDevice, out var path,
                   out var instanceId, instance++))
        {
            DeviceNotify(EventType.DeviceArrival, path);
        }
    }

    private void DeviceNotify(EventType eventType, string path)
    {
        var usbDevice = PnPDevice
            .GetDeviceByInterfaceId(path)
            .ToUsbPnPDevice();
        Console.WriteLine(path);
        var ids = UsbSymbolicName.Parse(path);
        Dispatcher.UIThread.Post(() =>
        {
            if (eventType == EventType.DeviceArrival)
            {
                var vid = ids.Vid;
                var pid = ids.Pid;
                var serial = ids.SerialNumber;
                if (vid == Dfu.DfuVid && (pid == Dfu.DfuPid16U2 || pid == Dfu.DfuPid8U2))
                {
                    _model.AvailableDevices.Add(new Dfu(new RegDeviceNotifyInfoEventArgs(new RegDeviceNotifyInfo(path, path, serial))));
                }
                else if (vid == 0x1209 && pid is 0x2882 or 0x2884)
                {
                    var children = usbDevice.GetProperty<string[]>(DevicePropertyKey.Device_Children);
                    foreach (var child in children)
                    {
                        try
                        {
                            var childDevice = PnPDevice
                                .GetDeviceByInstanceId(child)
                                .ToUsbPnPDevice();
                            var hardwareIds = childDevice.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds);
                            var product = childDevice.GetProperty<string>(DevicePropertyKey.Device_FriendlyName);
                            var childPath = childDevice.GetProperty<string>(DevicePropertyKey.Device_PDOName);
                            ushort revision = 0;
                            foreach (var id in hardwareIds)
                            {
                                var index = id.IndexOf(RevisionString, StringComparison.Ordinal);
                                if (index > -1)
                                {
                                    revision = ushort.Parse(id.Substring(index + RevisionString.Length, 4),
                                        NumberStyles.HexNumber);
                                }
                            }

                            Console.WriteLine(product);
                            Console.WriteLine(serial);
                            Console.WriteLine(revision);
                            WinUsbDevice.Open("\\\\?\\Global\\GLOBALROOT" + childPath, out var dev);
                            switch (product)
                            {
                                case "Santroller" when _model is {Programming: true, IsPico: false}:
                                    return;
                                case "Santroller":
                                    _model.AvailableDevices.Add(new Santroller(_model.Pio, child, dev, product, serial,
                                        revision));
                                    break;
                                case "Ardwiino" when _model.Programming:
                                case "Ardwiino" when revision == Ardwiino.SerialArdwiinoRevision:
                                    return;
                                case "Ardwiino":
                                    _model.AvailableDevices.Add(new Ardwiino(_model.Pio, child, dev, product, serial,
                                        revision));
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }
            else
            {
                _model.AvailableDevices.RemoveMany(
                    _model.AvailableDevices.Items.Where(device => device.IsSameDevice(path)));
            }
        });
    }

    private class RegDeviceNotifyInfoEventArgs : DeviceNotifyEventArgs
    {
        internal RegDeviceNotifyInfoEventArgs(IUsbDeviceNotifyInfo info)
        {
            Device = info;
        }
    }
    
    private class RegDeviceNotifyInfo : IUsbDeviceNotifyInfo
    {
        private readonly string _path;
        private readonly string _instanceId;
        private readonly string _serialNumber;

        public RegDeviceNotifyInfo(string path, string instanceId, string serialNumber)
        {
            _path = path;
            _instanceId = instanceId;
            _serialNumber = serialNumber;
        }

        public UsbSymbolicName SymbolicName => UsbSymbolicName.Parse(_instanceId);

        public string Name => _path;

        public Guid ClassGuid => DeviceInterfaceIds.UsbDevice;

        public int IdVendor => SymbolicName.Vid;

        public int IdProduct => SymbolicName.Pid;

        public string SerialNumber => _serialNumber;

        public bool Open(out UsbDevice usbDevice)
        {
            WinUsbDevice.Open(_path, out var winUsbDevice);
            usbDevice = winUsbDevice;
            return winUsbDevice != null && winUsbDevice.Open();
        }
    }

    private void DeviceArrived(DeviceEventArgs args)
    {
        DeviceNotify(EventType.DeviceArrival, args.SymLink);
    }

    private void DeviceRemoved(DeviceEventArgs args)
    {
        DeviceNotify(EventType.DeviceRemoveComplete, args.SymLink);
    }

    public void Dispose()
    {
        _deviceNotificationListener.DeviceArrived -= DeviceArrived;
        _deviceNotificationListener.DeviceRemoved -= DeviceRemoved;

// start listening for plugins or unplugs of GUID_DEVINTERFACE_USB_DEVICE interface devices
        _deviceNotificationListener.StopListen(DeviceInterfaceIds.UsbDevice);
    }
}
#endif
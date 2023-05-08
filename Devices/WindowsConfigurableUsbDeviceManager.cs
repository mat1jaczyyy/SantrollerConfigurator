#if Windows
using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using DynamicData;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.Main;
using LibUsbDotNet.WinUsb;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;
using ReactiveUI;

namespace GuitarConfigurator.NetCore.Devices;
public class ConfigurableUsbDeviceManager
{
    private readonly DeviceNotificationListener _deviceNotificationListener = new();
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

    private async void DeviceNotify(EventType eventType, string path)
    {
        await Task.Delay(200);
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            var ids = UsbSymbolicName.Parse(path);

            var usbDevice = PnPDevice
                .GetDeviceByInterfaceId(path, DeviceLocationFlags.Phantom)
                .ToUsbPnPDevice();
            if (eventType == EventType.DeviceArrival)
            {
                var vid = ids.Vid;
                var pid = ids.Pid;
                var serial = ids.SerialNumber;
                if (vid == Dfu.DfuVid && (pid == Dfu.DfuPid16U2 || pid == Dfu.DfuPid8U2))
                {
                    _model.AvailableDevices.Add(
                        new Dfu(new RegDeviceNotifyInfoEventArgs(new RegDeviceNotifyInfo(path, PnPDevice.GetInstanceIdFromInterfaceId(path), serial))));
                }
                else if((vid == 0x1209 && pid is 0x2882 or 0x2884) || vid == 0x12ba)
                {
                    var children = usbDevice.GetProperty<string[]>(DevicePropertyKey.Device_Children);
                    if (children == null)
                    {
                        return;
                    }
                    foreach (var child in children)
                    {
                        var childDevice = PnPDevice
                            .GetDeviceByInstanceId(child, DeviceLocationFlags.Phantom)
                            .ToUsbPnPDevice();
                        var childPath = childDevice.GetProperty<string>(DevicePropertyKey.Device_PDOName);

                        WinUsbDevice.Open("\\\\?\\Global\\GLOBALROOT" + childPath, out var dev);
                        if (dev != null)
                        {
                            var product = dev.Info.ProductString;
                            var revision = (ushort)dev.Info.Descriptor.BcdDevice;
                            switch (product)
                            {
                                case "Santroller" when _model is { Programming: true, IsPico: false }:
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
                    }
                }
            }
            else
            {
                var serial = ids.SerialNumber;
                _model.AvailableDevices.RemoveMany(
                    _model.AvailableDevices.Items.Where(device => device.IsSameDevice(path) || device.IsSameDevice(serial) || device.IsSameDevice(PnPDevice.GetInstanceIdFromInterfaceId(path))));
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

        public string Name => _instanceId;

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

        _deviceNotificationListener.StopListen(DeviceInterfaceIds.UsbDevice);
    }
}
#endif
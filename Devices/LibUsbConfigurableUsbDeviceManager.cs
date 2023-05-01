using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Kernel;
using GuitarConfigurator.NetCore.ViewModels;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.DeviceNotify.Linux;
using LibUsbDotNet.Main;

namespace GuitarConfigurator.NetCore.Devices;


#if !Windows
public class ConfigurableUsbDeviceManager
{
    private readonly IDeviceNotifier _deviceListener = new LinuxDeviceNotifier();
    private MainWindowViewModel _model;

    public ConfigurableUsbDeviceManager(MainWindowViewModel model)
    {
        _model = model;
        
    }

    public void Register() {
            _deviceListener.OnDeviceNotify += OnDeviceNotify;
            List<UsbRegistry> deviceListAll = UsbDevice.AllDevices.AsList();
            foreach (var dev in deviceListAll)
            {
                OnDeviceNotify(null, new DeviceNotifyArgsRegistry(dev));
            }
    }

    public void Dispose()
    {
        _deviceListener.OnDeviceNotify -= OnDeviceNotify;
    }


    private void OnDeviceNotify(object? sender, DeviceNotifyEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (e.DeviceType != DeviceType.DeviceInterface) return;
            if (e.EventType == EventType.DeviceArrival)
            {
                var vid = e.Device.IdVendor;
                var pid = e.Device.IdProduct;
                if (vid == Dfu.DfuVid && (pid == Dfu.DfuPid16U2 || pid == Dfu.DfuPid8U2))
                {
                    _model.AvailableDevices.Add(new Dfu(e));
                }
                else if (e.Device.Open(out var dev))
                {
                    var info = dev.Info;
                    var revision = (ushort) info.Descriptor.BcdDevice;
                    var product = info.ProductString?.Split(new[] {'\0'}, 2)[0];
                    var serial = info.SerialString?.Split(new[] {'\0'}, 2)[0] ?? "";
                    switch (product)
                    {
                        case "Santroller":
                            _model.AvailableDevices.Add(new Santroller(_model.Pio, e.Device.Name, dev, product, serial,
                                revision));
                            break;
                        case "Ardwiino" when _model.Programming:
                        case "Ardwiino" when revision == Ardwiino.SerialArdwiinoRevision:
                            return;
                        case "Ardwiino":
                            _model.AvailableDevices.Add(new Ardwiino(_model.Pio, e.Device.Name, dev, product, serial,
                                revision));
                            break;
                        default:
                            dev.Close();
                            break;
                    }
                }
            }
            else
            {
                _model.AvailableDevices.RemoveMany(
                    _model.AvailableDevices.Items.Where(device => device.IsSameDevice(e.Device.Name)));
            }
        });
    }

    private class DeviceNotifyArgsRegistry : DeviceNotifyEventArgs
    {
        public DeviceNotifyArgsRegistry(UsbRegistry dev)
        {
            Device = new RegDeviceNotifyInfo(dev);
            DeviceType = DeviceType.DeviceInterface;
            EventType = EventType.DeviceArrival;
        }
    }
    private class RegDeviceNotifyInfo : IUsbDeviceNotifyInfo
    {
        private readonly UsbRegistry _dev;

        public RegDeviceNotifyInfo(UsbRegistry dev)
        {
            _dev = dev;
        }

        public UsbSymbolicName SymbolicName => UsbSymbolicName.Parse(_dev.SymbolicName);

        public string Name => _dev.DevicePath;

        public Guid ClassGuid => _dev.DeviceInterfaceGuids[0];

        public int IdVendor => _dev.Vid;

        public int IdProduct => _dev.Pid;

        public string SerialNumber => _dev.Device.Info.SerialString;

        public bool Open(out UsbDevice usbDevice)
        {
            usbDevice = _dev.Device;
            return usbDevice != null && usbDevice.Open();
        }
    }
}

#endif
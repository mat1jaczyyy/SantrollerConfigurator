using Avalonia.Media;
using Dahomey.Json.Attributes;
using GuitarConfiguratorSharp.NetCore.Configuration.Combined;
using GuitarConfiguratorSharp.NetCore.Configuration.Microcontrollers;
using GuitarConfiguratorSharp.NetCore.Configuration.Outputs;
using GuitarConfiguratorSharp.NetCore.ViewModels;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Json;

[JsonDiscriminator("wiic")]
public class JsonWiiCombinedOutput : JsonOutput
{
    public override JsonInput? Input => null;
    public override Color LedOn { get; }
    public override Color LedOff { get; }
    public int Sda { get; }
    public int Scl { get; }
    public bool MapTapBarToFrets { get; set; }
    public bool MapTapBarToAxis { get; set; }
    public bool MapGuitarJoystickToDPad { get; set; }
    public bool MapNunchukAccelerationToRightJoy { get; set; }

    public JsonWiiCombinedOutput(Color ledOn, Color ledOff, int sda, int scl, bool mapTapBarToFrets,
        bool mapTapBarToAxis, bool mapGuitarJoystickToDPad, bool mapNunchukAccelerationToRightJoy)
    {
        LedOn = ledOn;
        LedOff = ledOff;
        Sda = sda;
        Scl = scl;
        MapTapBarToFrets = mapTapBarToFrets;
        MapTapBarToAxis = mapTapBarToAxis;
        MapGuitarJoystickToDPad = mapGuitarJoystickToDPad;
        MapNunchukAccelerationToRightJoy = mapNunchukAccelerationToRightJoy;
    }

    public override Output Generate(ConfigViewModel model, Microcontroller microcontroller)
    {
        return new WiiCombinedOutput(model, microcontroller, Sda, Scl, MapTapBarToFrets, MapTapBarToAxis,
            MapGuitarJoystickToDPad, MapNunchukAccelerationToRightJoy);
    }
}
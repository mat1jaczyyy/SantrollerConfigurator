using Avalonia.Media;

namespace GuitarConfiguratorSharp.NetCore.Configuration.Leds;

public class Led
{
    public Color On;
    public Color Off;
    public int index;
    public bool AffectsAll;
    public int RumbleCommand;
    
    public string GenerateRumbleCheck()
    {
        return "";
    }
}
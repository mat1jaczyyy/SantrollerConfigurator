using System.Runtime.InteropServices;

// ReSharper disable UnusedMember.Global

namespace SantrollerConfiguratorBranded.NetCore;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record Uf2Block
{
    public uint magicStart0 = 0;
    public uint magicStart1 = 0;
    public uint flags = 0;
    public uint targetAddr = 0;
    public uint payloadSize = 0;
    public uint blockNo = 0;
    public uint numBlocks = 0;
    public uint fileSize = 0; // for the pi pico, this is a familyID
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 476)]
    public byte[] data = null!;
    public uint magicEnd = 0;
}
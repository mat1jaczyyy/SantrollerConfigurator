using System.Runtime.InteropServices;
using ProtoBuf;

namespace GuitarConfigurator.NetCore.Utils;

[ProtoContract]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record Uf2Block
{
    [ProtoMember(1)] public uint magicStart0 = 0;
    [ProtoMember(2)] public uint magicStart1 = 0;
    [ProtoMember(3)] public uint flags = 0;
    [ProtoMember(4)] public uint targetAddr = 0;
    [ProtoMember(5)] public uint payloadSize = 0;
    [ProtoMember(6)] public uint blockNo = 0;
    [ProtoMember(7)] public uint numBlocks = 0;
    [ProtoMember(8)] public uint fileSize = 0; // for the pi pico, this is a familyID

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 476)] [ProtoMember(9)]
    public byte[] data = null!;

    [ProtoMember(10)] public uint magicEnd = 0;
}
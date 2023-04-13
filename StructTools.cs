using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GuitarConfigurator.NetCore;

public static class StructTools
{
    /// <summary>
    ///     converts byte[] to struct
    /// </summary>
    public static T RawDeserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors),] T>(byte[] rawData, int position)
    {
        var rawsize = Marshal.SizeOf<T>();
        if (rawsize > rawData.Length - position)
            throw new ArgumentException("Not enough data to fill struct. Array length from position: " +
                                        (rawData.Length - position) + ", Struct length: " + rawsize);
        var buffer = Marshal.AllocHGlobal(rawsize);
        Marshal.Copy(rawData, position, buffer, rawsize);
        var retobj = (T) Marshal.PtrToStructure<T>(buffer)!;
        Marshal.FreeHGlobal(buffer);
        return retobj;
    }


    public static string RawDeserializeStr(byte[] buffer)
    {
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStringAnsi(handle.AddrOfPinnedObject())!;
        }
        finally
        {
            handle.Free();
        }
    }
}
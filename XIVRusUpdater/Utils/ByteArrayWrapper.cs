using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace XIVRusUpdater.Utils;

public unsafe class ByteArrayWrapper : IDisposable
{
    private bool _disposed;

    public unsafe byte* Pointer { get; }
    public int Length { get; }

    public ByteArrayWrapper(byte[] bytes)
    {
        Length = bytes.Length;

        Pointer = (byte*)Marshal.AllocHGlobal(Length);

        fixed (byte* src = bytes)
        {
            Buffer.MemoryCopy(src, Pointer, Length, Length);
        }
    }

    public Span<byte> AsSpan() => new(Pointer, Length);

    public void Dispose()
    {
        if (_disposed)
            return;

        Marshal.FreeHGlobal((nint)Pointer);
        _disposed = true;
    }
}

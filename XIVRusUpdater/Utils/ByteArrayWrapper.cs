using System;
using System.Runtime.InteropServices;
using System.Text;

namespace XIVRusUpdater.Utils;

public unsafe class ByteArrayWrapper : IDisposable
{
    private bool _disposed;

    public unsafe byte* Pointer { get; private set; }
    public int Length { get; }

    public string Value =>
        Encoding.UTF8.GetString(AsReadOnlySpan());

    public ByteArrayWrapper(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        Length = bytes.Length;

        if (Length == 0)
            return;

        Pointer = (byte*)Marshal.AllocHGlobal(Length + 1);

        fixed (byte* src = bytes)
        {
            Buffer.MemoryCopy(src, Pointer, Length, Length);
        }
    }

    public ReadOnlySpan<byte> AsReadOnlySpan()
    {
        ThrowIfDisposed();
        return new ReadOnlySpan<byte>(Pointer, Length);
    }

    public Span<byte> AsSpan()
    {
        ThrowIfDisposed();
        return new Span<byte>(Pointer, Length);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ByteArrayWrapper));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Marshal.FreeHGlobal((nint)Pointer);
        Pointer = null;
        _disposed = true;
    }
}

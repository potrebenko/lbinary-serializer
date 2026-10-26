using System.Buffers;
using System.Runtime.CompilerServices;

namespace LBinarySerializer;

internal class BufferPool
{
    private const int MINIMUM_SIZE = 1024; 
    private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        
    public static byte[] GetCachedBuffer(int length = MINIMUM_SIZE)
    {
        return _pool.Rent(length);
    }

    internal static void EnsureCapacity(ref byte[] buffer, int offset, int length)
    {
        var desireSize = (long)offset + length;
        if (desireSize > buffer.Length)
        {
            if (desireSize > Array.MaxLength)
            {
                throw new InvalidOperationException("The buffer is too big");
            }

            var newCapacity = CalculateNewCapacity(buffer.Length, length);

            var newBuffer = _pool.Rent(newCapacity);
            if (offset > 0)
            {
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, offset);
            }

            ReleaseToPool(ref buffer);
            buffer = newBuffer;
        }
    }

    internal static void ReleaseToPool(ref byte[] buffer)
    {
        var tmp = buffer;
        buffer = null!;
        if(tmp is not null) _pool.Return(tmp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateNewCapacity(int bufferLength, int length)
    {
        var newCapacity = Math.Max((long)bufferLength * 2, (long)bufferLength + length);
        return (int)Math.Min(newCapacity, Array.MaxLength);
    }
}

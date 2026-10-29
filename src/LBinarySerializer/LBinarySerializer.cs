using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace LBinarySerializer;

[SuppressMessage("ReSharper", "MethodOverloadWithOptionalParameter")]
public class LBinarySerializer : IDisposable
{
    private const int ByteSize = 1;
    private const int IntSize = sizeof(int);
    private const int ShortSize = sizeof(short);
    private const int LongSize = sizeof(long);
    private const int FloatSize = sizeof(float);
    private const int DoubleSize = sizeof(double);
    private const int DecimalSize = sizeof(decimal);

    private byte[] _internalBuffer;
    private int _offset;
    private int _level;
    private bool _disposed;

    public LBinarySerializer() : this(1024)
    {
    }

    public LBinarySerializer(int bufferSize)
    {
        _internalBuffer = BufferPool.GetCachedBuffer(bufferSize);
        _offset = 0;
    }

    public void Reset()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LBinarySerializer), "The object has been disposed");
        }

        _offset = 0;
        _level = 0;
    }

    #region Premitives

    public void Write(bool value)
    {
        WriteOneByte(value ? (byte)1 : (byte)0);
    }

    public void Write(byte value)
    {
        WriteOneByte(value);
    }

    public void Write(byte[] value)
    {
        WriteToStream(value);
    }

    public void Write(Span<byte> value)
    {
        WriteToStream(value);
    }

    public void Write(ReadOnlySpan<byte> value)
    {
        WriteToStream(value);
    }

    public void Write(int value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, IntSize);
        BinaryPrimitives.WriteInt32LittleEndian(_internalBuffer.AsSpan(_offset, IntSize), value);
        _offset += IntSize;
    }

    private void Write(int value, int position)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, position, IntSize);
        BinaryPrimitives.WriteInt32LittleEndian(_internalBuffer.AsSpan(position, IntSize), value);
    }

    public void Write(uint value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, IntSize);
        BinaryPrimitives.WriteUInt32LittleEndian(_internalBuffer.AsSpan(_offset, IntSize), value);
        _offset += IntSize;
    }

    public void Write(short value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, ShortSize);
        BinaryPrimitives.WriteInt16LittleEndian(_internalBuffer.AsSpan(_offset, ShortSize), value);
        _offset += ShortSize;
    }

    public void Write(ushort value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, ShortSize);
        BinaryPrimitives.WriteUInt16LittleEndian(_internalBuffer.AsSpan(_offset, ShortSize), value);
        _offset += ShortSize;
    }

    public void Write(long value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, LongSize);
        BinaryPrimitives.WriteInt64LittleEndian(_internalBuffer.AsSpan(_offset, LongSize), value);
        _offset += LongSize;
    }

    public void Write(ulong value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, LongSize);
        BinaryPrimitives.WriteUInt64LittleEndian(_internalBuffer.AsSpan(_offset, LongSize), value);
        _offset += LongSize;
    }

    public void Write(float value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, FloatSize);
        BinaryPrimitives.WriteSingleLittleEndian(_internalBuffer.AsSpan(_offset, FloatSize), value);
        _offset += FloatSize;
    }

    public void Write(double value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, DoubleSize);
        BinaryPrimitives.WriteDoubleLittleEndian(_internalBuffer.AsSpan(_offset, DoubleSize), value);
        _offset += DoubleSize;
    }

    public void Write(decimal value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, DecimalSize);
        decimal.GetBits(value, MemoryMarshal.Cast<byte, int>(_internalBuffer.AsSpan(_offset, DecimalSize)));
        _offset += DecimalSize;
    }

    public void Write(Half value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, ShortSize);
        BitConverter.TryWriteBytes(_internalBuffer.AsSpan(_offset, ShortSize), value);
        _offset += ShortSize;
    }

    public void Write(Guid value)
    {
        const int guidSize = 16;
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, guidSize);
        value.TryWriteBytes(_internalBuffer.AsSpan(_offset, guidSize));
        _offset += guidSize;
    }

    public void Write(DateTime value)
    {
        WriteEnum(value.Kind);
        Write(value.Ticks);
    }

    public void Write(DateTimeOffset value)
    {
        Write(value.Ticks);
        Write(value.Offset);
    }

    public void Write(TimeSpan value)
    {
        Write(value.Ticks);
    }

    public void Write(char value)
    {
        Write((short)value);
    }

    public void WriteEnum<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        if (Unsafe.SizeOf<TEnum>() > IntSize)
        {
            throw new NotSupportedException("Enums with 64-bit underlying types are not supported by the four-byte format.");
        }

        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, IntSize);
        var span = _internalBuffer.AsSpan(_offset, IntSize);
        span.Clear(); // Narrow enums must not expose bytes left by a previous buffer user.
        MemoryMarshal.Write(span, in value);
        _offset += IntSize;
    }

    #endregion

    public void Write(string? value, EncodingType encoding = EncodingType.UTF8)
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value == string.Empty)
        {
            Write(0);
            return;
        }

        var textEncoding = encoding switch
        {
            EncodingType.UTF8 => Encoding.UTF8,
            EncodingType.ASCII => Encoding.ASCII,
            EncodingType.UTF16 => Encoding.Unicode,
            EncodingType.UTF32 => Encoding.UTF32,
            _ => throw new NotImplementedException()
        };

        // Four bytes per UTF-16 code unit plus fallback space bounds all supported encodings.
        if (4L * (value.Length + 1L) <= _internalBuffer.Length - _offset - IntSize)
        {
            var bytesWritten = textEncoding.GetBytes(value, _internalBuffer.AsSpan(_offset + IntSize));
            BinaryPrimitives.WriteInt32LittleEndian(_internalBuffer.AsSpan(_offset, IntSize), bytesWritten);
            _offset += IntSize + bytesWritten;
            return;
        }

        var stringSize = textEncoding.GetByteCount(value);
        Write(stringSize);
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, stringSize);
        textEncoding.GetBytes(value, _internalBuffer.AsSpan(_offset, stringSize));
        _offset += stringSize;
    }

    public LBinarySerializer Write<T>(T? value) where T : class, ILBinarySerializable
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return this;
        }

        if (_level + 1 >= SerializerSettings.MaxDepthSize)
        {
            throw new InvalidOperationException("The maximum object nesting depth was exceeded.");
        }

        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, IntSize);
        var initialObjectOffset = _offset; // Save object position
        _offset += IntSize; // Reserve space for object size
        _level++; // Increase the level of depth

        value.Serialize(this); // Serialize object

        var objSize = _offset - initialObjectOffset - IntSize;
        _level--; // Decrease the level of depth

        Write(objSize, initialObjectOffset); // Write object size
        return this;
    }

    /// <summary>
    /// Used only for unmanaged types.
    /// The structure can't contain DateTime, string and other managed types.
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void WriteStructure<T>(T value) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        Write(size);
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, size);
        Unsafe.WriteUnaligned(ref _internalBuffer[_offset], value);
        _offset += size;
    }

    #region Arrays

    public void Write<T>(T?[]? value) where T : class, ILBinarySerializable
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Length == 0)
        {
            Write(0);
            return;
        }

        Write(value.Length);
        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write<T>(T[]? value, bool unused = false) where T : unmanaged
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Length == 0)
        {
            Write(0);
            return;
        }

        Write(value.Length);

        ReadOnlySpan<T> span = value;
        Write(MemoryMarshal.Cast<T, byte>(span));
    }

    public void Write(string[]? value, EncodingType encoding = EncodingType.UTF8)
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Length == 0)
        {
            Write(0);
            return;
        }

        Write(value.Length);

        foreach (var item in value)
        {
            Write(item, encoding);
        }
    }

    #endregion

    #region Lists

    public void Write<T>(IList<T>? value) where T : class, ILBinarySerializable
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Count == 0)
        {
            Write(0);
            return;
        }

        Write(value.Count);

        foreach (var item in value)
        {
            Write(item);
        }
    }

    public void Write<T>(IList<T>? value, bool unused = false) where T : unmanaged
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Count == 0)
        {
            Write(0);
            return;
        }

        Write(value.Count);

        ReadOnlySpan<T> span = ListToReadOnlySpan(value);
        Write(MemoryMarshal.Cast<T, byte>(span));
    }

    public void Write(IList<string>? value, EncodingType encoding = EncodingType.UTF8)
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Count == 0)
        {
            Write(0);
            return;
        }

        Write(value.Count);

        foreach (var item in value)
        {
            Write(item, encoding);
        }
    }

    #endregion

    #region Dictionaries

    public void Write<T, TV>(IDictionary<T, TV>? value)
        where T : unmanaged
        where TV : class, ILBinarySerializable
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Count == 0)
        {
            Write(0);
            return;
        }

        Write(value.Count);
        
        if (value.GetType() == typeof(Dictionary<T, TV>))
        {
            foreach (var item in (Dictionary<T, TV>)value)
            {
                WriteStructure(item.Key);
                Write(item.Value);
            }
            return;
        }

        foreach (var item in value)
        {
            WriteStructure(item.Key);
            Write(item.Value);
        }
    }

    public void Write<T>(IDictionary<string, T>? value) where T : class, ILBinarySerializable
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Count == 0)
        {
            Write(0);
            return;
        }

        Write(value.Count);

        if (value.GetType() == typeof(Dictionary<string, T>))
        {
            foreach (var item in (Dictionary<string, T>)value)
            {
                Write(item.Key);
                Write(item.Value);
            }
            return;
        }

        foreach (var item in value)
        {
            Write(item.Key);
            Write(item.Value);
        }
    }

    public void Write<T>(IDictionary<string, T>? value, bool unused = false) where T : unmanaged
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Count == 0)
        {
            Write(0);
            return;
        }

        Write(value.Count);

        if (value.GetType() == typeof(Dictionary<string, T>))
        {
            foreach (var item in (Dictionary<string, T>)value)
            {
                Write(item.Key);
                WriteStructure(item.Value);
            }
            return;
        }

        foreach (var item in value)
        {
            Write(item.Key);
            WriteStructure(item.Value);
        }
    }

    public void Write<T>(Dictionary<T, string>? value, EncodingType encodingType = EncodingType.UTF8)
        where T : unmanaged
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return;
        }

        if (value.Count == 0)
        {
            Write(0);
            return;
        }

        Write(value.Count);

        foreach (var item in value)
        {
            WriteStructure(item.Key);
            Write(item.Value, encodingType);
        }
    }

    #endregion

    public byte[] ToArray()
    {
        if (_offset == 0)
        {
            return [];
        }

        var destination = GC.AllocateUninitializedArray<byte>(_offset);
        Array.Copy(_internalBuffer, 0, destination, 0, _offset);
        return destination;
    }

    /// <summary>
    /// Returns a borrowed view of the serializer's pooled buffer without copying.
    /// </summary>
    /// <remarks>
    /// Keep this serializer alive and do not write, reset, or dispose it while using the view.
    /// Use <see cref="ToArray"/> for a copy that can outlive or be used independently of the serializer.
    /// </remarks>
    public ReadOnlyMemory<byte> ToMemory()
    {
        return _internalBuffer.AsMemory(0, _offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ReadOnlySpan<T> ListToReadOnlySpan<T>(IList<T> list)
    {
        if (list is List<T> concreteList)
        {
            return CollectionsMarshal.AsSpan(concreteList);
        }
        else if (list is T[] array)
        {
            return array;
        }
        else
        {
            return list.ToArray();
        }
    }

    private void WriteOneByte(byte value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, ByteSize);
        _internalBuffer[_offset] = value;
        _offset++;
    }

    private void WriteToStream(ReadOnlySpan<byte> buffer)
    {
        var bufferLength = buffer.Length;
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, bufferLength);
        buffer.CopyTo(_internalBuffer.AsSpan(_offset, bufferLength));
        _offset += bufferLength;
    }

    private void WriteToStream(byte[] buffer)
    {
        var length = buffer.Length;
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, length);
        if (length <= 8)
        {
            var byteCount = length;
            while (--byteCount >= 0)
            {
                _internalBuffer[_offset + byteCount] = buffer[byteCount];
            }
        }
        else
        {
            Buffer.BlockCopy(buffer, 0, _internalBuffer, _offset, length);
        }

        _offset += length;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            BufferPool.ReleaseToPool(ref _internalBuffer);
            GC.SuppressFinalize(this);
        }
    }

    ~LBinarySerializer()
    {
        Dispose();
    }
}

using System.Buffers;
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

    private readonly int[] _objectDepthSizes = new int[SerializerSettings.MaxDepthSize];
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
        Span<byte> buffer = stackalloc byte[IntSize];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        WriteToStream(buffer);
    }

    private void Write(int value, int position)
    {
        Span<byte> buffer = stackalloc byte[IntSize];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        WriteToStream(buffer, position);
    }

    public void Write(uint value)
    {
        Span<byte> buffer = stackalloc byte[IntSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        WriteToStream(buffer);
    }

    public void Write(short value)
    {
        Span<byte> buffer = stackalloc byte[ShortSize];
        BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        WriteToStream(buffer);
    }

    public void Write(ushort value)
    {
        Span<byte> buffer = stackalloc byte[ShortSize];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        WriteToStream(buffer);
    }

    public void Write(long value)
    {
        Span<byte> buffer = stackalloc byte[LongSize];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        WriteToStream(buffer);
    }

    public void Write(ulong value)
    {
        Span<byte> buffer = stackalloc byte[LongSize];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        WriteToStream(buffer);
    }

    public void Write(float value)
    {
        Span<byte> buffer = stackalloc byte[FloatSize];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        WriteToStream(buffer);
    }

    public void Write(double value)
    {
        Span<byte> buffer = stackalloc byte[DoubleSize];
        BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
        WriteToStream(buffer);
    }

    public void Write(decimal value)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        Span<byte> buffer = stackalloc byte[DecimalSize];
        MemoryMarshal.Cast<int, byte>(bits).CopyTo(buffer);
        WriteToStream(buffer);
    }

    public void Write(Half value)
    {
        Span<byte> buffer = stackalloc byte[ShortSize];
        BitConverter.TryWriteBytes(buffer, value);
        WriteToStream(buffer);
    }

    public void Write(Guid value)
    {
        const int guidSize = 16;
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, guidSize);
        value.TryWriteBytes(_internalBuffer.AsSpan(_offset, guidSize));
        _offset += guidSize;
        _objectDepthSizes[_level] += guidSize;
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
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, IntSize);
        MemoryMarshal.Write(_internalBuffer.AsSpan(_offset, IntSize), in value);
        _offset += IntSize;
        _objectDepthSizes[_level] += IntSize;
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

        int stringSize;
        switch (encoding)
        {
            case EncodingType.UTF8:
                stringSize = Encoding.UTF8.GetByteCount(value);
                Write(stringSize);
                BufferPool.EnsureCapacity(ref _internalBuffer, _offset, stringSize);
                Encoding.UTF8.GetBytes(value, _internalBuffer.AsSpan(_offset, stringSize));
                _offset += stringSize;
                _objectDepthSizes[_level] += stringSize;
                break;
            case EncodingType.ASCII:
                stringSize = value.Length;
                Write(stringSize);
                Encoding.ASCII.GetBytes(value, 0, stringSize, _internalBuffer, _offset);
                _offset += stringSize;
                _objectDepthSizes[_level] += stringSize;
                break;
            case EncodingType.UTF16:
                stringSize = Encoding.Unicode.GetByteCount(value);
                Write(stringSize);
                unsafe
                {
                    fixed (byte* ptr = &_internalBuffer[_offset])
                    {
                        fixed (char* str = value)
                        {
                            Buffer.MemoryCopy(str, ptr, stringSize, stringSize);
                        }
                    }
                }

                _offset += stringSize;
                _objectDepthSizes[_level] += stringSize;
                break;
            case EncodingType.UTF32:
                stringSize = Encoding.UTF32.GetByteCount(value);
                Write(stringSize);
                BufferPool.EnsureCapacity(ref _internalBuffer, _offset, stringSize);
                Encoding.UTF32.GetBytes(value, _internalBuffer.AsSpan(_offset, stringSize));
                _offset += stringSize;
                _objectDepthSizes[_level] += stringSize;
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public LBinarySerializer Write<T>(T? value) where T : class, ILBinarySerializable
    {
        if (value == null)
        {
            Write(ObjectValue.Null);
            return this;
        }

        var initialObjectOffset = _offset; // Save object position
        _offset += IntSize; // Reserve space for object size
        _level++; // Increase the level of depth
        _objectDepthSizes[_level] = 0; // Reset object size

        value.Serialize(this); // Serialize object

        var objSize = _objectDepthSizes[_level]; // Get object size without a header
        _level--; // Decrease the level of depth

        if (_level > 0)
        {
            _objectDepthSizes[_level] += objSize + IntSize; // Add object size to parent
        }

        WriteHeader(objSize, initialObjectOffset); // Write object size
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
        _objectDepthSizes[_level] += size;
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

    private void WriteHeader(int value, int position)
    {
        Write(value, position);
    }

    private void WriteOneByte(byte value)
    {
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, ByteSize);
        _internalBuffer[_offset] = value;
        _offset++;
        _objectDepthSizes[_level]++;
    }

    private void WriteToStream(ReadOnlySpan<byte> buffer, int offset, int size = -1, bool updateOffset = false)
    {
        var length = size > 0 ? size : buffer.Length;
        BufferPool.EnsureCapacity(ref _internalBuffer, offset, length);
        buffer.Slice(0, length).CopyTo(_internalBuffer.AsSpan(offset, length));
        if (updateOffset)
        {
            _offset += length;
            _objectDepthSizes[_level] += length;
        }
    }

    private void WriteToStream(ReadOnlySpan<byte> buffer)
    {
        var bufferLength = buffer.Length;
        BufferPool.EnsureCapacity(ref _internalBuffer, _offset, bufferLength);
        buffer.CopyTo(_internalBuffer.AsSpan(_offset, bufferLength));
        _offset += bufferLength;
        _objectDepthSizes[_level] += bufferLength;
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
        _objectDepthSizes[_level] += length;
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
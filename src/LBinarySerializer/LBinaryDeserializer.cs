using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace LBinarySerializer;

[SuppressMessage("ReSharper", "MethodOverloadWithOptionalParameter")]
public class LBinaryDeserializer
{
    private const int IntSize = sizeof(int);
    private const int ShortSize = sizeof(short);
    private const int LongSize = sizeof(long);
    private const int FloatSize = sizeof(float);
    private const int DoubleSize = sizeof(double);
    private const int DecimalSize = sizeof(decimal);
    private const int GuidSize = 16;
    private const int RootLevel = 0;

    private readonly int[] _decimalBuffer = new int[4];
    private readonly int[] _objectDepthSizes = new int[SerializerSettings.MaxDepthSize];
    private int _level = RootLevel;
    private byte[] _serializedData;
    private int _offset;
    private int _scope;

    private StringComparer _stringComparer = StringComparer.Ordinal;

    public LBinaryDeserializer()
    {
        _serializedData = [];
    }

    public LBinaryDeserializer(byte[] serializedData)
    {
        _serializedData = serializedData;
    }

    public T? Deserialize<T>(byte[] serializedData) where T : class, ILBinarySerializable, new()
    {
        _serializedData = serializedData;
        Reset();
        return Deserialize<T>();
    }

    public T? Deserialize<T>() where T : class, ILBinarySerializable, new()
    {
        var value = new T();
        var objectSize = 0;
        if (_scope > 0)
        {
            objectSize = ReadInt();

            if (objectSize == ObjectValue.Null)
            {
                return null;
            }

            _level++;
            _objectDepthSizes[_level] = objectSize;
        }

        _scope++;
        value.Deserialize<T>(this);
        _scope--;

        if (_level > RootLevel)
        {
            _offset += _objectDepthSizes[_level]; // Add remaining bytes for backward compatibility
            _objectDepthSizes[_level] = 0;
            _level--;
            _objectDepthSizes[_level] -= objectSize;
        }

        return value;
    }

    public string? ReadString(EncodingType encoding = EncodingType.UTF8)
    {
        var length = ReadInt();

        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return string.Empty;
        }

        var result = encoding switch
        {
            EncodingType.UTF8 => ReadUnmanagedString(_serializedData, _offset, length, Encoding.UTF8),
            EncodingType.ASCII => ReadUnmanagedString(_serializedData, _offset, length, Encoding.ASCII),
            EncodingType.UTF16 => ReadUnmanagedString(_serializedData, _offset, length, Encoding.Unicode),
            EncodingType.UTF32 => ReadUnmanagedString(_serializedData, _offset, length, Encoding.UTF32),
            _ => throw new NotImplementedException()
        };

        _offset += length;
        _objectDepthSizes[_level] -= length;
        return result;
    }

    public T ReadStruct<T>() where T : struct
    {
        var length = ReadInt();

        if (length == 0 || !CanReadBytesForCurrentLevel(length))
        {
            return default;
        }

        var value = Unsafe.ReadUnaligned<T>(ref _serializedData[_offset]);
        _offset += length;
        _objectDepthSizes[_level] -= length;
        return value;
    }

    #region Premitives

    public bool ReadBool()
    {
        if (!CanReadBytesForCurrentLevel(1))
        {
            return false; // Return default value
        }

        var value = _serializedData[_offset] != 0;
        _offset++;
        _objectDepthSizes[_level] -= 1;
        return value;
    }

    public byte ReadByte()
    {
        if (!CanReadBytesForCurrentLevel(1))
        {
            return 0; // Return default value
        }

        var value = _serializedData[_offset];
        _offset++;
        _objectDepthSizes[_level] -= 1;
        return value;
    }

    public int ReadInt()
    {
        if (!CanReadBytesForCurrentLevel(IntSize))
        {
            return 0; // Return default value
        }

        var value = BinaryPrimitives.ReadInt32LittleEndian(_serializedData.AsSpan(_offset, IntSize));
        _offset += IntSize;
        _objectDepthSizes[_level] -= IntSize;
        return value;
    }

    public uint ReadUInt()
    {
        if (!CanReadBytesForCurrentLevel(IntSize))
        {
            return 0; // Return default value
        }

        var value = BinaryPrimitives.ReadUInt32LittleEndian(_serializedData.AsSpan(_offset, IntSize));
        _offset += IntSize;
        _objectDepthSizes[_level] -= IntSize;
        return value;
    }

    public short ReadShort()
    {
        if (!CanReadBytesForCurrentLevel(ShortSize))
        {
            return 0; // Return default value
        }

        var value = BinaryPrimitives.ReadInt16LittleEndian(_serializedData.AsSpan(_offset, ShortSize));
        _offset += ShortSize;
        _objectDepthSizes[_level] -= ShortSize;
        return value;
    }

    public ushort ReadUShort()
    {
        if (!CanReadBytesForCurrentLevel(ShortSize))
        {
            return 0; // Return default value
        }

        var value = BinaryPrimitives.ReadUInt16LittleEndian(_serializedData.AsSpan(_offset, ShortSize));
        _offset += ShortSize;
        _objectDepthSizes[_level] -= ShortSize;
        return value;
    }

    public long ReadLong()
    {
        if (!CanReadBytesForCurrentLevel(LongSize))
        {
            return 0; // Return default value
        }

        var value = BinaryPrimitives.ReadInt64LittleEndian(_serializedData.AsSpan(_offset, LongSize));
        _offset += LongSize;
        _objectDepthSizes[_level] -= LongSize;
        return value;
    }

    public ulong ReadULong()
    {
        if (!CanReadBytesForCurrentLevel(LongSize))
        {
            return 0; // Return default value
        }

        var value = BinaryPrimitives.ReadUInt64LittleEndian(_serializedData.AsSpan(_offset, LongSize));
        _offset += LongSize;
        _objectDepthSizes[_level] -= LongSize;
        return value;
    }

    public float ReadFloat()
    {
        if (!CanReadBytesForCurrentLevel(FloatSize))
        {
            return 0; // Return default value
        }

        var value = BinaryPrimitives.ReadSingleLittleEndian(_serializedData.AsSpan(_offset, FloatSize));
        _offset += FloatSize;
        _objectDepthSizes[_level] -= FloatSize;
        return value;
    }

    public double ReadDouble()
    {
        if (!CanReadBytesForCurrentLevel(DoubleSize))
        {
            return 0; // Return default value
        }

        var value = BinaryPrimitives.ReadDoubleLittleEndian(_serializedData.AsSpan(_offset, DoubleSize));
        _offset += DoubleSize;
        _objectDepthSizes[_level] -= DoubleSize;
        return value;
    }

    public decimal ReadDecimal()
    {
        if (!CanReadBytesForCurrentLevel(DecimalSize))
        {
            return 0m; // Return default value
        }

        var span = _serializedData.AsSpan(_offset, DecimalSize);
        var ints = MemoryMarshal.Cast<byte, int>(span);
        _decimalBuffer[0] = ints[0];
        _decimalBuffer[1] = ints[1];
        _decimalBuffer[2] = ints[2];
        _decimalBuffer[3] = ints[3];

        _offset += DecimalSize;
        _objectDepthSizes[_level] -= DecimalSize;
        return new decimal(_decimalBuffer);
    }

    public Half ReadHalf()
    {
        if (!CanReadBytesForCurrentLevel(ShortSize))
        {
            return Half.Zero; // Return default value
        }

        var value = BinaryPrimitives.ReadHalfLittleEndian(_serializedData.AsSpan(_offset, ShortSize));
        _offset += ShortSize;
        _objectDepthSizes[_level] -= ShortSize;
        return value;
    }

    public Guid ReadGuid()
    {
        if (!CanReadBytesForCurrentLevel(GuidSize))
        {
            return Guid.Empty; // Return default value
        }

        var value = new Guid(_serializedData.AsSpan(_offset, GuidSize));
        _offset += GuidSize;
        _objectDepthSizes[_level] -= GuidSize;
        return value;
    }

    public DateTime ReadDateTime()
    {
        if (!CanReadBytesForCurrentLevel(IntSize + LongSize))
        {
            return DateTime.MinValue; // Return default value
        }

        var dateTimeKind = ReadInt();
        var ticks = ReadLong();
        return new DateTime(ticks, (DateTimeKind)dateTimeKind);
    }

    public DateTimeOffset ReadDateTimeOffset()
    {
        if (!CanReadBytesForCurrentLevel(LongSize + LongSize))
        {
            return DateTimeOffset.MinValue;
        }

        var utcTicks = ReadLong();
        var offset = ReadTimeSpan();
        return new DateTimeOffset(utcTicks, offset);
    }

    public TimeSpan ReadTimeSpan()
    {
        if (!CanReadBytesForCurrentLevel(LongSize))
        {
            return TimeSpan.Zero; // Return default value
        }

        var ticks = ReadLong();
        return new TimeSpan(ticks);
    }

    public char ReadChar()
    {
        if (!CanReadBytesForCurrentLevel(ShortSize))
        {
            return char.MinValue; // Return default value
        }

        return (char)ReadShort();
    }

    public TEnum ReadEnum<TEnum>() where TEnum : unmanaged, Enum
    {
        if (!CanReadBytesForCurrentLevel(IntSize))
        {
            return default; // Return default value
        }

        var span = _serializedData.AsSpan(_offset, IntSize);
        _offset += IntSize;
        _objectDepthSizes[_level] -= IntSize;
        return Unsafe.ReadUnaligned<TEnum>(ref MemoryMarshal.GetReference(span));
    }

    #endregion

    #region Arrays

    public T?[]? ReadArrayOf<T>() where T : class, ILBinarySerializable, new()
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return [];
        }

        var array = new T?[length];
        for (var i = 0; i < length; i++)
        {
            _scope++;
            array[i] = Deserialize<T>();
            _scope--;
        }

        return array;
    }

    public T[]? ReadArrayOf<T>(bool unused = false) where T : unmanaged
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return [];
        }

        var dataSize = Unsafe.SizeOf<T>() * length;
        var span = new ReadOnlySpan<byte>(_serializedData, _offset, dataSize);
        _offset += dataSize;
        _objectDepthSizes[_level] -= dataSize;

        var value = MemoryMarshal.Cast<byte, T>(span);
        return value.ToArray();
    }

    public string?[]? ReadArrayOfStrings(EncodingType encodingType = EncodingType.UTF8)
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return [];
        }

        var array = new string?[length];
        for (var i = 0; i < length; i++)
        {
            array[i] = ReadString(encodingType);
        }

        return array;
    }

    #endregion

    #region Lists

    public List<T?>? ReadListOf<T>() where T : class, ILBinarySerializable, new()
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return new List<T?>();
        }

        var list = new List<T?>(length);
        for (var i = 0; i < length; i++)
        {
            _scope++;
            list.Add(Deserialize<T>());
            _scope--;
        }

        return list;
    }

    public List<T?>? ReadListOf<T>(bool unused = false) where T : unmanaged
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return new List<T?>();
        }

        var list = new List<T?>(length);

        var dataSize = Unsafe.SizeOf<T>() * length;
        var items = MemoryMarshal.Cast<byte, T>(_serializedData.AsSpan(_offset, dataSize));
        for (var i = 0; i < length; i++)
        {
            list.Add(items[i]);
        }

        _offset += dataSize;
        _objectDepthSizes[_level] -= dataSize;

        return list;
    }

    public List<string?>? ReadListOfStrings(EncodingType encodingType = EncodingType.UTF8)
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return new List<string?>();
        }

        var list = new List<string?>(length);
        for (var i = 0; i < length; i++)
        {
            list.Add(ReadString(encodingType));
        }

        return list;
    }

    #endregion

    #region Dictionaries

    public Dictionary<T, TV?>? ReadDictionaryOf<T, TV>()
        where T : unmanaged
        where TV : class, ILBinarySerializable, new()
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return new Dictionary<T, TV?>();
        }

        var dictionary = new Dictionary<T, TV?>(length);
        for (var i = 0; i < length; i++)
        {
            var key = ReadStruct<T>();
            _scope++;
            var value = Deserialize<TV>();
            _scope--;
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    public Dictionary<string, T?>? ReadDictionaryOf<T>()
        where T : class, ILBinarySerializable, new()
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return new Dictionary<string, T?>(StringComparer.Ordinal);
        }

        var dictionary = new Dictionary<string, T?>(length, _stringComparer);
        for (var i = 0; i < length; i++)
        {
            var key = ReadString(EncodingType.ASCII)!;
            _scope++;
            var value = Deserialize<T>();
            _scope--;
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    public Dictionary<string, T>? ReadDictionaryOfStructs<T>()
        where T : unmanaged
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return new Dictionary<string, T>();
        }

        var dictionary = new Dictionary<string, T>(length, _stringComparer);
        for (var i = 0; i < length; i++)
        {
            var key = ReadString(EncodingType.ASCII)!;
            var value = ReadStruct<T>();
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    public Dictionary<T, string?>? ReadDictionaryOfStrings<T>(EncodingType encodingType = EncodingType.UTF8)
        where T : unmanaged
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return new Dictionary<T, string?>();
        }

        var dictionary = new Dictionary<T, string?>(length);
        for (var i = 0; i < length; i++)
        {
            var key = ReadStruct<T>();
            var value = ReadString(encodingType);
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string ReadUnmanagedString(byte[] data, int offset, int length, Encoding encoding)
    {
        fixed (byte* ptr = data)
        {
            return new string((sbyte*)ptr, offset, length, encoding);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanReadBytesForCurrentLevel(int size)
    {
        if (_level == RootLevel && _offset + size <= _serializedData.Length)
        {
            return true;
        }

        if (_level > RootLevel && _objectDepthSizes[_level] >= size)
        {
            return true;
        }

        return false;
    }

    private void Reset()
    {
        _level = RootLevel;
        _scope = 0;
        _offset = 0;
        Array.Clear(_objectDepthSizes, 0, _objectDepthSizes.Length);
    }
}
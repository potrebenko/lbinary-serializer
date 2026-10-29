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
        var objectSize = 0;
        if (_scope > 0)
        {
            if (RemainingBytes == 0)
            {
                return null;
            }

            objectSize = ReadInt();

            if (objectSize == ObjectValue.Null)
            {
                return null;
            }

            EnsureAvailable(objectSize);
            if (_level + 1 >= _objectDepthSizes.Length)
            {
                throw new InvalidDataException("The maximum object nesting depth was exceeded.");
            }

            _level++;
            _objectDepthSizes[_level] = objectSize;
        }

        var value = new T();
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

        EnsureAvailable(length);
        var span = _serializedData.AsSpan(_offset, length);
        var result = encoding switch
        {
            EncodingType.UTF8 => Encoding.UTF8.GetString(span),
            EncodingType.ASCII => Encoding.ASCII.GetString(span),
            EncodingType.UTF16 => Encoding.Unicode.GetString(span),
            EncodingType.UTF32 => Encoding.UTF32.GetString(span),
            _ => throw new NotImplementedException()
        };

        _offset += length;
        _objectDepthSizes[_level] -= length;
        return result;
    }

    public T ReadStruct<T>() where T : unmanaged
    {
        var length = ReadInt();

        if (length == 0)
        {
            return default;
        }

        if (length != Unsafe.SizeOf<T>())
        {
            throw new InvalidDataException("The serialized structure size does not match the requested type.");
        }

        EnsureAvailable(length);
        var value = MemoryMarshal.Read<T>(_serializedData.AsSpan(_offset, length));
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

        _offset += DecimalSize;
        _objectDepthSizes[_level] -= DecimalSize;
        return new decimal(ints);
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
        if (Unsafe.SizeOf<TEnum>() > IntSize)
        {
            throw new NotSupportedException("Enums with 64-bit underlying types are not supported by the four-byte format.");
        }

        if (!CanReadBytesForCurrentLevel(IntSize))
        {
            return default; // Return default value
        }

        var span = _serializedData.AsSpan(_offset, IntSize);
        _offset += IntSize;
        _objectDepthSizes[_level] -= IntSize;
        return MemoryMarshal.Read<TEnum>(span);
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

        ValidateCollectionSize(length, IntSize);
        var array = new T?[length];
        for (var i = 0; i < length; i++)
        {
            EnsureAvailable(IntSize);
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

        var dataSize = ValidateCollectionSize(length, Unsafe.SizeOf<T>());
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

        ValidateCollectionSize(length, IntSize);
        var array = new string?[length];
        for (var i = 0; i < length; i++)
        {
            EnsureAvailable(IntSize);
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

        ValidateCollectionSize(length, IntSize);
        var list = new List<T?>(length);
        for (var i = 0; i < length; i++)
        {
            EnsureAvailable(IntSize);
            _scope++;
            list.Add(Deserialize<T>());
            _scope--;
        }

        return list;
    }

    public List<T>? ReadListOf<T>(bool unused = false) where T : unmanaged
    {
        var length = ReadInt();
        switch (length)
        {
            case ObjectValue.Null:
                return null;
            case 0:
                return new List<T>();
        }

        var dataSize = ValidateCollectionSize(length, Unsafe.SizeOf<T>());
        var list = new List<T>(length);
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

        ValidateCollectionSize(length, IntSize);
        var list = new List<string?>(length);
        for (var i = 0; i < length; i++)
        {
            EnsureAvailable(IntSize);
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

        ValidateCollectionSize(length, 2 * IntSize);
        var dictionary = new Dictionary<T, TV?>(length);
        for (var i = 0; i < length; i++)
        {
            EnsureAvailable(IntSize);
            var key = ReadStruct<T>();
            EnsureAvailable(IntSize);
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

        ValidateCollectionSize(length, 2 * IntSize);
        var dictionary = new Dictionary<string, T?>(length, _stringComparer);
        for (var i = 0; i < length; i++)
        {
            EnsureAvailable(IntSize);
            var key = ReadString()!;
            EnsureAvailable(IntSize);
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

        ValidateCollectionSize(length, 2 * IntSize);
        var dictionary = new Dictionary<string, T>(length, _stringComparer);
        for (var i = 0; i < length; i++)
        {
            EnsureAvailable(IntSize);
            var key = ReadString()!;
            EnsureAvailable(IntSize);
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

        ValidateCollectionSize(length, 2 * IntSize);
        var dictionary = new Dictionary<T, string?>(length);
        for (var i = 0; i < length; i++)
        {
            EnsureAvailable(IntSize);
            var key = ReadStruct<T>();
            EnsureAvailable(IntSize);
            var value = ReadString(encodingType);
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    #endregion

    private int RemainingBytes => _level == RootLevel
        ? _serializedData.Length - _offset
        : Math.Min(_serializedData.Length - _offset, _objectDepthSizes[_level]);

    private int ValidateCollectionSize(int count, int minimumItemSize)
    {
        var size = (long)count * minimumItemSize;
        if (count < 0 || size > RemainingBytes)
        {
            throw new InvalidDataException("The collection count exceeds the available data.");
        }

        return (int)size;
    }

    private  void EnsureAvailable(int size)
    {
        if (size < 0 || size > RemainingBytes)
        {
            throw new InvalidDataException("The payload length exceeds the available data.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanReadBytesForCurrentLevel(int size)
    {
        if (RemainingBytes == 0)
        {
            return false;
        }

        EnsureAvailable(size);
        return true;
    }

    private void Reset()
    {
        _level = RootLevel;
        _scope = 0;
        _offset = 0;
        Array.Clear(_objectDepthSizes, 0, _objectDepthSizes.Length);
    }
}

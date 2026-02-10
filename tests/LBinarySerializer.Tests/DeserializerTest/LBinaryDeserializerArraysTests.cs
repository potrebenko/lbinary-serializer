namespace LBinarySerializer.Tests;

public class LBinaryDeserializerArraysTests
{
    #region Array of serializable objects

    [Theory]
    [AutoData]
    public void ReadArray_Serializable_ShouldReturnArray(LBinarySerializer serializer, DummyNestedClass[] value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<DummyNestedClass>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }
    
    [Fact]
    public void ReadArray_SerializableEmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();

        // Act
        var result = deserializer.ReadArrayOf<DummyNestedClass>();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void ReadArray_SerializableNullArray_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((DummyNestedClass[])null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<DummyNestedClass>();

        // Assert
        result.Should().BeNull();
    }
    
    #endregion

    #region Array of strings

    [Theory]
    [AutoData]
    public void ReadArray_StringArray_ShouldReturnArray(LBinarySerializer serializer, string[] value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOfStrings();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void ReadArray_StringArrayEmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();   

        // Act
        var result = deserializer.ReadArrayOfStrings();

        // Assert
        result.Should().BeEmpty();
    }
    
    [Theory]
    [AutoData]
    public void ReadArray_StringArrayNullArray_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((string[])null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);
        
        // Act
        var result = deserializer.ReadArrayOfStrings();
        
        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void ReadArray_StringNullItem_ShouldReturnArray(LBinarySerializer serializer, string[] value)
    {
        // Arrange
        value[1] = null;
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOfStrings();
        
        // Assert
        result.Should().BeEquivalentTo(value);
    }
    
    #endregion

    #region Array of primitives

    [Theory]
    [AutoData]
    public void ReadArray_IntArray_ShouldReturnArray(LBinarySerializer serializer, int[] value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<int>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Theory]
    [AutoData]
    public void ReadArray_DoubleArray_ShouldReturnArray(LBinarySerializer serializer, double[] value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<double>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Theory]
    [AutoData]
    public void ReadArray_GUIDArray_ShouldReturnArray(LBinarySerializer serializer, Guid[] value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<Guid>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }
    
    [Fact]
    public void ReadArray_IntArrayEmpty_ShouldReturnEmptyArray()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();
        
        // Act
        var result = deserializer.ReadArrayOf<int>();

        // Assert
        result.Should().BeEmpty();
    }
    
    [Theory]
    [AutoData]
    public void ReadArray_IntArrayNull_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((int[])null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<int>();
        
        // Assert
        result.Should().BeNull();
    }


    [Theory]
    [AutoData]
    public void ReadArray_DateTimeOffset_ShouldReturnValue(LBinarySerializer serializer, DateTimeOffset[] value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<DateTimeOffset>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Theory]
    [AutoData]
    public void ReadArray_ByteArray_ShouldReturnArray(LBinarySerializer serializer, byte[] value)
    {
        // Arrange
        serializer.Write(value, false);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<byte>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Theory]
    [AutoData]
    public void ReadArray_LongArray_ShouldReturnArray(LBinarySerializer serializer, long[] value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<long>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Theory]
    [AutoData]
    public void ReadArray_FloatArray_ShouldReturnArray(LBinarySerializer serializer, float[] value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<float>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Theory]
    [AutoData]
    public void ReadArray_EmptySerializableArray_ShouldReturnEmptyArray(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write(Array.Empty<DummyNestedClass>());
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<DummyNestedClass>();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void ReadArray_EmptyIntArray_ShouldReturnEmptyArray(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write(Array.Empty<int>());
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOf<int>();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void ReadArray_EmptyStringArray_ShouldReturnEmptyArray(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write(Array.Empty<string>());
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadArrayOfStrings();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
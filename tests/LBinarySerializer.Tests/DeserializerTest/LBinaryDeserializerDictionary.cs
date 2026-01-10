namespace LBinarySerializer.Tests;

public class LBinaryDeserializerDictionary
{
    #region Primitives and serializable

    [Theory]
    [AutoData]
    public void ReadDictionary_IntDictionary_ShouldReturnDictionary(LBinarySerializer serializer,
        Dictionary<int, DummyNestedClass> value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDictionaryOf<int, DummyNestedClass>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Theory]
    [AutoData]
    public void ReadDictionary_IntDictionaryEmpty_ShouldReturnEmptyDictionary(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write(new Dictionary<int, DummyNestedClass>());
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDictionaryOf<int, DummyNestedClass>();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void ReadDictionary_IntDictionaryNull_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((Dictionary<int, DummyNestedClass>)null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDictionaryOf<int, DummyNestedClass>();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Primitives and strings

    [Theory]
    [AutoData]
    public void ReadDictionary_ManagedStringDictionary_ShouldReturnDictionary(LBinarySerializer serializer,
        Dictionary<int, string> value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDictionaryOfStrings<int>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void ReadDictionary_ManagedStringDictionaryEmpty_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();

        // Act
        var result = deserializer.ReadDictionaryOfStrings<int>();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void ReadDictionary_ManagedStringDictionaryNull_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((Dictionary<int, string>)null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDictionaryOfStrings<int>();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Strings and serializable

    [Theory]
    [AutoData]
    public void ReadDictionary_StringDictionary_ShouldReturnDictionary(LBinarySerializer serializer,
        Dictionary<string, DummyNestedClass> value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDictionaryOf<DummyNestedClass>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void ReadDictionary_StringDictionary_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();

        // Act
        var result = deserializer.ReadDictionaryOf<DummyNestedClass>();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void ReadDictionary_StringDictionaryNull_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((Dictionary<string, DummyNestedClass>)null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDictionaryOf<DummyNestedClass>();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Strings and primitives

    [Theory]
    [AutoData]
    public void ReadDictionary_IntStringDictionary_ShouldReturnDictionary(LBinarySerializer serializer,
        Dictionary<string, double> value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDictionaryOfStructs<double>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void ReadDictionary_IntStringDictionaryEmpty_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();

        // Act
        var result = deserializer.ReadDictionaryOfStructs<double>();

        // Assert        
        result.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void ReadDictionary_IntStringDictionaryNull_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((Dictionary<string, double>)null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act                
        var result = deserializer.ReadDictionaryOfStructs<double>();

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
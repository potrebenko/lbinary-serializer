namespace LBinarySerializer.Tests;

public class LBinaryDeserializerListsTests
{
    #region Primitives and structures
    
    [Theory]
    [AutoData]
    public void ReadList_DateTimeList_ShouldReturnList(LBinarySerializer serializer, List<DateTime> value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadListOf<DateTime>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }
    
    [Fact]
    public void ReadList_DateTimeListEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();

        // Act
        var result = deserializer.ReadListOf<DateTime>();

        // Assert
        result.Should().BeEmpty();
    }   
    
    [Theory]
    [AutoData]
    public void ReadList_DateTimeListNullList_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((List<DateTime>)null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadListOf<DateTime>();        

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Serializable classes

    [Theory]
    [AutoData]
    public void ReadList_Serializable_ShouldReturnList(LBinarySerializer serializer, List<DummyNestedClass> value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadListOf<DummyNestedClass>();

        // Assert
        result.Should().BeEquivalentTo(value);
    }
    
    [Fact]
    public void ReadList_SerializableEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();

        // Act
        var result = deserializer.ReadListOf<DummyNestedClass>();

        // Assert
        result.Should().BeEmpty();
    }
    
    [Theory]
    [AutoData]
    public void ReadList_SerializableNullList_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((List<DummyNestedClass>)null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadListOf<DummyNestedClass>();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Strings

    [Theory]
    [AutoData]
    public void ReadList_StringList_ShouldReturnList(LBinarySerializer serializer, List<string> value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadListOfStrings();

        // Assert
        result.Should().BeEquivalentTo(value);
    }
    
    [Fact]
    public void ReadList_EmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var deserializer = new LBinaryDeserializer();

        // Act
        var result = deserializer.ReadListOfStrings();        
        
        // Assert
        result.Should().BeEmpty();
    }
    
    [Theory]
    [AutoData]
    public void ReadList_NullList_ShouldReturnNull(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((List<string>)null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadListOfStrings();        
        
        // Assert
        result.Should().BeNull();
    }

    #endregion
}
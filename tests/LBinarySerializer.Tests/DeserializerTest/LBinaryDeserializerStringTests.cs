namespace LBinarySerializer.Tests;

public class LBinaryDeserializerStringTests
{
    [Theory]
    [AutoData]
    public void ReadString_EmptyString_ShouldReturnEmptyString(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write(string.Empty);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadString();

        // Assert
        result.Should().Be(string.Empty);
    }
    
    [Theory]
    [AutoData]
    public void ReadString_NullString_ShouldReturnNullValue(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write((string)null);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadString();

        // Assert
        result.Should().BeNull();
    }
    
    [Theory]
    [AutoData]
    public void ReadString_AnsiString_ShouldReturnValue(LBinarySerializer serializer, string value)
    {
        // Arrange
        serializer.Write(value, EncodingType.ASCII);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadString();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void ReadString_UTF8String_ShouldReturnValue(LBinarySerializer serializer, string value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);
        
        // Act
        var result = deserializer.ReadString();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void ReadString_UnicodeString_ShouldReturnValue(LBinarySerializer serializer, string value)
    {
        // Arrange
        serializer.Write(value, EncodingType.UTF16);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);
        
        // Act
        var result = deserializer.ReadString(EncodingType.UTF16);

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void ReadString_UTF32String_ShouldReturnValue(LBinarySerializer serializer, string value)
    {
        // Arrange
        serializer.Write(value, EncodingType.UTF32);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);
        
        // Act
        var result = deserializer.ReadString(EncodingType.UTF32);

        // Assert
        result.Should().Be(value);
    }
}
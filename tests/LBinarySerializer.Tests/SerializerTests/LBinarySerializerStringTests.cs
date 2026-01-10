using System.Text;

namespace LBinarySerializer.Tests;

public class LBinarySerializerStringTests
{
    [Theory]
    [AutoData]
    public void Write_UTF8String_ShouldContainValue(LBinarySerializer serializer, string value)
    {
        // Act
        serializer.Write(value);

        // Assert
        // Check the header
        var length = BitConverter.ToInt32(serializer.ToArray().Take(4).ToArray());
        length.Should().Be(value.Length);

        // Check the string
        serializer.ToArray().Skip(4).Should().BeEquivalentTo(Encoding.UTF8.GetBytes(value));
    }


    [Theory]
    [AutoData]
    public void Write_ANSIString_ShouldContainValue(LBinarySerializer serializer, string value)
    {
        // Act
        serializer.Write(value, EncodingType.ASCII);

        // Assert
        // Check the header
        var length = BitConverter.ToInt32(serializer.ToArray().Take(4).ToArray());
        length.Should().Be(value.Length);

        // Check the string
        serializer.ToArray().Skip(4).Should().BeEquivalentTo(Encoding.ASCII.GetBytes(value));
    }

    [Theory]
    [AutoData]
    public void Write_UTF8StringCyrillic_ShouldContainValue(LBinarySerializer serializer)
    {
        // Arrange
        var value = "Русский текст!";

        // Act
        serializer.Write(value);

        // Assert
        // Check the header
        var length = BitConverter.ToInt32(serializer.ToArray().Take(4).ToArray());
        length.Should().Be(Encoding.UTF8.GetByteCount(value));

        // Check the string
        serializer.ToArray().Skip(4).Should().BeEquivalentTo(Encoding.UTF8.GetBytes(value));
    }

    [Theory]
    [AutoNSubstituteData]
    public void Write_UTF32String_ShouldContainValue(LBinarySerializer serializer)
    {
        // Act
        var value = "\ud846\ude38";
        serializer.Write(value, EncodingType.UTF32);

        // Assert
        // Check the header
        var length = BitConverter.ToInt32(serializer.ToArray().Take(4).ToArray());
        length.Should().Be(Encoding.UTF32.GetByteCount(value));

        // Check the string
        serializer.ToArray().Skip(4).Should().BeEquivalentTo(Encoding.UTF32.GetBytes(value));
    }

    [Theory]
    [AutoNSubstituteData]
    public void Write_UTF16String_ShouldContainValue(LBinarySerializer serializer)
    {
        // Act
        var value = "\\u263A";
        serializer.Write(value, EncodingType.UTF16);

        // Assert
        // Check the header
        var length = BitConverter.ToInt32(serializer.ToArray().Take(4).ToArray());
        length.Should().Be(Encoding.Unicode.GetByteCount(value));

        // Check the string
        serializer.ToArray().Skip(4).Should().BeEquivalentTo(Encoding.Unicode.GetBytes(value));
    }
}
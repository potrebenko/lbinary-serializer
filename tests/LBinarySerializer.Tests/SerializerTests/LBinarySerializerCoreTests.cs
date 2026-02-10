using System.Text;

namespace LBinarySerializer.Tests;

public class LBinarySerializerCoreTests
{
    [Theory]
    [AutoData]
    public void EnsureCapacity_IncreaseCapacity_ShouldContainValue(string value)
    {
        // Arrange
        var serializer = new LBinarySerializer(10);
        var expectedCount = value.Length + 4; // Length + Header

        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().HaveCount(expectedCount);
    }

    [Fact]
    public void EnsureCapacity_OutOfCapacity_ShouldThrowException()
    {
        // Arrange
        var value = new byte[Array.MaxLength - 3];
        var serializer = new LBinarySerializer(10);
        serializer.Write([1, 2, 3, 4]);

        // Act
        serializer.Invoking(x => x.Write(value)).Should().Throw<InvalidOperationException>()
            .WithMessage("The buffer is too big");
    }

    [Fact]
    public void ToArray_EmptySerializer_ShouldReturnEmptyArray()
    {
        // Arrange
        var serializer = new LBinarySerializer();

        // Act
        var result = serializer.ToArray();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void ToMemory_ShouldReturnSameDataAsToArray(LBinarySerializer serializer, int value)
    {
        // Arrange
        serializer.Write(value);

        // Act
        var memory = serializer.ToMemory();

        // Assert
        memory.ToArray().Should().BeEquivalentTo(serializer.ToArray());
    }

    [Theory]
    [AutoData]
    public void Reset_ShouldAllowReuse(int first, int second)
    {
        // Arrange
        var serializer = new LBinarySerializer();
        serializer.Write(first);

        // Act
        serializer.Reset();
        serializer.Write(second);

        // Assert
        var result = serializer.ToArray();
        result.Should().HaveCount(sizeof(int));
        BitConverter.ToInt32(result).Should().Be(second);
    }

    [Fact]
    public void Reset_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var serializer = new LBinarySerializer();
        serializer.Dispose();

        // Act & Assert
        serializer.Invoking(x => x.Reset()).Should().Throw<ObjectDisposedException>();
    }

    [Theory]
    [AutoData]
    public void Write_MultiplePrimitives_ShouldSerializeInOrder(int intVal, bool boolVal, long longVal)
    {
        // Arrange
        var serializer = new LBinarySerializer();

        // Act
        serializer.Write(intVal);
        serializer.Write(boolVal);
        serializer.Write(longVal);

        // Assert
        var result = serializer.ToArray();
        result.Should().HaveCount(sizeof(int) + 1 + sizeof(long));

        var deserializer = new LBinaryDeserializer(result);
        deserializer.ReadInt().Should().Be(intVal);
        deserializer.ReadBool().Should().Be(boolVal);
        deserializer.ReadLong().Should().Be(longVal);
    }
}
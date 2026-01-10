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
}
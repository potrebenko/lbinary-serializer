namespace LBinarySerializer.Tests;

public class LBinaryDeserializerBackwardCompatibilityTests
{
    [Theory]
    [AutoData]
    public void Deserialize_DifferentFields_ShouldDeserialize(LBinarySerializer serializer, DummyExtended value)
    {
        // Arrange
        value.Serialize(serializer);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.Deserialize<DummyShort>();

        // Assert
        result.Array.Select(x => x.Name).Should().BeEquivalentTo(value.Array!.Select(x => x.Name));
    }
}
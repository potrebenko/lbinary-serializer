namespace LBinarySerializer.Tests;

public class LBinaryDeserializerObjectsTests
{
    [Theory]
    [AutoData]
    public void Deserialize_ClassObject_ShouldReturnDefaultValue(LBinaryDeserializer deserializer,
        LBinarySerializer serializer, DummyNestedClass value)
    {
        // Arrange
        value.Serialize(serializer);
        var data = serializer.ToArray();
        
        // Act
        var result = deserializer.Deserialize<DummyNestedClass>(data);

        // Assert
        result!.Age.Should().Be(value.Age);
        result.BirthDate.Should().Be(value.BirthDate);
        result.Guid.Should().Be(value.Guid);
        result.IsAdmin.Should().Be(value.IsAdmin);
        result.Name.Should().Be(value.Name);
        result.SecondName.Should().Be(value.SecondName);
        result.DummyEnum.Should().Be(value.DummyEnum);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_NestedObject_ShouldReturnValue(DummyBaseClass value)
    {
        // Arrange
        var serializer = new LBinarySerializer();
        value.Serialize(serializer);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.Deserialize<DummyBaseClass>()!;

        // Assert
        result.Age.Should().Be(value.Age);
        result.Name.Should().Be(value.Name);
        result.Dummy.Should().NotBeNull();
        result.Dummy!.Age.Should().Be(value.Dummy!.Age);
        result.Dummy!.BirthDate.Should().Be(value.Dummy!.BirthDate);
        result.Dummy!.Guid.Should().Be(value.Dummy!.Guid);
        result.Dummy!.IsAdmin.Should().Be(value.Dummy!.IsAdmin);
        result.Dummy!.Name.Should().Be(value.Dummy!.Name);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_NotDefinedStructure_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadStruct<DummyStruct>();

        // Assert
        result.Should().Be(default(DummyStruct));
    }
    
    [Theory]
    [AutoNSubstituteData]
    public void Deserialize_UnmanagedStructure_ShouldReturnValue(LBinarySerializer serializer, DummyStruct value)
    {
        // Arrange
        serializer.WriteStructure(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadStruct<DummyStruct>();

        // Assert
        result.Should().Be(value);
    }
}
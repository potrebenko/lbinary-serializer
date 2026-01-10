namespace LBinarySerializer.Tests;

public class LBinaryDeserializerPrimitivesTests
{
    [Theory]
    [AutoData]
    public void Deserialize_Bool_ShouldReturnTrue(LBinarySerializer serializer)
    {
        // Arrange
        serializer.Write(true);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadBool();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void Deserialize_BoolOutOfRange_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);
        
        // Act
        deserializer.ReadBool();
        var result = deserializer.ReadBool();

        // Assert
        result.Should().BeFalse();
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_Byte_ShouldReturnValue(LBinarySerializer serializer, byte value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadByte();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_ByteOutOfRange_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var defaultValue = default(byte);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        deserializer.ReadByte();
        var result = deserializer.ReadByte();

        // Assert
        result.Should().Be(defaultValue);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_Int32_ShouldReturnValue(LBinarySerializer serializer, int value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadInt();

        // Assert
        result.Should().Be(value);
    }
    
    [Fact]
    public void Deserialize_Int32OutOfRange_ShouldReturnZero()
    {
        // Arrange
        var serializer = new LBinarySerializer(1);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadLong();

        // Assert
        result.Should().Be(0);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_UInt32_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadUInt();

        // Assert
        result.Should().Be(0);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_UInt32OutOfRange_ShouldReturnDefaultValue(LBinarySerializer serializer, uint value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadUInt();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_Short_ShouldReturnValue(LBinarySerializer serializer, short value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadShort();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_ShortOutOfRange_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadShort();

        // Assert
        result.Should().Be(0);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_UShort_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadUShort();

        // Assert
        result.Should().Be(0);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_UShortOutOfRange_ShouldReturnValue(LBinarySerializer serializer, ushort value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadUShort();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_ULong_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadULong();

        // Assert
        result.Should().Be(0);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_ULongOutOfRange_ShouldReturnValue(LBinarySerializer serializer, ulong value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadULong();

        // Assert
        result.Should().Be(value);
    }

    [Theory] 
    [AutoData] 
    public void Deserialize_Float_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadFloat();

        // Assert
        result.Should().Be(0);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_FloatOutOfRange_ShouldReturnValue(LBinarySerializer serializer, float value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadFloat();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_Double_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDouble();

        // Assert
        result.Should().Be(0);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_DoubleOutOfRange_ShouldReturnValue(LBinarySerializer serializer, double value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDouble();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_Decimal_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDecimal();

        // Assert
        result.Should().Be(0m);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_DecimalOutOfRange_ShouldReturnValue(LBinarySerializer serializer, decimal value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDecimal();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_HalfOutOfRange_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadHalf();

        // Assert
        result.Should().Be(Half.Zero);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_Half_ShouldReturnValue(LBinarySerializer serializer, Half value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadHalf();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_Guid_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadGuid();

        // Assert
        result.Should().Be(Guid.Empty);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_GuidOutOfRange_ShouldReturnDefaultValue(LBinarySerializer serializer, Guid value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadGuid();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_DateTime_ShouldReturnMinDateTime(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDateTime();

        // Assert
        result.Should().Be(DateTime.MinValue);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_DateTime_ShouldReturnUnspecifiedTime(LBinarySerializer serializer)
    {
        // Arrange
        var localTime = new DateTime(2025, 05, 12, 0, 0, 0, DateTimeKind.Unspecified);
        serializer.Write(localTime);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDateTime();

        // Assert
        result.Should().Be(localTime);
        result.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Theory]
    [AutoData]
    public void Deserialize_DateTime_ShouldReturnLocalTime(LBinarySerializer serializer)
    {
        // Arrange
        var localTime = new DateTime(2025, 05, 12, 0, 0, 0, DateTimeKind.Local);
        serializer.Write(localTime);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDateTime();

        // Assert
        result.Should().Be(localTime);
        result.Kind.Should().Be(DateTimeKind.Local);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_DateTime_ShouldReturnUtcTime(LBinarySerializer serializer)
    {
        // Arrange
        var localTime = new DateTime(2025, 05, 12, 0, 0, 0, DateTimeKind.Utc);
        serializer.Write(localTime);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDateTime();

        // Assert
        result.Should().Be(localTime);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [AutoData]
    public void Deserialize_DateTimeOffset_ShouldReturnDateTimeOffset(LBinarySerializer serializer)
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2025, 5, 12, 0, 0, 0, TimeSpan.FromHours(2));
        serializer.Write(dateTimeOffset);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadDateTimeOffset();

        // Assert
        result.Should().Be(dateTimeOffset);
    }

    [Fact]
    public void Deserialize_DateTimeOffset_ShouldReturnMinDateTimeOffset()
    {
        // Arrange
        var expectedValue = DateTimeOffset.MinValue;
        var deserializer = new LBinaryDeserializer();

        // Act
        var result = deserializer.ReadDateTimeOffset();

        // Assert
        result.Should().Be(expectedValue);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_TimeSpan_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadTimeSpan();

        // Assert
        result.Should().Be(TimeSpan.Zero);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_TimeSpan_ShouldReturnValue(LBinarySerializer serializer, TimeSpan value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadTimeSpan();

        // Assert
        result.Should().Be(value);
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_CharOutOfRange_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadChar();

        // Assert
        result.Should().Be(char.MinValue);
    }

    [Theory]
    [AutoData]
    public void Deserialize_Char_ShouldReturnValue(LBinarySerializer serializer, char value)
    {
        // Arrange
        serializer.Write(value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadChar();

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [AutoData]
    public void Deserialize_Enum_ShouldReturnValue(LBinarySerializer serializer, DummyEnum value)
    {
        // Arrange
        serializer.WriteEnum( value);
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadEnum<DummyEnum>();

        // Assert
        result.Should().Be(value);    
    }
    
    [Theory]
    [AutoData]
    public void Deserialize_EnumOutOfRange_ShouldReturnDefaultValue(LBinarySerializer serializer)
    {
        // Arrange
        var data = serializer.ToArray();
        var deserializer = new LBinaryDeserializer(data);

        // Act
        var result = deserializer.ReadEnum<DummyEnum>();

        // Assert
        result.Should().Be(DummyEnum.Value1);    
    }
}
namespace LBinarySerializer.Tests;

/// <summary>
/// Primitive types serialization tests
/// </summary>
public class LBinarySerializerPrimitivesTests
{
    [Theory]
    [AutoData]
    public void Write_True_ShouldContainOne(LBinarySerializer serializer)
    {
        // Arrange
        var value = true;

        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray()[0].Should().Be(1);
        serializer.ToArray().Should().HaveCount(1);
    }
    
    [Theory]
    [AutoData]
    public void Write_False_ShouldContainZero(LBinarySerializer serializer)
    {
        // Arrange
        var value = false;

        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray()[0].Should().Be(0);
        serializer.ToArray().Should().HaveCount(1);
    }
    
    [Theory]
    [AutoData]
    public void Write_Byte_ShouldContainValue(LBinarySerializer serializer, byte value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray()[0].Should().Be(value);
        serializer.ToArray().Should().HaveCount(1);
    }
    
    [Theory]
    [AutoData]
    public void Write_ByteArray_ShouldContainValue(LBinarySerializer serializer, byte[] value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(value);
        serializer.ToArray().Should().HaveCount(value.Length);
    }
    
    [Theory]
    [AutoData]
    public void Write_Span_ShouldContainValue(LBinarySerializer serializer, byte[] value)
    {
        // Act
        serializer.Write(value.AsSpan());

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(value);
        serializer.ToArray().Should().HaveCount(value.Length);
    }
    
    [Theory]
    [AutoData]
    public void Write_Int32_ShouldContainValue(LBinarySerializer serializer, int value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(int));
    }
    
    [Theory]
    [AutoData]
    public void Write_UInt32_ShouldContainValue(LBinarySerializer serializer, uint value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(uint));
    }
    
    [Theory]
    [AutoData]
    public void Write_Int16_ShouldContainValue(LBinarySerializer serializer, short value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(short));
    }
    
    [Theory]
    [AutoData]
    public void Write_UInt16_ShouldContainValue(LBinarySerializer serializer, ushort value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(ushort));
    }
    
    [Theory]
    [AutoData]
    public void Write_Long_ShouldContainValue(LBinarySerializer serializer, long value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(long));
    }
    
    [Theory]
    [AutoData]
    public void Write_ULong_ShouldContainValue(LBinarySerializer serializer, ulong value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(ulong));
    }
    
    [Theory]
    [AutoData]
    public void Write_Float_ShouldContainValue(LBinarySerializer serializer, float value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(float));
    }
    
    [Theory]
    [AutoData]
    public void Write_Double_ShouldContainValue(LBinarySerializer serializer, double value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(double));
    }
        
    [Theory]
    [AutoData]
    public void Write_Decimal_ShouldContainValue(LBinarySerializer serializer, decimal value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(decimal.GetBits(value).SelectMany(BitConverter.GetBytes));
        serializer.ToArray().Should().HaveCount(sizeof(decimal));
    }
    
    [Theory]
    [AutoData]
    public void Write_Half_ShouldContainValue(LBinarySerializer serializer, Half value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(short));
    }
    
    [Theory]
    [AutoData]
    public void Write_Guid_ShouldNotEmptyGuid(LBinarySerializer serializer, Guid value)
    {
        // Act
        var guidSize = 16;
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(value.ToByteArray());
        serializer.ToArray().Should().HaveCount(guidSize);
    }
    
    [Theory]
    [AutoData]
    public void Write_DateTime_ShouldContainValue(LBinarySerializer serializer, DateTime value)
    {
        // Arrange
        var expectedLength = sizeof(int) + sizeof(long);
        
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Take(4).Should().BeEquivalentTo(BitConverter.GetBytes((int)DateTimeKind.Unspecified));
        serializer.ToArray().Skip(4).Should().BeEquivalentTo(BitConverter.GetBytes(value.Ticks));
        serializer.ToArray().Should().HaveCount(expectedLength);
    }
    
    [Theory]
    [AutoData]
    public void Write_TimeSpan_ShouldContainValue(LBinarySerializer serializer, TimeSpan value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value.Ticks));
        serializer.ToArray().Should().HaveCount(sizeof(long));
    }
    
    [Theory]
    [AutoData]
    public void Write_Char_ShouldContainValue(LBinarySerializer serializer, char value)
    {
        // Act
        serializer.Write(value);

        // Assert
        serializer.ToArray().Should().BeEquivalentTo(BitConverter.GetBytes(value));
        serializer.ToArray().Should().HaveCount(sizeof(short));
    }
}
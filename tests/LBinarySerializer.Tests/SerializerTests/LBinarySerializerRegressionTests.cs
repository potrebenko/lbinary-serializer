namespace LBinarySerializer.Tests;

public class LBinarySerializerRegressionTests
{
    [Theory]
    [InlineData(13)]
    [InlineData(16)]
    public void Write_ObjectAtBufferBoundary_ShouldRoundTrip(int padding)
    {
        // Arrange
        using var serializer = new LBinarySerializer(16);
        serializer.Write(new byte[padding]);
        var value = new DummyBaseClass { Dummy = new DummyNestedClass { Age = 42 }, Age = 99 };

        // Act
        value.Serialize(serializer);

        // Assert
        var result = new LBinaryDeserializer(serializer.ToArray()[padding..]).Deserialize<DummyBaseClass>();
        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void Write_MaximumObjectDepth_ShouldRoundTrip()
    {
        // Arrange
        using var serializer = new LBinarySerializer();
        var value = new DepthValue { Remaining = SerializerSettings.MaxDepthSize - 1 };

        // Act
        value.Serialize(serializer);

        // Assert
        new LBinaryDeserializer(serializer.ToArray()).Deserialize<DepthValue>()
            .Remaining.Should().Be(value.Remaining);
    }

    [Fact]
    public void Write_ExcessiveObjectDepth_ShouldRejectBeforeReservingHeader()
    {
        // Arrange
        using var serializer = new LBinarySerializer();
        var value = new DepthValue { Remaining = SerializerSettings.MaxDepthSize };

        // Act
        Action write = () => value.Serialize(serializer);

        // Assert
        write.Should().Throw<InvalidOperationException>().WithMessage("*nesting depth*");
        serializer.ToMemory().Length.Should().Be((2 * SerializerSettings.MaxDepthSize - 1) * sizeof(int));
        serializer.Reset();
        serializer.Write(42);
        new LBinaryDeserializer(serializer.ToArray()).ReadInt().Should().Be(42);
    }

    public class DepthValue : ILBinarySerializable
    {
        public int Remaining { get; set; }

        public void Serialize(LBinarySerializer serializer)
        {
            serializer.Write(Remaining);
            serializer.Write(Remaining == 0 ? null : new DepthValue { Remaining = Remaining - 1 });
        }

        public void Deserialize<T>(LBinaryDeserializer deserializer)
        {
            Remaining = deserializer.ReadInt();
            var child = deserializer.Deserialize<DepthValue>();
            if (Remaining == 0)
            {
                child.Should().BeNull();
            }
            else
            {
                child.Remaining.Should().Be(Remaining - 1);
            }
        }
    }

    [Theory]
    [InlineData(EncodingType.ASCII)]
    [InlineData(EncodingType.UTF16)]
    public void Write_StringLargerThanBuffer_ShouldRoundTrip(EncodingType encoding)
    {
        using var serializer = new LBinarySerializer(16);
        var value = new string('A', 4096);

        serializer.Write(value, encoding);

        new LBinaryDeserializer(serializer.ToArray()).ReadString(encoding).Should().Be(value);
    }

    [Theory]
    [InlineData(EncodingType.ASCII)]
    [InlineData(EncodingType.UTF16)]
    public void Write_StringAtBufferBoundary_ShouldGrowBuffer(EncodingType encoding)
    {
        using var serializer = new LBinarySerializer(16);
        serializer.Write(new byte[12]);

        serializer.Write("AB", encoding);

        var deserializer = new LBinaryDeserializer(serializer.ToArray());
        deserializer.ReadInt();
        deserializer.ReadInt();
        deserializer.ReadInt();
        deserializer.ReadString(encoding).Should().Be("AB");
    }

    [Fact]
    public void WriteEnum_ByteEnumAfterReset_ShouldInitializeEntireWireValue()
    {
        using var serializer = new LBinarySerializer();
        serializer.Write(new byte[] { 0x5A, 0x5A, 0x5A, 0x5A });
        serializer.Reset();

        serializer.WriteEnum(ByteEnum.One);

        serializer.ToArray().Should().Equal(new byte[] { 1, 0, 0, 0 });
    }

    [Fact]
    public void WriteEnum_LongEnum_ShouldRejectUnsupportedWireSize()
    {
        using var serializer = new LBinarySerializer();

        Action write = () => serializer.WriteEnum(LongEnum.AboveUInt32);

        write.Should().Throw<NotSupportedException>();
        serializer.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void WriteEnum_SupportedUnderlyingTypes_ShouldRoundTripInFourBytes()
    {
        AssertEnumRoundTrip(ByteEnum.One);
        AssertEnumRoundTrip(SByteEnum.Negative);
        AssertEnumRoundTrip(ShortEnum.Negative);
        AssertEnumRoundTrip(UShortEnum.Maximum);
        AssertEnumRoundTrip(IntEnum.Negative);
        AssertEnumRoundTrip(UIntEnum.Maximum);
    }

    private static void AssertEnumRoundTrip<TEnum>(TEnum value) where TEnum : unmanaged, Enum
    {
        using var serializer = new LBinarySerializer();
        serializer.WriteEnum(value);

        serializer.ToArray().Should().HaveCount(4);
        new LBinaryDeserializer(serializer.ToArray()).ReadEnum<TEnum>().Should().Be(value);
    }

    [Fact]
    public void Write_Int32_ShouldUseLittleEndianByteOrder()
    {
        using var serializer = new LBinarySerializer();

        serializer.Write(0x01020304);

        serializer.ToArray().Should().Equal(new byte[] { 4, 3, 2, 1 });
    }

    [Fact(Skip = "Opt-in: allocates several GiB. Remove Skip only on a machine with sufficient memory.")]
    public void Write_LargePayloadBelowMaximumSize_ShouldGrowWithoutIntegerOverflow()
    {
        using var serializer = new LBinarySerializer(1 << 30);
        var payload = new byte[1_610_612_736];
        payload[0] = 0x12;
        payload[^1] = 0x34;

        serializer.Write(payload);

        var result = serializer.ToMemory();
        result.Length.Should().Be(payload.Length);
        result.Span[0].Should().Be(0x12);
        result.Span[^1].Should().Be(0x34);
    }

    private enum ByteEnum : byte { One = 1 }

    private enum SByteEnum : sbyte { Negative = -1 }

    private enum ShortEnum : short { Negative = -1 }

    private enum UShortEnum : ushort { Maximum = ushort.MaxValue }

    private enum IntEnum : int { Negative = -1 }

    private enum UIntEnum : uint { Maximum = uint.MaxValue }
    public enum LongEnum : long { AboveUInt32 = 1L << 32 }
}

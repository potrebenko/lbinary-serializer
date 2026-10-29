namespace LBinarySerializer.Tests;

public class LBinaryDeserializerRegressionTests
{
    [Theory]
    [InlineData("string-array")]
    [InlineData("string-list")]
    [InlineData("object-array")]
    [InlineData("object-list")]
    public void ReadCollection_FirstItemConsumesPayload_ShouldRejectMissingSecondHeader(string kind)
    {
        // Arrange
        using var serializer = new LBinarySerializer();
        serializer.Write(2);
        if (kind.StartsWith("string"))
        {
            serializer.Write("ABCD");
        }
        else
        {
            serializer.Write(new StringValue { Value = "ABCD" });
        }
        var deserializer = new LBinaryDeserializer(serializer.ToArray());

        // Act
        Action read = kind switch
        {
            "string-array" => () => deserializer.ReadArrayOfStrings(),
            "string-list" => () => deserializer.ReadListOfStrings(),
            "object-array" => () => deserializer.ReadArrayOf<StringValue>(),
            "object-list" => () => deserializer.ReadListOf<StringValue>(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        // Assert
        read.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("struct-object", false)]
    [InlineData("string-object", false)]
    [InlineData("string-struct", false)]
    [InlineData("struct-string", false)]
    [InlineData("struct-object", true)]
    [InlineData("string-object", true)]
    [InlineData("string-struct", true)]
    [InlineData("struct-string", true)]
    public void ReadDictionary_EntryConsumesPayload_ShouldRejectMissingHeader(string kind, bool missingKey)
    {
        // Arrange
        using var serializer = new LBinarySerializer();
        serializer.Write(missingKey ? 2 : 1);
        if (kind.StartsWith("string"))
        {
            serializer.Write("ABCD");
        }
        else
        {
            serializer.WriteStructure(42);
        }
        if (missingKey)
        {
            if (kind.EndsWith("object"))
            {
                serializer.Write(new StringValue { Value = "ABCD" });
            }
            else if (kind.EndsWith("struct"))
            {
                serializer.WriteStructure(42);
            }
            else
            {
                serializer.Write("ABCD");
            }
        }
        var deserializer = new LBinaryDeserializer(serializer.ToArray());

        // Act
        Action read = kind switch
        {
            "struct-object" => () => deserializer.ReadDictionaryOf<int, StringValue>(),
            "string-object" => () => deserializer.ReadDictionaryOf<StringValue>(),
            "string-struct" => () => deserializer.ReadDictionaryOfStructs<int>(),
            "struct-string" => () => deserializer.ReadDictionaryOfStrings<int>(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        // Assert
        read.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Deserialize_OlderNestedObject_ShouldDefaultMissingChildAndKeepFollowingData(int objectSize)
    {
        // Arrange
        using var serializer = new LBinarySerializer();
        serializer.Write(objectSize);
        if (objectSize > 0)
        {
            serializer.Write(42);
        }
        serializer.Write(99);

        // Act
        var result = new LBinaryDeserializer(serializer.ToArray()).Deserialize<NodeContainer>();

        // Assert
        result.Node.Should().NotBeNull();
        result.Node.Value.Should().Be(objectSize == 0 ? 0 : 42);
        result.Node.Child.Should().BeNull();
        result.After.Should().Be(99);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadObjects_ExplicitNullAndEmptyHeaders_ShouldPreserveItems(bool asList)
    {
        // Arrange
        using var serializer = new LBinarySerializer();
        serializer.Write(3);
        serializer.Write((StringValue)null);
        serializer.Write(0);
        serializer.Write(new StringValue { Value = "last" });
        var deserializer = new LBinaryDeserializer(serializer.ToArray());

        // Act
        var result = asList
            ? deserializer.ReadListOf<StringValue>().ToArray()
            : deserializer.ReadArrayOf<StringValue>();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().BeNull();
        result[1].Should().NotBeNull();
        result[1].Value.Should().BeEmpty();
        result[2].Value.Should().Be("last");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadStrings_ExplicitNullAndEmptyHeaders_ShouldPreserveItems(bool asList)
    {
        // Arrange
        using var serializer = new LBinarySerializer();
        string[] values = ["first", null, ""];
        serializer.Write(values);
        var deserializer = new LBinaryDeserializer(serializer.ToArray());

        // Act
        var result = asList ? deserializer.ReadListOfStrings().ToArray() : deserializer.ReadArrayOfStrings();

        // Assert
        result.Should().Equal(values);
    }

    public class NodeContainer : ILBinarySerializable
    {
        public Node Node { get; set; }
        public int After { get; set; }
        public void Serialize(LBinarySerializer serializer) => throw new NotSupportedException();
        public void Deserialize<T>(LBinaryDeserializer deserializer)
        {
            Node = deserializer.Deserialize<Node>();
            After = deserializer.ReadInt();
        }
    }

    public class Node : ILBinarySerializable
    {
        public int Value { get; set; }
        public Node Child { get; set; }
        public void Serialize(LBinarySerializer serializer) => throw new NotSupportedException();
        public void Deserialize<T>(LBinaryDeserializer deserializer)
        {
            Value = deserializer.ReadInt();
            Child = deserializer.Deserialize<Node>();
        }
    }

    [Theory]
    [InlineData(EncodingType.UTF8)]
    [InlineData(EncodingType.ASCII)]
    [InlineData(EncodingType.UTF16)]
    [InlineData(EncodingType.UTF32)]
    public void ReadString_LengthBeyondInput_ShouldRejectPayload(EncodingType encoding)
    {
        var deserializer = new LBinaryDeserializer(new byte[] { 64, 0, 0, 0 });

        Action read = () => deserializer.ReadString(encoding);

        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ReadString_NegativeLength_ShouldRejectPayload()
    {
        var deserializer = new LBinaryDeserializer(new byte[] { 254, 255, 255, 255 });

        Action read = () => deserializer.ReadString();

        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ReadInt_PartialField_ShouldRejectPayload()
    {
        var deserializer = new LBinaryDeserializer(new byte[] { 1, 2, 3 });

        Action read = () => deserializer.ReadInt();

        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ReadString_LengthBeyondObjectBoundary_ShouldRejectPayload()
    {
        // The string bytes exist in the array but belong outside the four-byte nested object.
        var deserializer = new LBinaryDeserializer(new byte[]
        {
            4, 0, 0, 0,
            4, 0, 0, 0,
            65, 66, 67, 68
        });

        Action read = () => deserializer.Deserialize<StringContainer>();

        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ReadStruct_SizeSmallerThanTarget_ShouldRejectPayload()
    {
        // Padding keeps the old eight-byte unsafe read inside the array.
        var deserializer = new LBinaryDeserializer(new byte[]
        {
            1, 0, 0, 0, 7, 8, 9, 10, 11, 12, 13, 14
        });

        Action read = () => deserializer.ReadStruct<long>();

        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ReadStruct_GenericContract_ShouldRequireUnmanagedTypes()
    {
        // Inspect the public constraint without constructing object references from raw bytes.
        var typeParameter = typeof(LBinaryDeserializer).GetMethod(nameof(LBinaryDeserializer.ReadStruct))!
            .GetGenericArguments().Single();

        typeParameter.GetCustomAttributesData().Should().Contain(attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsUnmanagedAttribute");
    }

    [Fact]
    public void ReadEnum_LongEnum_ShouldRejectUnsupportedWireSize()
    {
        // The following field keeps the old eight-byte read inside the array.
        var deserializer = new LBinaryDeserializer(new byte[] { 1, 0, 0, 0, 2, 0, 0, 0 });

        Action read = () => deserializer.ReadEnum<LBinarySerializerRegressionTests.LongEnum>();

        read.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ReadArray_CountOverflowsByteSize_ShouldRejectPayload()
    {
        var deserializer = new LBinaryDeserializer(new byte[] { 0, 0, 0, 64 });

        Action read = () => deserializer.ReadArrayOf<int>();

        read.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(int.MaxValue)]
    public void ReadList_InvalidCount_ShouldRejectBeforeAllocating(int count)
    {
        var deserializer = new LBinaryDeserializer(BitConverter.GetBytes(count));

        Action read = () => deserializer.ReadListOf<int>();

        read.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("struct-object")]
    [InlineData("string-object")]
    [InlineData("string-struct")]
    [InlineData("struct-string")]
    public void ReadDictionary_CountWithoutEntries_ShouldRejectBeforeAllocating(string kind)
    {
        var deserializer = new LBinaryDeserializer(BitConverter.GetBytes(int.MaxValue));
        Action read = kind switch
        {
            "struct-object" => () => deserializer.ReadDictionaryOf<int, StringValue>(),
            "string-object" => () => deserializer.ReadDictionaryOf<StringValue>(),
            "string-struct" => () => deserializer.ReadDictionaryOfStructs<int>(),
            "struct-string" => () => deserializer.ReadDictionaryOfStrings<int>(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        read.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(64)]
    public void Deserialize_InvalidObjectLength_ShouldRejectPayload(int length)
    {
        var deserializer = new LBinaryDeserializer(BitConverter.GetBytes(length));

        Action read = () => deserializer.Deserialize<StringContainer>();

        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Deserialize_ExcessiveNesting_ShouldRejectPayload()
    {
        using var serializer = new LBinarySerializer();
        for (var depth = SerializerSettings.MaxDepthSize; depth > 0; depth--)
        {
            serializer.Write(depth * sizeof(int));
        }
        serializer.Write(-1);
        var deserializer = new LBinaryDeserializer(serializer.ToArray());

        Action read = () => deserializer.Deserialize<RecursiveValue>();

        read.Should().Throw<InvalidDataException>().WithMessage("*nesting depth*");
    }

    [Fact]
    public void Deserialize_OlderNestedObjects_ShouldDefaultMissingFieldsAndKeepFollowingData()
    {
        using var serializer = new LBinarySerializer();
        var value = new DummyShort
        {
            Age = 42,
            Array = [new() { Name = "first" }, new() { Name = "second" }]
        };
        value.Serialize(serializer);
        serializer.Write("after-array");

        var result = new LBinaryDeserializer(serializer.ToArray()).Deserialize<DummyExtended>();

        result.Age.Should().Be(42);
        result.Array.Select(item => item.Name).Should().Equal("first", "second");
        result.Array.Select(item => item.Age).Should().Equal(0, 0);
        result.Name.Should().Be("after-array");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadStrings_CountWithoutItemHeaders_ShouldRejectPayload(bool asList)
    {
        var deserializer = new LBinaryDeserializer(new byte[] { 4, 0, 0, 0 });

        Action read = () =>
        {
            if (asList)
            {
                deserializer.ReadListOfStrings();
            }
            else
            {
                deserializer.ReadArrayOfStrings();
            }
        };

        read.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadObjects_CountWithoutItemHeaders_ShouldRejectPayload(bool asList)
    {
        var deserializer = new LBinaryDeserializer(new byte[] { 4, 0, 0, 0 });

        Action read = () =>
        {
            if (asList)
            {
                deserializer.ReadListOf<StringValue>();
            }
            else
            {
                deserializer.ReadArrayOf<StringValue>();
            }
        };

        read.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("é")]
    [InlineData("ключ")]
    public void ReadDictionaryOfStructs_UnicodeKey_ShouldPreserveKey(string key)
    {
        using var serializer = new LBinarySerializer();
        serializer.Write(new Dictionary<string, int> { [key] = 42 });

        var result = new LBinaryDeserializer(serializer.ToArray()).ReadDictionaryOfStructs<int>();

        result.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, int>(key, 42));
    }

    [Fact]
    public void ReadDictionaryOfObjects_UnicodeKeys_ShouldRemainDistinct()
    {
        using var serializer = new LBinarySerializer();
        serializer.Write(new Dictionary<string, StringValue>
        {
            ["é"] = new() { Value = "first" },
            ["ö"] = new() { Value = "second" }
        });

        var result = new LBinaryDeserializer(serializer.ToArray()).ReadDictionaryOf<StringValue>();

        result.Should().HaveCount(2);
        result["é"].Value.Should().Be("first");
        result["ö"].Value.Should().Be("second");
    }

    public class StringContainer : ILBinarySerializable
    {
        public void Serialize(LBinarySerializer serializer) => throw new NotSupportedException();
        public void Deserialize<T>(LBinaryDeserializer deserializer) => deserializer.Deserialize<StringValue>();
    }

    public class StringValue : ILBinarySerializable
    {
        public string Value { get; set; }
        public void Serialize(LBinarySerializer serializer) => serializer.Write(Value);
        public void Deserialize<T>(LBinaryDeserializer deserializer) => Value = deserializer.ReadString();
    }

    public class RecursiveValue : ILBinarySerializable
    {
        public void Serialize(LBinarySerializer serializer) => throw new NotSupportedException();
        public void Deserialize<T>(LBinaryDeserializer deserializer) => deserializer.Deserialize<RecursiveValue>();
    }
}

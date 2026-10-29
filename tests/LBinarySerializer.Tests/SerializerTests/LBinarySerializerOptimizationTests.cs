using System.Buffers.Binary;
using System.Collections;
using System.Collections.ObjectModel;
using System.Text;

namespace LBinarySerializer.Tests;

public class LBinarySerializerOptimizationTests
{
    public static IEnumerable<object[]> Strings()
    {
        foreach (var encoding in Enum.GetValues<EncodingType>())
        foreach (var capacity in new[] { 16, 32768 })
        foreach (var value in new[] { null, "", "ASCII", "é世😀", "\ud800", "x\udc00y", new string('é', 4096) })
        {
            yield return new object[] { encoding, capacity, value };
        }
    }

    [Theory]
    [MemberData(nameof(Strings))]
    public void Write_StringWithSpareCapacityOrGrowth_ShouldPreserveWireBytes(EncodingType encoding, int capacity, string value)
    {
        // Arrange
        using var serializer = new LBinarySerializer(capacity);
        serializer.Write(new byte[13]);
        var textEncoding = encoding switch
        {
            EncodingType.UTF8 => Encoding.UTF8,
            EncodingType.UTF16 => Encoding.Unicode,
            EncodingType.UTF32 => Encoding.UTF32,
            EncodingType.ASCII => Encoding.ASCII,
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
        var bytes = value == null ? Array.Empty<byte>() : textEncoding.GetBytes(value);
        var expected = new byte[sizeof(int) + bytes.Length + sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(expected, value == null ? -1 : bytes.Length);
        bytes.CopyTo(expected, sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(sizeof(int) + bytes.Length), 42);

        // Act
        serializer.Write(value, encoding);
        serializer.Write(42);

        // Assert
        serializer.ToArray()[13..].Should().Equal(expected);
    }

    [Theory]
    [InlineData("struct-object")]
    [InlineData("string-object")]
    [InlineData("string-struct")]
    public void Write_DictionaryAndInterfaceFallback_ShouldProduceIdenticalBytes(string kind)
    {
        // Arrange
        using var concrete = new LBinarySerializer();
        using var fallback = new LBinarySerializer();
        var value = new DummyNestedClass { Name = "é世😀", Age = 42 };

        // Act
        switch (kind)
        {
            case "struct-object":
                var numbers = new Dictionary<int, DummyNestedClass> { [1] = value, [2] = null };
                concrete.Write(numbers);
                fallback.Write(new ReadOnlyDictionary<int, DummyNestedClass>(numbers));
                break;
            case "string-object":
                var objects = new Dictionary<string, DummyNestedClass> { ["é"] = value, ["empty"] = null };
                concrete.Write(objects);
                fallback.Write(new ReadOnlyDictionary<string, DummyNestedClass>(objects));
                break;
            case "string-struct":
                var primitives = new Dictionary<string, int> { ["é"] = 42, ["zero"] = 0 };
                concrete.Write(primitives);
                fallback.Write(new ReadOnlyDictionary<string, int>(primitives));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        // Assert
        concrete.ToArray().Should().Equal(fallback.ToArray());
    }

    [Fact]
    public void Write_NestedObjects_ShouldPreserveSizeHeadersAndFollowingFields()
    {
        // Arrange
        using var serializer = new LBinarySerializer(16);
        var value = new IntNode { Value = 1, Tail = 10, Child = new IntNode { Value = 2, Tail = 20 } };
        int[] wireValues = [1, 24, 1, 12, 2, -1, 20, 10];
        var expected = new byte[wireValues.Length * sizeof(int)];
        for (var i = 0; i < wireValues.Length; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(i * sizeof(int)), wireValues[i]);
        }

        // Act
        serializer.Write(new[] { value });

        // Assert
        serializer.ToArray().Should().Equal(expected);
        new LBinaryDeserializer(expected).ReadArrayOf<IntNode>().Should().BeEquivalentTo(new[] { value });
    }

    [Theory]
    [InlineData("struct-object")]
    [InlineData("string-object")]
    [InlineData("string-struct")]
    public void Write_DictionarySubclass_ShouldHonorInterfaceEnumerationAndPreserveFollowingField(string kind)
    {
        // Arrange
        using var serializer = new LBinarySerializer();
        var value = new DummyNestedClass { Name = "first", Age = 42 };

        // Act
        switch (kind)
        {
            case "struct-object":
                IDictionary<int, DummyNestedClass> numbers = new FirstEntryDictionary<int, DummyNestedClass>
                {
                    [1] = value, [2] = null
                };
                serializer.Write(numbers);
                break;
            case "string-object":
                IDictionary<string, DummyNestedClass> objects = new FirstEntryDictionary<string, DummyNestedClass>
                {
                    ["first"] = value, ["second"] = null
                };
                serializer.Write(objects);
                break;
            case "string-struct":
                IDictionary<string, int> primitives = new FirstEntryDictionary<string, int>
                {
                    ["first"] = 42, ["second"] = 0
                };
                serializer.Write(primitives);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
        serializer.Write(99);
        var deserializer = new LBinaryDeserializer(serializer.ToArray());

        // Assert
        switch (kind)
        {
            case "struct-object":
                deserializer.ReadDictionaryOf<int, DummyNestedClass>().Should().ContainSingle()
                    .Which.Value.Should().BeEquivalentTo(value);
                break;
            case "string-object":
                deserializer.ReadDictionaryOf<DummyNestedClass>().Should().ContainSingle()
                    .Which.Value.Should().BeEquivalentTo(value);
                break;
            case "string-struct":
                deserializer.ReadDictionaryOfStructs<int>().Should().ContainSingle()
                    .Which.Should().Be(new KeyValuePair<string, int>("first", 42));
                break;
        }
        deserializer.ReadInt().Should().Be(99);
    }

    private class FirstEntryDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDictionary<TKey, TValue>
        where TKey : notnull
    {
        int ICollection<KeyValuePair<TKey, TValue>>.Count => Math.Min(1, Count);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            using var enumerator = base.GetEnumerator();
            if (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();
    }

    [Fact]
    public void ReadDecimal_InvalidFlags_ShouldStillRejectPayload()
    {
        // Arrange
        var bytes = new byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), 1);
        var deserializer = new LBinaryDeserializer(bytes);

        // Act
        Action read = () => deserializer.ReadDecimal();

        // Assert
        read.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Write_DecimalEdgeValues_ShouldPreserveBits()
    {
        // Arrange
        decimal[] values = [decimal.MinValue, decimal.MaxValue, 0m, 1e-28m, new decimal(0, 0, 0, true, 28)];
        using var serializer = new LBinarySerializer(16);

        foreach (var value in values)
        {
            // Act
            serializer.Reset();
            serializer.Write(value);
            var result = new LBinaryDeserializer(serializer.ToArray()).ReadDecimal();

            // Assert
            decimal.GetBits(result).Should().Equal(decimal.GetBits(value));
        }
    }

    public class IntNode : ILBinarySerializable
    {
        public int Value { get; set; }
        public int Tail { get; set; }
        public IntNode Child { get; set; }

        public void Serialize(LBinarySerializer serializer)
        {
            serializer.Write(Value);
            serializer.Write(Child);
            serializer.Write(Tail);
        }

        public void Deserialize<T>(LBinaryDeserializer deserializer)
        {
            Value = deserializer.ReadInt();
            Child = deserializer.Deserialize<IntNode>();
            Tail = deserializer.ReadInt();
        }
    }
}

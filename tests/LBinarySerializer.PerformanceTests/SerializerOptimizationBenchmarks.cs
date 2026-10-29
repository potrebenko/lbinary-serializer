using BenchmarkDotNet.Attributes;

namespace LBinarySerializer.PerformanceTests;

[MemoryDiagnoser]
public class SerializerOptimizationBenchmarks
{
    private readonly LBinarySerializer _serializer = new(32768);
    private readonly string _ascii = new('a', 4096);
    private readonly string _unicode = string.Concat(Enumerable.Repeat("é世😀", 1024));
    private readonly Dictionary<string, int> _dictionary = new()
    {
        ["one"] = 1, ["two"] = 2, ["three"] = 3
    };

    [Benchmark]
    public ReadOnlyMemory<byte> Primitives()
    {
        _serializer.Reset();
        for (var i = 0; i < 32; i++)
        {
            _serializer.Write(i);
            _serializer.Write((long)i);
            _serializer.Write((double)i);
        }
        return _serializer.ToMemory();
    }

    [Benchmark]
    public ReadOnlyMemory<byte> Dictionary()
    {
        _serializer.Reset();
        _serializer.Write(_dictionary);
        return _serializer.ToMemory();
    }

    [Benchmark]
    public ReadOnlyMemory<byte> AsciiString()
    {
        _serializer.Reset();
        _serializer.Write(_ascii);
        return _serializer.ToMemory();
    }

    [Benchmark]
    public ReadOnlyMemory<byte> UnicodeString()
    {
        _serializer.Reset();
        _serializer.Write(_unicode);
        return _serializer.ToMemory();
    }

    [Benchmark]
    public int StringWithBufferGrowth()
    {
        using var serializer = new LBinarySerializer(16);
        serializer.Write(_unicode);
        return serializer.ToMemory().Length;
    }

    [Benchmark]
    public int NewSerializer()
    {
        using var serializer = new LBinarySerializer();
        serializer.Write(42);
        return serializer.ToMemory().Length;
    }

    [Benchmark]
    public LBinaryDeserializer NewDeserializer() => new();

    [Benchmark]
    public byte[] OwnedOutput()
    {
        _serializer.Reset();
        _serializer.Write(_ascii);
        return _serializer.ToArray();
    }

    [GlobalCleanup]
    public void Cleanup() => _serializer.Dispose();
}

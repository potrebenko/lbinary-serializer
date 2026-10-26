LBinarySerializer
================

LBinarySerializer is a fast and efficient binary serializer for .NET.

![NuGet Version](https://img.shields.io/nuget/v/LBinarySerializer) ![NuGet Downloads](https://img.shields.io/nuget/dt/LBinarySerializer) ![GitHub License](https://img.shields.io/github/license/potrebenko/LBinarySerializer)

![badge](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/potrebenko/ae3e58be2403eb6ef2ad344cf26357b3/raw/code-coverage.yml)

Usage
---------------------

~~~csharp
namespace Example;

using LBinarySerializer;

public class TestClass : ILBinarySerializable
{
    public string? Name { get; set; }
    public int Age { get; set; }

    public void Serialize(LBinarySerializer serializer)
    {
        serializer.Write(Name);
        serializer.Write(Age);
    }

    public void Deserialize<T>(LBinaryDeserializer deserializer)
    {
        Name = deserializer.ReadString();
        Age = deserializer.ReadInt();
    }
}

public static class Converter
{
    public static byte[] Serialize<T>(T value) where T : ILBinarySerializable
    {
        using var serializer = new LBinarySerializer();
        value.Serialize(serializer);
        return serializer.ToArray();
    }

    public static T? Deserialize<T>(byte[] rawData) where T : class, ILBinarySerializable, new()
    {
        var deserializer = new LBinaryDeserializer(rawData);
        return deserializer.Deserialize<T>();
    }
}
~~~

The maximum size of serialized data is [2,147,483,647](https://docs.microsoft.com/en-us/dotnet/api/system.array?view=net-8.0#remarks) bytes.

Result lifetime
---------------

`ToArray()` returns an independent copy. Use it when returning bytes from a method,
storing them, or passing them to work that can outlive the serializer:

~~~csharp
using var serializer = new LBinarySerializer();
serializer.Write("saved result");
byte[] result = serializer.ToArray(); // Remains valid after disposal or reuse.
~~~

`ToMemory()` returns a borrowed view of a pooled buffer without copying. Keep the
serializer alive and do not write, reset, or dispose it while using that view.
After disposal or buffer growth, another serializer may reuse and overwrite the
buffer. For asynchronous consumers, finish and await their use before changing or
disposing the serializer.

Validation and compatibility
---------------------------

Invalid lengths, truncated fields, and collection counts that exceed the available
payload throw `InvalidDataException`. Missing trailing fields at an object boundary
still receive default values so older payloads remain readable.
Missing trailing nested objects receive `null`; an explicit zero-size object is
still constructed. Every declared collection entry must have its own headers.
Serialization rejects objects beyond the nesting limit with `InvalidOperationException`;
deserialization rejects them with `InvalidDataException`.

`ReadStruct<T>` requires an unmanaged type and a stored size matching that type.
Enums use four bytes: byte, sbyte, short, ushort, int, and uint underlying types are
supported; long and ulong underlying types throw `NotSupportedException`.
String dictionary keys are encoded and decoded as UTF-8.

Array (List) structure
---------------------

~~~mermaid
---
Array (List) structure
---
packet-beta
0-4: "Number of items"
5-12: "Item 1"
13-20: "Item 2"
~~~

Dictionary structure
---------------------

Header size is 4 bytes. -1 in the header means that the dictionary is null. 0 in the header means that the dictionary is empty.
The size of key and value can be any number of bytes. If the key is integer, it is serialized as Int32 and contains 4 bytes. The key can be any unmanaged type or string. The value can be any unmanaged type, string or ILBinarySerializable object.

~~~mermaid
---
Dictionary structure
---
packet-beta
0-4: "Number of elements"
5-8: "Key"
9-12: "Value"
13-16: "Key"
17-20: "Value"
~~~

String structure
---------------

Header size is 4 bytes. -1 in the header means that the string is null. 0 in the header means that the string is empty.

~~~mermaid
---
String structure
---
packet-beta
0-4: "String length"
5-20: "Data"
~~~

Object structure
---------------

Header size is 4 bytes. -1 in the header means that the object is null. Supports ILBinarySerializable objects that represent a class.

~~~mermaid
---
Object structure
---
packet-beta
0-4: "Object size"
5-104: "Data"
~~~

Struct structure
----------------

Header size is always 0 or greater. Supports only unmanaged types as fields. Strings, some structures or any ILBinarySerializable objects can't be used as fields. The deserializer supports only `unmanged` types.

~~~mermaid
---
Struct structure
---
packet-beta
0-4: "Structure size"
5-104: "Data"
~~~

DateTime structure
------------------------

The DateTime structure is represented in `Ticks` 8 bytes. First 4 bytes describe its kind: UTC, Local or Unspecified.

~~~mermaid
---
DateTime structure
---
packet-beta
0-4: "DateTime Kind"
5-12: "Ticks"
~~~

DateTimeOffset structure
------------------------

The DateTimeOffset structure has 2 parts: time in `Ticks` and its offset in TimeSpan.

~~~mermaid
---
DateTimeOffset structure
---
packet-beta
0-8: "Ticks"
9-16: "Offset"
~~~

Supported structure types
------------------------

| Name | Size |
| --- | --- |
| Boolean | 1 |
| Byte | 1 |
| SByte | 1 |
| Char | 2 |
| Half | 2 |
| Int16 | 2 |
| UInt16 | 2 |
| Single | 4 |
| Int32 | 4 |
| UInt32 | 4 |
| String | 2147483643 (max size of string in .net - header 4 bytes) |
| DateTime | 12 |
| DateTimeOffset | 16 |
| Double | 8 |
| Int64 | 8 |
| UInt64 | 8 |
| Guid | 16 |
| Decimal | 16 |

Performance tests and comparison
------------------------
Run both suites in Release mode:

~~~sh
dotnet run -c Release --project tests/LBinarySerializer.PerformanceTests/ -- --filter '*'
~~~

Measured on 2026-10-25. JSON serialization and deserialization use the same options;
setup verifies that the measured JSON reader restores the original object.

### .NET 10
~~~
BenchmarkDotNet v0.15.6, macOS 26.7.1 (25G241) [Darwin 25.6.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a
~~~

#### Small objects

| Method                     | Categories                | N | Mean      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-------------------------- |-- |----------:|------:|--------:|-------:|----------:|------------:|
| LBinaryDeserializer        | Deserialize Small Objects | 1 |  43.17 ns |  1.00 |    0.03 | 0.0268 |     168 B |        1.00 |
| ProtoBufDeserializer       | Deserialize Small Objects | 1 | 163.35 ns |  3.79 |    0.07 | 0.0267 |     168 B |        1.00 |
| JsonSerializerDeserializer | Deserialize Small Objects | 1 | 236.18 ns |  5.47 |    0.10 | 0.0267 |     168 B |        1.00 |
| MemoryPackDeserializer     | Deserialize Small Objects | 1 |  29.47 ns |  0.68 |    0.01 | 0.0268 |     168 B |        1.00 |
|                            |                           |   |           |       |         |        |           |             |
| LBinarySerializer          | Serialize Small Objects   | 1 |  38.43 ns |  1.00 |    0.00 | 0.0166 |     104 B |        1.00 |
| ProtoBufSerializer         | Serialize Small Objects   | 1 | 117.69 ns |  3.06 |    0.02 | 0.0165 |     104 B |        1.00 |
| JsonSerializer             | Serialize Small Objects   | 1 | 159.34 ns |  4.15 |    0.01 | 0.0267 |     168 B |        1.62 |
| MemoryPackSerializer       | Serialize Small Objects   | 1 |  36.84 ns |  0.96 |    0.01 | 0.0166 |     104 B |        1.00 |

#### Large objects

| Method                     | Categories                | N | Mean       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |-------------------------- |-- |-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| LBinaryDeserializer        | Deserialize Large Objects | 1 |   503.5 ns |  1.00 |    0.04 | 0.2956 | 0.0010 |    1856 B |        1.00 |
| ProtoBufDeserializer       | Deserialize Large Objects | 1 | 1,399.6 ns |  2.78 |    0.13 | 0.2956 | 0.0019 |    1856 B |        1.00 |
| JsonSerializerDeserializer | Deserialize Large Objects | 1 | 2,131.9 ns |  4.24 |    0.12 | 0.4082 |      - |    2576 B |        1.39 |
| MemoryPackDeserializer     | Deserialize Large Objects | 1 |   341.3 ns |  0.68 |    0.02 | 0.2956 | 0.0010 |    1856 B |        1.00 |
|                            |                           |   |            |       |         |        |        |           |             |
| LBinarySerializer          | Serialize Large Objects   | 1 |   398.6 ns |  1.00 |    0.02 | 0.1273 |      - |     800 B |        1.00 |
| ProtoBufSerializer         | Serialize Large Objects   | 1 |   802.6 ns |  2.01 |    0.03 | 0.1154 |      - |     728 B |        0.91 |
| JsonSerializer             | Serialize Large Objects   | 1 | 1,070.8 ns |  2.69 |    0.05 | 0.2365 |      - |    1488 B |        1.86 |
| MemoryPackSerializer       | Serialize Large Objects   | 1 |   223.9 ns |  0.56 |    0.01 | 0.1185 |      - |     744 B |        0.93 |

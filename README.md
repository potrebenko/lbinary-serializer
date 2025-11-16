LBinarySerializer
================

LBinarySerializer is a fast and efficient binary serializer for .NET.

![NuGet Version](https://img.shields.io/nuget/v/LBinarySerializer) ![NuGet Downloads](https://img.shields.io/nuget/dt/LBinarySerializer) ![GitHub License](https://img.shields.io/github/license/potrebenko/LBinarySerializer)

![badge](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/potrebenko/ae3e58be2403eb6ef2ad344cf26357b3/raw/code-coverage.yml)

Usage
---------------------

~~~csharp
public class TestClass : ILBinarySerializable
{
    public string Name { get; set; }
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
    public static byte[] Serialize(T object) where T : ILBinarySerializable
    {
        using var serializer = new LBinarySerializer();
        object.Serialize(serializer);
        return serializer.GetData();
    }

    public static T Deserialize<T>(byte[] rawData) where T : class, ILBinarySerializable, new()
    {
        var deserializer = new Deserializer(rawData);
        return deserializer.Deserialize<T>();
    }
}
~~~

The maximum size of serialized data is [2,147,483,647](https://docs.microsoft.com/en-us/dotnet/api/system.array?view=net-8.0#remarks) bytes.

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
### .NET 8
~~~
BenchmarkDotNet v0.14.0, macOS Sequoia 15.5 (24F74) [Darwin 24.5.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.13 (8.0.1325.6609), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.13 (8.0.1325.6609), Arm64 RyuJIT AdvSIMD
~~~

#### Small objects

| Method                     | Categories                | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-------------------------- |----------:|------:|-------:|----------:|------------:|
| LBinaryDeserializer        | Deserialize Small Objects |  49.34 ns |     ? | 0.0268 |     168 B |           ? |
| ProtoBufDeserializer       | Deserialize Small Objects | 185.80 ns |     ? | 0.0267 |     168 B |           ? |
| JsonSerializerDeserializer | Deserialize Small Objects | 157.23 ns |     ? | 0.0076 |      48 B |           ? |
|                            |                           |           |       |        |           |             |
| LBinarySerializer          | Serialize Small Objects   |  62.15 ns |  1.00 | 0.0280 |     176 B |        1.00 |
| ProtoBufSerializer         | Serialize Small Objects   | 158.43 ns |  2.55 | 0.0165 |     104 B |        0.59 |
| JsonSerializer             | Serialize Small Objects   | 201.34 ns |  3.24 | 0.0267 |     168 B |        0.95 |

### Large objects

| Method                     | Categories                | Mean       | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |-------------------------- |-----------:|------:|-------:|-------:|----------:|------------:|
| LBinaryDeserializer        | Deserialize Large Objects |   564.2 ns |     ? | 0.3071 | 0.0019 |    1928 B |           ? |
| ProtoBufDeserializer       | Deserialize Large Objects | 1,516.1 ns |     ? | 0.2956 | 0.0019 |    1856 B |           ? |
| JsonSerializerDeserializer | Deserialize Large Objects | 1,052.2 ns |     ? | 0.0172 |      - |     112 B |           ? |
|                            |                           |            |       |        |        |           |             |
| LBinarySerializer          | Serialize Large Objects   |   828.3 ns |  1.00 | 0.2632 |      - |    1656 B |        1.00 |
| ProtoBufSerializer         | Serialize Large Objects   | 1,035.0 ns |  1.25 | 0.1144 |      - |     728 B |        0.44 |
| JsonSerializer             | Serialize Large Objects   | 1,355.6 ns |  1.64 | 0.2365 |      - |    1488 B |        0.90 |

### .NET 10
~~~
BenchmarkDotNet v0.15.6, macOS 26.1 (25B78) [Darwin 25.1.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
~~~

#### Small objects

| Method                     | Categories                | N | Mean      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-------------------------- |-- |----------:|------:|--------:|-------:|----------:|------------:|
| MemoryPackSerializer       | Deserialize Small Objects | 1 |  35.18 ns |     ? |       ? | 0.0166 |     104 B |           ? |
| LBinaryDeserializer        | Deserialize Small Objects | 1 |  41.22 ns |     ? |       ? | 0.0268 |     168 B |           ? |
| ProtoBufDeserializer       | Deserialize Small Objects | 1 | 162.48 ns |     ? |       ? | 0.0267 |     168 B |           ? |
| JsonSerializerDeserializer | Deserialize Small Objects | 1 | 153.48 ns |     ? |       ? | 0.0076 |      48 B |           ? |
| MemoryPackDeserializer     | Deserialize Small Objects | 1 |  28.66 ns |     ? |       ? | 0.0268 |     168 B |           ? |
|                            |                           |   |           |       |         |        |           |             |
| LBinarySerializer          | Serialize Small Objects   | 1 |  47.51 ns |  1.00 |    0.00 | 0.0280 |     176 B |        1.00 |
| ProtoBufSerializer         | Serialize Small Objects   | 1 | 113.86 ns |  2.40 |    0.01 | 0.0166 |     104 B |        0.59 |
| JsonSerializer             | Serialize Small Objects   | 1 | 148.89 ns |  3.13 |    0.01 | 0.0267 |     168 B |        0.95 |

#### Large objects

| Method                     | Categories                | N | Mean       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |-------------------------- |-- |-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| LBinaryDeserializer        | Deserialize Large Objects | 1 |   493.3 ns |     ? |       ? | 0.3071 | 0.0019 |    1928 B |           ? |
| ProtoBufDeserializer       | Deserialize Large Objects | 1 | 1,271.2 ns |     ? |       ? | 0.2956 | 0.0019 |    1856 B |           ? |
| JsonSerializerDeserializer | Deserialize Large Objects | 1 |   996.9 ns |     ? |       ? | 0.0172 |      - |     112 B |           ? |
| MemoryPackDeserializer     | Deserialize Large Objects | 1 |   327.4 ns |     ? |       ? | 0.2956 | 0.0010 |    1856 B |           ? |
|                            |                           |   |            |       |         |        |        |           |             |
| LBinarySerializer          | Serialize Large Objects   | 1 |   672.7 ns |  1.00 |    0.00 | 0.2632 |      - |    1656 B |        1.00 |
| ProtoBufSerializer         | Serialize Large Objects   | 1 |   775.2 ns |  1.15 |    0.00 | 0.1154 |      - |     728 B |        0.44 |
| JsonSerializer             | Serialize Large Objects   | 1 | 1,054.8 ns |  1.57 |    0.00 | 0.2365 |      - |    1488 B |        0.90 |
| MemoryPackSerializer       | Serialize Large Objects   | 1 |   215.9 ns |  0.32 |    0.00 | 0.1185 |      - |     744 B |        0.45 |

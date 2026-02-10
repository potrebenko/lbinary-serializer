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
        return serializer.ToArray();
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
| LBinaryDeserializer        | Deserialize Small Objects | 1 |  42.59 ns |  1.00 |    0.01 | 0.0268 |     168 B |        1.00 |
| ProtoBufDeserializer       | Deserialize Small Objects | 1 | 164.02 ns |  3.85 |    0.02 | 0.0267 |     168 B |        1.00 |
| JsonSerializerDeserializer | Deserialize Small Objects | 1 | 154.87 ns |  3.64 |    0.02 | 0.0076 |      48 B |        0.29 |
| MemoryPackDeserializer     | Deserialize Small Objects | 1 |  29.24 ns |  0.69 |    0.00 | 0.0268 |     168 B |        1.00 |
|                            |                           |   |           |       |         |        |           |             |
| LBinarySerializer          | Serialize Small Objects   | 1 |  33.23 ns |  1.00 |    0.00 | 0.0166 |     104 B |        1.00 |
| ProtoBufSerializer         | Serialize Small Objects   | 1 | 115.43 ns |  3.47 |    0.02 | 0.0166 |     104 B |        1.00 |
| JsonSerializer             | Serialize Small Objects   | 1 | 151.22 ns |  4.55 |    0.03 | 0.0267 |     168 B |        1.62 |
| MemoryPackSerializer       | Serialize Small Objects   | 1 |  36.07 ns |  1.09 |    0.01 | 0.0166 |     104 B |        1.00 |


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

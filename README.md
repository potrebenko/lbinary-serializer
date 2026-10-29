# LBinarySerializer

A binary serializer for .NET 8, 9, and 10. Types implement `ILBinarySerializable`
and explicitly write and read their fields in the same order.

![NuGet Version](https://img.shields.io/nuget/v/LBinarySerializer) ![NuGet Downloads](https://img.shields.io/nuget/dt/LBinarySerializer) ![GitHub License](https://img.shields.io/github/license/potrebenko/LBinarySerializer)

![Unit test coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/potrebenko/ae3e58be2403eb6ef2ad344cf26357b3/raw/code-coverage.yml)

## Installation

```sh
dotnet add package LBinarySerializer
```

## Usage

This complete console example serializes an object and reads it back:

```csharp
using LBinarySerializer;
using BinarySerializer = LBinarySerializer.LBinarySerializer;

var value = new TestClass { Name = "Ada", Age = 37 };
using var serializer = new BinarySerializer();
value.Serialize(serializer);
byte[] data = serializer.ToArray();

var restored = new LBinaryDeserializer(data).Deserialize<TestClass>()!;
Console.WriteLine($"{restored.Name}: {restored.Age}");

public class TestClass : ILBinarySerializable
{
    public string? Name { get; set; }
    public int Age { get; set; }

    public void Serialize(BinarySerializer serializer)
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
```

The alias distinguishes the serializer class from its namespace. At the root,
call `value.Serialize(serializer)` to write fields without an object header.
Inside a model, use `serializer.Write(child)` and
`deserializer.Deserialize<Child>()` for a nested class; its size header lets the
reader find its boundary. Calling `serializer.Write(value)` at the root adds a
header that the root `Deserialize<T>()` does not expect.

## Result lifetime and reuse

`ToArray()` returns an independent copy that remains valid after the serializer
is reset or disposed. Use it for stored results and returned byte arrays.

`ToMemory()` returns a borrowed view of a pooled buffer without copying. Keep the
serializer alive and do not write, reset, or dispose it while using that view.
After disposal or buffer growth, another serializer may reuse and overwrite the
buffer. Await asynchronous consumers before changing or disposing the serializer.

For repeated operations, reuse a serializer and call `Reset()` before writing the
next payload. Consume its borrowed result completely before resetting it, or use
`ToArray()` for an independent copy. A reused deserializer's
`Deserialize<T>(byte[])` overload resets its read state for each payload. Use
separate instances for concurrent operations.

## Validation and compatibility

Field order and field types are the schema: payloads do not include property
names or type metadata. Keep serialization and deserialization aligned. Append
new fields at the end; reordering or changing existing fields is not compatible.

At an object boundary, absent trailing fields receive defaults. Missing nested
objects receive `null`; an explicit zero-size object is still constructed. Unread
trailing bytes in a nested object are skipped, allowing an older reader to move
to the next field in its parent.

Invalid lengths, partial fields, and collection counts that exceed the remaining
payload throw `InvalidDataException`. Each declared collection entry must have its
own headers. An exhausted object is treated as missing trailing fields, so this
validation does not detect every truncated message. Invalid date or decimal values
can also throw argument exceptions from their .NET constructors.

`SerializerSettings.MaxDepthSize` is 10: the root occupies level zero, leaving
nine levels for non-null nested objects. Exceeding the limit throws
`InvalidOperationException` when writing and `InvalidDataException` when reading.

The serialized buffer is bounded by `Array.MaxLength` and available memory;
large writes may require both the old and new buffers during growth.

### Unmanaged list API migration

The unmanaged `ReadListOf<T>(bool)` overload now returns `List<T>?` instead of
`List<T?>?`. For example, `ReadListOf<int>()` returns a nullable list of non-null
integers, which can be passed directly to `Write`. Null and empty lists keep their
existing binary representation.

This changes the public method signature: rebuild dependent applications and
replace explicit `List<int?>` declarations with `List<int>` or `var` as appropriate.
Class and string list readers still support null elements.

## Binary format

All sizes below are in bytes. Length and count headers are signed four-byte
little-endian integers. A length measures payload bytes, not characters; a count
measures collection entries. Headers exclude their own four bytes.

| Value | Header | Payload |
|---|---|---|
| String | Encoded byte length; `-1` = null, `0` = empty | Encoded text |
| Array or list | Item count; `-1` = null, `0` = empty | Items in order |
| Dictionary | Entry count; `-1` = null, `0` = empty | Key, value, repeated for each entry |
| Nested `ILBinarySerializable` class | Payload byte length; `-1` = null, `0` = empty object | Fields written by `Serialize` |
| `WriteStructure<T>` | Raw size of `T` | Raw unmanaged value |

Strings default to UTF-8. UTF-16 (little-endian Unicode), ASCII, and UTF-32 are also
available through `EncodingType`. Use the same encoding when writing and reading.
For example, UTF-8 `"é"` has a four-byte length header containing `2`, followed by
`C3 A9`.

Unmanaged arrays and lists store contiguous raw values after the count, with no
per-item size headers. String items have their own string headers; class items
have their own object headers. **`Write(byte[])`, `Write(Span<byte>)`, and
`Write(ReadOnlySpan<byte>)` write raw bytes without a count header.** To pair a
byte array with `ReadArrayOf<byte>()`, use `Write(bytes, unused: false)` to select
the counted unmanaged-array overload.

### Dictionaries

The API supports these key/value combinations:

| Key | Value | Reader |
|---|---|---|
| Unmanaged `TKey` | `ILBinarySerializable` class | `ReadDictionaryOf<TKey, TValue>()` |
| `string` | `ILBinarySerializable` class | `ReadDictionaryOf<TValue>()` |
| `string` | Unmanaged `TValue` | `ReadDictionaryOfStructs<TValue>()` |
| Unmanaged `TKey` | `string` | `ReadDictionaryOfStrings<TKey>()` |

String keys are always UTF-8. Unmanaged keys and values use the struct format:
a four-byte size header followed by the raw value. An `int` key therefore occupies
**eight bytes**, including its header. Class and string values use their respective
headers. Pass the same string-value encoding to writer and reader when using
`Dictionary<TKey, string>`.

### Structs and byte order

`WriteStructure<T>` and `ReadStruct<T>` require `unmanaged` types: no reference
fields. The writer stores `Unsafe.SizeOf<T>()` followed by the raw value. The
reader requires that size to match the requested type, except that an explicit
zero-size header returns `default(T)`.

Scalar integer, `char`, `Single`, and `Double` methods use little-endian byte order.
Raw structs, unmanaged collections, enum payloads, `Half`, and decimal words use
native memory byte order or layout. Raw structs can include padding; matching
layouts and endianness are required when exchanging these values between systems.
Prefer explicit scalar fields for a format that must avoid struct layout changes.
`Guid` uses the representation produced by `Guid.TryWriteBytes` and read by
`new Guid(bytes)`.

### Scalar sizes

These sizes apply to scalar methods, without a struct size header:

| Type | Bytes | Reader |
|---|---:|---|
| Boolean | 1 | `ReadBool()` |
| Byte | 1 | `ReadByte()` |
| Char | 2 | `ReadChar()` |
| Half | 2 | `ReadHalf()` |
| Int16 / UInt16 | 2 | `ReadShort()` / `ReadUShort()` |
| Int32 / UInt32 | 4 | `ReadInt()` / `ReadUInt()` |
| Single | 4 | `ReadFloat()` |
| Int64 / UInt64 | 8 | `ReadLong()` / `ReadULong()` |
| Double | 8 | `ReadDouble()` |
| TimeSpan | 8 | `ReadTimeSpan()` |
| DateTime | 12 | `ReadDateTime()` |
| DateTimeOffset | 16 | `ReadDateTimeOffset()` |
| Guid | 16 | `ReadGuid()` |
| Decimal | 16 | `ReadDecimal()` |
| Supported enum | 4 | `ReadEnum<TEnum>()` |

Enums support byte, sbyte, short, ushort, int, and uint underlying types. Long and
ulong underlying types throw `NotSupportedException`. There is no standalone
`Write(sbyte)` / `ReadSByte()` pair; use a matching unmanaged struct or collection
API when storing sbyte values as raw one-byte values.

`DateTime` is a four-byte `Kind` followed by eight-byte `Ticks` (byte offsets 0–3
and 4–11). `DateTimeOffset` is eight-byte local-clock `Ticks` followed by eight-byte
offset ticks (offsets 0–7 and 8–15); the first field is not UTC ticks.
`TimeSpan` stores its eight-byte tick count.

## Build and test

Use a .NET 10 SDK compatible with `global.json`. From the repository root:

```sh
dotnet build src/LBinarySerializer/ -c Release
dotnet test tests/LBinarySerializer.Tests/ -c Release
dotnet pack src/LBinarySerializer/ -c Release -o artifacts --include-symbols
```

The library targets .NET 8, 9, and 10; the test and benchmark projects target .NET 10.

## Performance tests and comparison
Run all benchmark suites in Release mode:

~~~sh
dotnet run -c Release --project tests/LBinarySerializer.PerformanceTests/ -- --filter '*'
~~~

### Optimization comparison (2026-10-27)

Compared commit `74758a5` with the optimized implementation on the Apple M1 Max
and .NET 10.0.12 environment shown below. The baseline ran from an isolated copy;
its generated benchmark project was checked to reference that copy.
These changes preserve the binary format, depth limits, and input validation.

The main workloads used BenchmarkDotNet's default job:

| Workload | Before | After | Allocated before | Allocated after |
|---|---:|---:|---:|---:|
| Serialize small objects | 38.46 ns | 22.92 ns | 104 B | 104 B |
| Deserialize small objects | 45.09 ns | 42.85 ns | 168 B | 168 B |
| Serialize large objects | 393.8 ns | 262.1 ns | 800 B | 744 B |
| Deserialize large objects | 484.9 ns | 493.2 ns | 1856 B | 1856 B |

Serialization improved; reused-deserializer throughput was roughly unchanged.
The following focused measurements used `ShortRun` (three warmups and three
measurement iterations). Treat their timing differences as indicative rather
than portable guarantees. Allocation counts are bytes per operation:

| Workload | Before | After | Allocated before | Allocated after |
|---|---:|---:|---:|---:|
| 96 primitive writes | 360.74 ns | 223.10 ns | 0 B | 0 B |
| Three-entry dictionary | 79.11 ns | 25.02 ns | 56 B | 0 B |
| 4,096 ASCII characters, borrowed output | 297.97 ns | 165.91 ns | 0 B | 0 B |
| 4,096 Unicode code units, borrowed output | 7,410.20 ns | 4,109.29 ns | 0 B | 0 B |
| Unicode string with buffer growth | 7,279.34 ns | 7,315.00 ns | 112 B | 40 B |
| Construct, write, dispose serializer | 54.18 ns | 45.41 ns | 112 B | 40 B |
| Construct deserializer | 16.87 ns | 10.99 ns | 168 B | 120 B |
| 4,096 ASCII characters, copied output | 505.64 ns | 359.06 ns | 4128 B | 4128 B |

The primitive benchmark reuses one serializer for 32 groups of int, long, and
double writes. String benchmarks use UTF-8. Borrowed-output cases consume the
result before the next reset; copied output retains its independent lifetime.
Construction benchmarks are separate because the main object benchmarks reuse
both serializer and deserializer instances.

Reproduce the main and focused workloads separately, from each version's root.
Copy `SerializerOptimizationBenchmarks.cs` into the baseline checkout as well so
both versions use the same focused workloads:

~~~sh
dotnet run -c Release --project tests/LBinarySerializer.PerformanceTests/ -- --filter '*ObjectsBenchmarks.LBinary*'
dotnet run -c Release --project tests/LBinarySerializer.PerformanceTests/ -- --filter '*OptimizationBenchmarks*' --job short
~~~

### Cross-library comparison (2026-10-28)

Measured on 2026-10-28 with the optimized implementation. JSON serialization and deserialization use the same options;
setup verifies that the measured JSON reader restores the original object.

The environment for these measurements:

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
| LBinaryDeserializer        | Deserialize Small Objects | 1 |  45.85 ns |  1.00 |    0.01 | 0.0268 |     168 B |        1.00 |
| ProtoBufDeserializer       | Deserialize Small Objects | 1 | 164.97 ns |  3.60 |    0.02 | 0.0267 |     168 B |        1.00 |
| JsonSerializerDeserializer | Deserialize Small Objects | 1 | 233.94 ns |  5.10 |    0.03 | 0.0267 |     168 B |        1.00 |
| MemoryPackDeserializer     | Deserialize Small Objects | 1 |  29.67 ns |  0.65 |    0.01 | 0.0268 |     168 B |        1.00 |
|                            |                           |   |           |       |         |        |           |             |
| LBinarySerializer          | Serialize Small Objects   | 1 |  22.95 ns |  1.00 |    0.00 | 0.0166 |     104 B |        1.00 |
| ProtoBufSerializer         | Serialize Small Objects   | 1 | 119.68 ns |  5.21 |    0.02 | 0.0165 |     104 B |        1.00 |
| JsonSerializer             | Serialize Small Objects   | 1 | 164.33 ns |  7.16 |    0.02 | 0.0267 |     168 B |        1.62 |
| MemoryPackSerializer       | Serialize Small Objects   | 1 |  35.30 ns |  1.54 |    0.00 | 0.0166 |     104 B |        1.00 |

#### Large objects

| Method                     | Categories                | N | Mean       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |-------------------------- |-- |-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| LBinaryDeserializer        | Deserialize Large Objects | 1 |   499.7 ns |  1.00 |    0.02 | 0.2956 | 0.0010 |    1856 B |        1.00 |
| ProtoBufDeserializer       | Deserialize Large Objects | 1 | 1,324.3 ns |  2.65 |    0.03 | 0.2956 | 0.0019 |    1856 B |        1.00 |
| JsonSerializerDeserializer | Deserialize Large Objects | 1 | 2,101.9 ns |  4.21 |    0.05 | 0.4082 |      - |    2576 B |        1.39 |
| MemoryPackDeserializer     | Deserialize Large Objects | 1 |   328.3 ns |  0.66 |    0.01 | 0.2956 | 0.0010 |    1856 B |        1.00 |
|                            |                           |   |            |       |         |        |        |           |             |
| LBinarySerializer          | Serialize Large Objects   | 1 |   261.0 ns |  1.00 |    0.00 | 0.1183 |      - |     744 B |        1.00 |
| ProtoBufSerializer         | Serialize Large Objects   | 1 |   805.6 ns |  3.09 |    0.01 | 0.1154 |      - |     728 B |        0.98 |
| JsonSerializer             | Serialize Large Objects   | 1 | 1,070.2 ns |  4.10 |    0.01 | 0.2365 |      - |    1488 B |        2.00 |
| MemoryPackSerializer       | Serialize Large Objects   | 1 |   229.4 ns |  0.88 |    0.00 | 0.1185 |      - |     744 B |        1.00 |

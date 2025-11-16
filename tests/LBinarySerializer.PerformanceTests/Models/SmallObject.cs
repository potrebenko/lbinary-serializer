using MemoryPack;
using ProtoBuf;

namespace LBinarySerializer.PerformanceTests;

[ProtoContract]
[MemoryPackable]
public partial class SmallObject : ILBinarySerializable
{
    [ProtoMember(1)]
    public int IntValue { get; set; }
    
    [ProtoMember(2)]
    public string? StringValue { get; set; }
    
    [ProtoMember(3)]
    public DateTime DateTime { get; set; }
    
    [ProtoMember(4)]
    public double DoubleValue { get; set; }
    
    public void Serialize(LBinarySerializer serializer)
    {
        serializer.Write(IntValue);
        serializer.Write(StringValue);
        serializer.Write(DateTime);
        serializer.Write(DoubleValue);
    }

    public void Deserialize<T>(LBinaryDeserializer deserializer)
    {
        IntValue = deserializer.ReadInt();
        StringValue = deserializer.ReadString();
        DateTime = deserializer.ReadDateTime();
        DoubleValue = deserializer.ReadDouble();
    }
}
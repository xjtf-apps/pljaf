using Orleans;

namespace voks.server.model;

[GenerateSerializer]
public class Media
{
    [Id(0)] public Guid StoreId { get; set; }
    [Id(1)] public string Filename { get; set; }
    [Id(2)] public byte[] BinaryData { get; set; }
}
using Orleans;

namespace pljaf.server.model;

[GenerateSerializer]
public class Media
{
    [Id(0)] public required Guid StoreId { get; set; }
}
using Orleans;

namespace pljaf.server.model;

[GenerateSerializer]
public class Media
{
    [Id(0)] public Guid StoreId { get; set; }
}
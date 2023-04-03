namespace pljaf.client.model;

public abstract class Message
{
    public abstract bool ContainsLink();
    public required Metadata Metadata { get; init; }
    public abstract ValueTask<Message> UpdateMetadataAsync();
}

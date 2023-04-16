namespace voks.client.model;

public abstract class Message
{
    public abstract bool ContainsLink();
    public required MsgId MsgId {  get; set; }
    public required Metadata Metadata { get; init; }
    public abstract ValueTask<Message> UpdateMetadataAsync();
}

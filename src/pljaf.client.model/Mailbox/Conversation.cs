namespace pljaf.client.model;

public abstract class Conversation
{
    public required ConvId ConvId { get; init; }
    public required ConversationState State { get; init; }
    public required IEnumerable<Message> Messages { get; init; }

    public string? Name { get; init; }
    public string? Topic { get; init; }

    public abstract Task<Conversation> UpdateConversation();
    public abstract Task<Metadata> SendMessage(OriginalMessage message);
    public abstract Task<Conversation> SendSuccessfull(OriginalMessage message, Metadata metadata);
}
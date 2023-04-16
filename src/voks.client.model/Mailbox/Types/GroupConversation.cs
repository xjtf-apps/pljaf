namespace voks.client.model;

public sealed class GroupConversation : Conversation
{
    public override Task<Metadata> SendMessage(OriginalMessage message)
    {
        throw new NotImplementedException();
    }

    public override Task<Conversation> SendSuccessfull(OriginalMessage message, Metadata metadata)
    {
        throw new NotImplementedException();
    }

    public override Task<Conversation> UpdateConversation()
    {
        throw new NotImplementedException();
    }
}

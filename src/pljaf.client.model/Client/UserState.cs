namespace pljaf.client.model;

public sealed class UserState
{
    public readonly Dictionary<ConvId, ConversationState> ConversationStates = new();
}
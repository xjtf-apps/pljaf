namespace voks.client.model;

public sealed class UserState
{
    public readonly Dictionary<ConvId, ConversationState> ConversationStates = new();
}
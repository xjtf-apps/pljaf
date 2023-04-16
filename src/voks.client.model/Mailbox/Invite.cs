namespace voks.client.model;

public sealed class Invite
{
    public required UserId Inviter { get; init; }
    public required UserId Invited { get; init; }
    public required ConvId Conversation { get; init; }
}
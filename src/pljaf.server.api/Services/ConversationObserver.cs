using pljaf.server.model;

namespace pljaf.server.api;

public sealed class ConversationObserver : ICommunicationObserver
{
    private readonly IGrainFactory _grainFactory;
    private readonly IConversationGrain _conversation;

    public event EventHandler<string>? OnChange;

    public ConversationObserver(IGrainFactory grainFactory, IConversationGrain conversation)
    {
        _conversation = conversation;
        _grainFactory = grainFactory;
    }

    public async Task SubscribeToGrain()
    {
        await ((ICommunicationObserver)this).SubscribeToConversationGrain(_grainFactory, _conversation);
    }

    public async Task UnsubscribeFromGrain()
    {
        await ((ICommunicationObserver)this).UnsubscribeFromConversationGrain(_grainFactory, _conversation);
    }

    public async Task OnNameChanged(string name)
    {
        OnChange?.Invoke(this, $"Conversation:NameChanged, ConvId={await _conversation.GetIdAsync()}, Name={name}");
    }

    public async Task OnTopicChanged(string topic)
    {
        OnChange?.Invoke(this, $"Conversation:TopicChanged, ConvId={await _conversation.GetIdAsync()}, Topic={topic}");
    }

    public async Task OnMemberJoined(IUserGrain newMember)
    {
        OnChange?.Invoke(this, $"Conversation:MemberJoined, ConvId={await _conversation.GetIdAsync()}, UserId={await newMember.GetIdAsync()}");
    }

    public async Task OnMemberLeft(IUserGrain leftMember)
    {
        OnChange?.Invoke(this, $"Conversation:MemberLeft, ConvId={await _conversation.GetIdAsync()}, UserId={await leftMember.GetIdAsync()}");
    }

    public async Task OnMemberInvited(IUserGrain inviter, IUserGrain invited)
    {
        OnChange?.Invoke(this, $"Conversation:MemberLeft, ConvId={await _conversation.GetIdAsync()}, UserIdInviter={await inviter.GetIdAsync()}, UserIdInvited={await invited.GetIdAsync()}");
    }

    public async Task OnMessagePosted(IMessageGrain message)
    {
        OnChange?.Invoke(this, $"Conversation:MessagePosted, ConvId={await _conversation.GetIdAsync()}, MessageId={await message.GetIdAsync()}");
    }
}

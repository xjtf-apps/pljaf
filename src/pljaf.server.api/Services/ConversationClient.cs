using pljaf.server.model;

namespace pljaf.server.api;

public class ConversationClient : ICommunicationObserver, IDisposable
{
    private readonly object _lock = new();
    private readonly IConversationGrain _conversation;
    private readonly List<IMessageGrain> _messages = new();
    private readonly List<MessageClient> _messageObservers = new();

    public event EventHandler<string>? OnChange;

    public ConversationClient(IConversationGrain conversation)
    {
        _conversation = conversation;
    }

    public async Task Subscribe(IGrainFactory grainFactory)
    {
        await ((ICommunicationObserver)this).SubscribeToConversationGrain(grainFactory, _conversation);
    }

    public async Task Unsubscribe(IGrainFactory grainFactory)
    {
        await ((ICommunicationObserver)this).UnsubscribeFromConversationGrain(grainFactory, _conversation);
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

        try
        {
            if (Monitor.TryEnter(_lock))
            {
                _messages.Add(message);
                _messageObservers.Add(new MessageClient(message));
                _messageObservers[-1].Subscribe(message.get)
                _messageObservers[-1].OnChange += ConversationClient_OnMessageChange;
            }
        }
        finally
        {
            if (Monitor.IsEntered(_lock))
                Monitor.Exit(_lock);
        }
    }

    private async void ConversationClient_OnMessageChange(object? sender, string e)
    {
        OnChange?.Invoke(this, $"Conversation:MessageChanged, ConvId={await _conversation.GetIdAsync()},{Environment.NewLine}{e}");
    }

    public void Dispose()
    {
        _messageObservers.ForEach(obs =>
        {
            obs.OnChange -= ConversationClient_OnMessageChange;
        });
    }
}

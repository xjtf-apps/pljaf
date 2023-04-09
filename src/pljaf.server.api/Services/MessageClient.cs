using pljaf.server.model;

namespace pljaf.server.api;

public class MessageClient : IMessageObserver
{
    private readonly IMessageGrain _message;
    public event EventHandler<string>? OnChange;

    public MessageClient(IMessageGrain message)
    {
        _message = message;
    }

    public async Task Subscribe(IGrainFactory grainFactory)
    {
        await ((IMessageObserver)this).SubscribeToMessageGrain(grainFactory, _message);
    }

    public async Task Unsubscribe(IGrainFactory grainFactory)
    {
        await ((IMessageObserver)this).UnsubscribeFromMessageGrain(grainFactory, _message);
    }

    public async Task DownloadAttachedMedia(Media? mediaRef)
    {
        OnChange?.Invoke(this, $"Message:MediaChanged, MsgId={await _message.GetIdAsync()}, StoreId={mediaRef?.StoreId}");
    }

    public async Task ReceiveSentConfirmation(DateTime timestamp)
    {
        OnChange?.Invoke(this, $"Message:Confirmed, MsgId={await _message.GetIdAsync()}, Timestamp={timestamp}");
    }
}
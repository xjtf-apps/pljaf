using pljaf.server.model;

namespace pljaf.server.api;

public class MessageObserver : IMessageObserver, IAsyncDisposable
{
    private readonly IMessageGrain _message;
    private readonly IGrainFactory _grainFactory;

    public event EventHandler<string>? OnChange;
    
    public bool IsConfirmed { get; private set; }

    public MessageObserver(IGrainFactory grainFactory, IMessageGrain message)
    {
        _message = message;
        _grainFactory = grainFactory;
    }

    public async Task SubscribeToGrain()
    {
        await ((IMessageObserver)this).SubscribeToMessageGrain(_grainFactory, _message);
    }

    public async Task UnsubscribeFromGrain()
    {
        await ((IMessageObserver)this).UnsubscribeFromMessageGrain(_grainFactory, _message);
    }

    public async Task DownloadAttachedMedia(Media? mediaRef)
    {
        OnChange?.Invoke(this, $"Message:MediaChanged, MsgId={await _message.GetIdAsync()}, StoreId={mediaRef?.StoreId}");
    }

    public async Task ReceiveSentConfirmation(DateTime timestamp)
    {
        OnChange?.Invoke(this, $"Message:Confirmed, MsgId={await _message.GetIdAsync()}, Timestamp={timestamp}");
        IsConfirmed = true;
    }

    public async ValueTask DisposeAsync()
    {
        await UnsubscribeFromGrain();
    }
}
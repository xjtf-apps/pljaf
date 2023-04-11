using Orleans;
using Orleans.Runtime;
using Orleans.Utilities;
using Microsoft.Extensions.Logging;

namespace pljaf.server.model;

public class MessageGrain : Grain, IMessageGrain
{
    private readonly IPersistentState<DateTime> _timestamp;
    private readonly IPersistentState<StringValue> _senderId;
    private readonly IPersistentState<Media?> _mediaReference;
    private readonly IPersistentState<StringValue> _encryptedTextData;

    private readonly ObserverManager<IMessageAuthoredObserver> _messageAuthoredManager;
    private readonly ObserverManager<IMessageMediaAttachedObserver> _mediaAttachedManager;

    public MessageGrain(
        ILogger<MessageGrain> logger,
        [PersistentState(Constants.StoreKeys.Message.Sender)] IPersistentState<StringValue> sender,
        [PersistentState(Constants.StoreKeys.Message.Timestamp)] IPersistentState<DateTime> timestamp,
        [PersistentState(Constants.StoreKeys.Message.EncryptedTextData)] IPersistentState<StringValue> encryptedTextData,
        [PersistentState(Constants.StoreKeys.Message.MediaReference, Constants.Stores.MediaStore)]IPersistentState<Media?> mediaReference)
    {
        _senderId = sender;
        _timestamp = timestamp;
        _mediaReference = mediaReference;
        _encryptedTextData = encryptedTextData;

        var observerTimespan = TimeSpan.FromMinutes(5);
        _mediaAttachedManager = new ObserverManager<IMessageMediaAttachedObserver>(observerTimespan, logger);
        _messageAuthoredManager = new ObserverManager<IMessageAuthoredObserver>(observerTimespan, logger);
    }

    public async Task<Guid> GetIdAsync() => await Task.FromResult(this.GetGrainId().GetGuidKey());
    public async Task<IUserGrain> GetSenderAsync() => await Task.FromResult(GrainFactory.GetGrain<IUserGrain>(_senderId.State.Value));
    public async Task<DateTime> GetTimestampAsync() => await Task.FromResult(_timestamp.State);
    public async Task<Media?> GetMediaReferenceAsync() => await Task.FromResult(_mediaReference.State);
    public async Task<string> GetEncryptedTextDataAsync() => await Task.FromResult(_encryptedTextData.State.Value!);

    public async Task SetMediaReferenceAsync(Media? mediaReference)
    {
        _mediaReference.State = mediaReference; await _mediaReference.WriteStateAsync();
        await _mediaAttachedManager.Notify(sub => sub.DownloadAttachedMedia(mediaReference));
    }

    public async Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, string encryptedTextData)
    {
        _senderId.State = StringValue.New(await sender.GetIdAsync()); await _senderId.WriteStateAsync();
        _timestamp.State = timestamp; await _timestamp.WriteStateAsync();
        _encryptedTextData.State = StringValue.New(encryptedTextData); await _encryptedTextData.WriteStateAsync();
        await _messageAuthoredManager.Notify(sub => sub.ReceiveSentConfirmation(timestamp));
    }

    #region observers
    public Task Subscribe(IMessageMediaAttachedObserver mediaAttachedObserver)
    {
        _mediaAttachedManager.Subscribe(mediaAttachedObserver, mediaAttachedObserver);
        return Task.CompletedTask;
    }

    public Task Subscribe(IMessageAuthoredObserver messageAuthoredObserver)
    {
        _messageAuthoredManager.Subscribe(messageAuthoredObserver, messageAuthoredObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IMessageMediaAttachedObserver mediaAttachedObserver)
    {
        _mediaAttachedManager.Unsubscribe(mediaAttachedObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IMessageAuthoredObserver messageAuthoredObserver)
    {
        _messageAuthoredManager.Unsubscribe(messageAuthoredObserver);
        return Task.CompletedTask;
    }
    #endregion
}
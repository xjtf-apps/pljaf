﻿using Orleans;
using Orleans.Runtime;
using Orleans.Utilities;
using Microsoft.Extensions.Logging;

namespace pljaf.server.model;

public class MessageGrain : Grain, IMessageGrain
{
    private readonly IPersistentState<IUserGrain> _sender;
    private readonly IPersistentState<DateTime> _timestamp;
    private readonly IPersistentState<Media?> _mediaReference;
    private readonly IPersistentState<byte[]> _encryptedTextData;

    private readonly ObserverManager<IMediaAttachedObserver> _mediaAttachedManager;
    private readonly ObserverManager<IMessageAuthoredObserver> _messageAuthoredManager;

    public MessageGrain(
        ILogger<MessageGrain> logger,
        [PersistentState(Constants.StoreKeys.Message.Sender)] IPersistentState<IUserGrain> sender,
        [PersistentState(Constants.StoreKeys.Message.Timestamp)] IPersistentState<DateTime> timestamp,
        [PersistentState(Constants.StoreKeys.Message.EncryptedTextData)] IPersistentState<byte[]> encryptedTextData,
        [PersistentState(Constants.StoreKeys.Message.MediaReference, Constants.Stores.MediaStore)]IPersistentState<Media?> mediaReference)
    {
        _sender = sender;
        _timestamp = timestamp;
        _mediaReference = mediaReference;
        _encryptedTextData = encryptedTextData;

        var observerTimespan = TimeSpan.FromMinutes(5);
        _mediaAttachedManager = new ObserverManager<IMediaAttachedObserver>(observerTimespan, logger);
        _messageAuthoredManager = new ObserverManager<IMessageAuthoredObserver>(observerTimespan, logger);
    }

    public async Task<Guid> GetIdAsync() => await Task.FromResult(this.GetGrainId().GetGuidKey());
    public async Task<IUserGrain> GetSenderAsync() => await Task.FromResult(_sender.State);
    public async Task<DateTime> GetTimestampAsync() => await Task.FromResult(_timestamp.State);
    public async Task<Media?> GetMediaReferenceAsync() => await Task.FromResult(_mediaReference.State);
    public async Task<byte[]> GetEncryptedTextDataAsync() => await Task.FromResult(_encryptedTextData.State);

    public async Task SetMediaReferenceAsync(Media? mediaReference)
    {
        _mediaReference.State = mediaReference; await _mediaReference.WriteStateAsync();
        await _mediaAttachedManager.Notify(sub => sub.DownloadAttachedMedia(mediaReference));
    }

    public async Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, byte[] encryptedTextData)
    {
        _sender.State = sender; await _sender.WriteStateAsync();
        _timestamp.State = timestamp; await _timestamp.WriteStateAsync();
        _encryptedTextData.State = encryptedTextData; await _encryptedTextData.WriteStateAsync();
        await _messageAuthoredManager.Notify(sub => sub.ReceiveSentConfirmation(timestamp));
    }

    #region observers
    public Task Subscribe(IMediaAttachedObserver mediaAttachedObserver)
    {
        _mediaAttachedManager.Subscribe(mediaAttachedObserver, mediaAttachedObserver);
        return Task.CompletedTask;
    }

    public Task Subscribe(IMessageAuthoredObserver messageAuthoredObserver)
    {
        _messageAuthoredManager.Subscribe(messageAuthoredObserver, messageAuthoredObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IMediaAttachedObserver mediaAttachedObserver)
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
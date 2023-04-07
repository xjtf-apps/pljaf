﻿using Orleans;

namespace pljaf.server.model;

public interface IMessageGrain : IGrainWithGuidKey
{
    Task<Guid> GetIdAsync();
    Task<IUserGrain> GetSenderAsync();
    Task<DateTime> GetTimestampAsync();

    Task SetMediaReferenceAsync(Media? media);
    Task<Media?> GetMediaReferenceAsync();
    Task<byte[]> GetEncryptedTextDataAsync();

    Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, byte[] encryptedTextData);

    #region observers
    Task Subscribe(IMediaAttachedObserver mediaAttachedObserver);
    Task Subscribe(IMessageAuthoredObserver messageAuthoredObserver);
    Task Unsubscribe(IMediaAttachedObserver mediaAttachedObserver);
    Task Unsubscribe(IMessageAuthoredObserver messageAuthoredObserver);
    #endregion
}

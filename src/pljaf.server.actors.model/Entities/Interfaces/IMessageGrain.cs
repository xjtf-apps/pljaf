using Orleans;

namespace pljaf.server.model;

public interface IMessageGrain : IGrainWithGuidKey
{
    Task<Guid> GetIdAsync();
    Task<IUserGrain> GetSenderAsync();
    Task<DateTime> GetTimestampAsync();

    Task SetMediaReferenceAsync(Media? media);
    Task<Media?> GetMediaReferenceAsync();
    Task<string> GetEncryptedTextDataAsync();

    Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, string encryptedTextData);

    #region observers
    Task Subscribe(IMessageMediaAttachedObserver mediaAttachedObserver);
    Task Subscribe(IMessageAuthoredObserver messageAuthoredObserver);
    Task Unsubscribe(IMessageMediaAttachedObserver mediaAttachedObserver);
    Task Unsubscribe(IMessageAuthoredObserver messageAuthoredObserver);
    #endregion
}

using Orleans;

namespace pljaf.server.model;

public interface IMessageGrain : IGrainWithGuidKey
{
    Task<Guid> GetIdAsync();
    Task<IUserGrain> GetSenderAsync();
    Task<DateTime> GetTimestampAsync();
    Task<Media?> GetMediaReferenceAsync();
    Task<string> GetEncryptedTextDataAsync();

    Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, string encryptedTextData, Media? media);
}

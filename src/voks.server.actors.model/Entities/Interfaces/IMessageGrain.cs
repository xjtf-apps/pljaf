using Orleans;
using Orleans.Concurrency;

namespace voks.server.model;

public interface IMessageGrain : IGrainWithGuidKey
{
    [AlwaysInterleave] Task<Guid> GetIdAsync();
    [AlwaysInterleave] Task<IUserGrain> GetSenderAsync();
    [AlwaysInterleave] Task<DateTime> GetTimestampAsync();
    [AlwaysInterleave] Task<Media?> GetMediaReferenceAsync();
    [AlwaysInterleave] Task<string> GetEncryptedTextDataAsync();

    Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, string encryptedTextData, Media? media);
}

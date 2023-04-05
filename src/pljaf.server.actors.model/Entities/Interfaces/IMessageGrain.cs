using Orleans;

namespace pljaf.server.model;

public interface IMessageGrain : IGrainWithGuidKey
{
    Guid Id { get; }
    IUserGrain Sender { get; }
    DateTime Timestamp { get; }

    Media? MediaReference { get; }
    byte[] EncryptedTextData { get; }

    Task AuthorMessageAsync(IUserGrain sender, DateTime timestamp, byte[] encryptedTextData, Media? mediaReference = null);
}

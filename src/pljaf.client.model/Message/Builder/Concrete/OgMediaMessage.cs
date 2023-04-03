namespace pljaf.client.model;

public sealed class OgMediaMessage : OriginalMessage, IMediaSource
{
    internal IMediaSource MediaSource { get; init; }
    internal OgMediaMessage(IMediaSource mediaSource) => MediaSource = mediaSource;

    public Task<byte[]> GetMediaDataAsync()
    {
        return MediaSource.GetMediaDataAsync();
    }
}

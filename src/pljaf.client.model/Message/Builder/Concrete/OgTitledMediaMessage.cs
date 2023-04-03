namespace pljaf.client.model;

public sealed class OgTitledMediaMessage : OriginalMessage, IUnicodeBody, IMediaSource
{
    internal IUnicodeBody UnicodeBody { get; init; }
    internal IMediaSource MediaSource { get; init; }
    internal OgTitledMediaMessage(IUnicodeBody unicodeBody, IMediaSource mediaSource)
    {
        UnicodeBody = unicodeBody;
        MediaSource = mediaSource;
    }

    public string GetTextField()
    {
        return UnicodeBody.GetTextField();
    }

    public Task<byte[]> GetMediaDataAsync()
    {
        return MediaSource.GetMediaDataAsync();
    }
}
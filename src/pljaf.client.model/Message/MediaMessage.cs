namespace pljaf.client.model;

public abstract class MediaMessage : Message, IMediaSource
{
    public abstract Task<byte[]> GetMediaDataAsync();
    public abstract MediaReference GetMediaReference();
}

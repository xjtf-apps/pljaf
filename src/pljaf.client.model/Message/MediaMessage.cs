namespace pljaf.client.model;

public abstract class MediaMessage : Message, IMediaSource
{
    public abstract MediaReference GetMediaReference();
}

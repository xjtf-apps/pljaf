namespace pljaf.client.model;

public abstract class TitledMediaMessage : Message, IUnicodeBody, IMediaSource
{
    public abstract string GetTextField();
    public abstract MediaReference GetMediaReference();
}
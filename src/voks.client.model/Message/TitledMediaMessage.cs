namespace voks.client.model;

public abstract class TitledMediaMessage : Message, IUnicodeBody, IMediaSource
{
    public abstract string GetTextField();
    public abstract Task<byte[]> GetMediaDataAsync();
    public abstract MediaReference GetMediaReference();
}
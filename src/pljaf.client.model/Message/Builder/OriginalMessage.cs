namespace pljaf.client.model;

public abstract class OriginalMessage
{
    public required UserId Sender { get; init; }
    public required ConvId Conversation { get; init; }

    public static class Text
    {
        public static OgTextMessage FromSender(UserId sender, ConvId conversation, IUnicodeBody textBody)
            =>
                new(textBody) { Sender = sender, Conversation = conversation };
    }

    public static class Media
    {
        public static OgMediaMessage FromSender(UserId sender, ConvId conversation, IMediaSource mediaSource)
            =>
                new(mediaSource) { Sender = sender, Conversation = conversation };
    }
    
    public static class MediaWithTitle
    {
        public static OgTitledMediaMessage FromSender(UserId sender, ConvId conversation, IUnicodeBody titleBody, IMediaSource mediaSource)
            =>
                new(titleBody, mediaSource) { Sender = sender, Conversation = conversation };
    }
}

namespace voks.client.model;

public sealed class OgTextMessage : OriginalMessage, IUnicodeBody
{
    internal IUnicodeBody UnicodeBody { get; init; }
    internal OgTextMessage(IUnicodeBody unicodeBody) => UnicodeBody = unicodeBody;

    public string GetTextField()
    {
        return UnicodeBody.GetTextField();
    }
}

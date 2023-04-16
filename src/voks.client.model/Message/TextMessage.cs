namespace voks.client.model;

public abstract class TextMessage : Message, IUnicodeBody
{
    public abstract string GetTextField();
}

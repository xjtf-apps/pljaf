namespace pljaf.client.model;

public abstract class TextMessage : Message, IUnicodeBody
{
    public abstract string GetTextField();
}

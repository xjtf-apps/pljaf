namespace voks.server.model;

[GenerateSerializer]
public class StringValue
{
    [Id(0)] public string? Value { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is StringValue other)
        {
            return Value?.Equals(other.Value) ?? false;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }

    public static StringValue New(string? value = null)
        =>
            new() { Value = value };
}

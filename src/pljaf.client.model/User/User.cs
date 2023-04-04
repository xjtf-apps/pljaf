namespace pljaf.client.model;

public sealed class User
{
    public required UserId Id { get; init; }
    public ImageRef? AvatarRef { get; init; }
}

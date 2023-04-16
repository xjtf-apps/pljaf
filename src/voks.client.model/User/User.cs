namespace voks.client.model;

public sealed class User
{
    public UserId? Id { get; init; }
    public ImageRef? AvatarRef { get; init; }
    public string? DisplayName { get; set; }
    public string? StatusLine { get; set; } = "";
}

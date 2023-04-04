namespace pljaf.server.model;

public class Profile
{
    public required string DisplayName { get; set; }
    public required string StatusLine { get; set; } = "";
    public Media? ProfilePicture { get; set; }
}

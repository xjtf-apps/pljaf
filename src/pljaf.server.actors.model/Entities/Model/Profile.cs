using Orleans;

namespace pljaf.server.model;

[GenerateSerializer]
public class Profile
{
    [Id(0)] public required string DisplayName { get; set; }
    [Id(1)] public required string StatusLine { get; set; } = "";
    [Id(2)] public Media? ProfilePicture { get; set; }
}

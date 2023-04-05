using Orleans;

namespace pljaf.server.model;

[GenerateSerializer]
public class Profile
{
    [Id(0)] public string DisplayName { get; set; }
    [Id(1)] public string StatusLine { get; set; } = "";
    [Id(2)] public Media? ProfilePicture { get; set; }
}

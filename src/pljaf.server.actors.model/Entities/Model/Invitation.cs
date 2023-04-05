using Orleans;

namespace pljaf.server.model;

[GenerateSerializer]
public class Invitation
{
    [Id(0)] public required DateTime Timestamp { get; set; }
    [Id(1)] public required IUserGrain Inviter { get; set; }
    [Id(2)] public required IUserGrain Invited { get; set; }
}

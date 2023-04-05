using Orleans;

namespace pljaf.server.model;

[GenerateSerializer]
public class Invitation
{
    [Id(0)] public DateTime Timestamp { get; set; }
    [Id(1)] public IUserGrain Inviter { get; set; }
    [Id(2)] public IUserGrain Invited { get; set; }
}

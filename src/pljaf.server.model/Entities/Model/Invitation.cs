namespace pljaf.server.model;

public class Invitation
{
    public required DateTime Timestamp { get; set; }
    public required IUserGrain Inviter { get; set; }
    public required IUserGrain Invited { get; set; }
}
